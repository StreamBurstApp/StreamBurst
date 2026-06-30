using Infrastructure.Catalog;
using Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using StreamBurst.Abstractions.Catalog;
using StreamBurst.Abstractions.EventBus;
using StreamBurst.Abstractions.Module;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        private readonly ILogger<ModuleLoader> _logger;

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
            _logger = loggerFactory.CreateLogger<ModuleLoader>();

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
            _logger.LogInformation("Loading module from {Dir}", moduleDir);

            var manifestPath = Path.Combine(moduleDir, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                _logger.LogError("manifest.json not found in {Dir}", moduleDir);
                return;
            }

            _logger.LogInformation("Found manifest.json in {Dir}", moduleDir);

            ModuleManifest manifest;
            try
            {
                var json = File.ReadAllText(manifestPath);
                manifest = JsonSerializer.Deserialize<ModuleManifest>(json)
                    ?? throw new InvalidOperationException("manifest.json deserialized into null");
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Couldn't load manifest.json from {Dir}", moduleDir);
                return;
            }

            _logger.LogInformation("Loaded manifest for module {Id}", manifest.Id);

            if (!Version.TryParse(manifest.SdkVersion, out var moduleSdkVersion) ||
                moduleSdkVersion.Major != CurrentSdkVersion.Major)
            {
                RegisterFailed(manifest, $"Incompatible SDK version! Module SDK version: {manifest.SdkVersion} | Core SDK version: {CurrentSdkVersion}");
                return;
            }

            _logger.LogInformation("Module {Id} has compatible SDK version: {Version}", manifest.Id, manifest.SdkVersion);

            var dllPath = Path.Combine(moduleDir, manifest.EntryAssembly);
            if (!File.Exists(dllPath))
            {
                RegisterFailed(manifest, $"Couldn't find entry assembly: {dllPath}");
                return;
            }

            _logger.LogInformation("Loading assembly {Assembly} for module {Id}", manifest.EntryAssembly, manifest.Id);

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

            _logger.LogInformation("Successfully loaded module {Id} from {Assembly}", manifest.Id, manifest.EntryAssembly);

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

            _logger.LogInformation("Module {Id} loaded with status {Status}", manifest.Id, loaded.Status);

            _loadedModules[manifest.Id] = loaded;
        }

        private void RegisterFailed(ModuleManifest manifest, string error)
        {
            _logger.LogError("Module {Id} wasn't initialized: {Error}", manifest.Id, error);
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

