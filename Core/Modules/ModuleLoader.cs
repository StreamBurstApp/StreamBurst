using StreamBurst.Abstractions.Catalog;
using StreamBurst.Abstractions.EventBus;
using StreamBurst.Abstractions.Module;
using Infrastructure.Catalog;
using Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Modules
{
    internal sealed class ModuleLoader
    {
        private static readonly Version CurrentSdkVersion = new(1, 0, 0);

        private readonly string _modulesRoot;
        private readonly string _appDataRoot;
        private readonly IEventBus _eventBus;
        private readonly IEventCatalogRegistry _catalogRegistry;
        private readonly ILoggerFactory _loggerFactory;

        private readonly Dictionary<string, LoadedModule> _loadedModules = new();

        public IReadOnlyDictionary<string, LoadedModule> LoadedModules => _loadedModules;

        public ModuleLoader(
            string appDataRoot,
            IEventBus eventBus,
            IEventCatalogRegistry catalogRegistry,
            ILoggerFactory loggerFactory)
        {
            _appDataRoot = appDataRoot;
            _modulesRoot = Path.Combine(appDataRoot, "modules");
            _eventBus = eventBus;
            _catalogRegistry = catalogRegistry;
            _loggerFactory = loggerFactory;

            Directory.CreateDirectory(_modulesRoot);
        }

        public async Task LoadAllAsync(CancellationToken ct = default)
        {
            if (!Directory.Exists(_modulesRoot))
            {
                return;
            }

            foreach (var moduleDir in Directory.GetDirectories(_modulesRoot))
            {
                await LoadSingleModuleAsync(moduleDir, ct);
            }
        }

        private async Task LoadSingleModuleAsync(string moduleDir, CancellationToken ct)
        {
            var manifestPath = Path.Combine(moduleDir, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                return;
            }

            ModuleManifest manifest;
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, ct);
                manifest = JsonSerializer.Deserialize<ModuleManifest>(json)
                    ?? throw new InvalidOperationException("manifest.json deserialized into null");
            }
            catch (Exception ex)
            {
                _loggerFactory.CreateLogger<ModuleLoader>()
                    .LogError(ex, "Couldn't load manifest.json from {Dir}", moduleDir);
                return;
            }

            if (!Version.TryParse(manifest.SdkVersion, out var moduleSdkVersion) ||
                moduleSdkVersion.Major != CurrentSdkVersion.Major)
            {
                RegisterFailed(manifest, $"Incompatible SDK version! Module SDK version: {manifest.SdkVersion} | Core SDK version: {CurrentSdkVersion}");
                return;
            }

            var dllPath = Path.Combine(moduleDir, manifest.EntryAssembly);
            if (!File.Exists(dllPath))
            {
                RegisterFailed(manifest, $"Couldn't find entry assembly: {dllPath}");
                return;
            }

            ModuleLoadContext loadContext;
            IModule instance;

            try
            {
                loadContext = new ModuleLoadContext(dllPath);
                var assembly = loadContext.LoadFromAssemblyPath(dllPath);

                var type = assembly.GetType(manifest.EntryType)
                    ?? throw new InvalidOperationException(
                        $"Type {manifest.EntryType} wasn't found in {manifest.EntryAssembly}");

                instance = Activator.CreateInstance(type) as IModule
                    ?? throw new InvalidOperationException(
                        $"Type {manifest.EntryType} didn't implement IModule or dosn't have public constructor");
            }
            catch (Exception ex)
            {
                RegisterFailed(manifest, $"Error during assebly/type loading: {ex.Message}");
                return;
            }

            var moduleLogger = _loggerFactory.CreateLogger(manifest.Id);
            var moduleStorage = new ModuleStorage(manifest.Id, _appDataRoot);
            var context = new ModuleContextImpl(_eventBus, _catalogRegistry, moduleLogger, moduleStorage);

            var loaded = new LoadedModule
            {
                Manifest = manifest,
                Instance = instance,
                LoadContext = loadContext,
                Context = context,
                Status = ModuleStatus.Loaded
            };

            try
            {
                await instance.InitializeAsync(context, ct);

                RegisterStaticCatalog(manifest.Id, instance);
                RegisterDynamicCatalogSubscription(manifest.Id, instance);

                loaded.Status = ModuleStatus.Initialized;
                moduleLogger.LogInformation("Module {Id} initialized successfully", manifest.Id);
            }
            catch (Exception ex)
            {
                loaded.Status = ModuleStatus.Failed;
                loaded.ErrorMessage = ex.Message;
                moduleLogger.LogError(ex, "InitializeAsync failed for {Id}", manifest.Id);
            }

            _loadedModules[manifest.Id] = loaded;
        }

        private void RegisterFailed(ModuleManifest manifest, string error)
        {
            _loggerFactory.CreateLogger<ModuleLoader>().LogError("Module {Id} wasn't initialized: {Error}", manifest.Id, error);
        }

        private void RegisterStaticCatalog(string moduleId, IModule instance)
        {
            var events = instance.GetEventCatalog();
            var actions = instance.GetActionCatalog();

            if (_catalogRegistry is EventCatalogRegistry registryImpl)
            {
                registryImpl.RegisterStatic(moduleId, events, actions);
            }
        }

        private void RegisterDynamicCatalogSubscription(string moduleId, IModule instance)
        {
            if (instance is not IModuleDynamicCatalog dynamicModule)
                return;

            dynamicModule.CatalogChanged += (_, args) =>
            {
                if (_catalogRegistry is EventCatalogRegistry registryImpl)
                {
                    registryImpl.ApplyDynamicChange(moduleId, args);
                }
            };

            var initialInstances = dynamicModule.GetCurrentInstances();
            if (initialInstances.Count > 0 && _catalogRegistry is EventCatalogRegistry registry)
            {
                registry.ApplyDynamicChange(moduleId, new CatalogChangedEventArgs
                {
                    Kind = CatalogChangeKind.Added,
                    AffectedEntries = initialInstances
                });
            }
        }
    }
}

