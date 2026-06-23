using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Core.Modules;
using Infrastructure.Catalog;
using Infrastructure.EventBus;
using Microsoft.Extensions.Logging;
using StreamBurst.Logging;
using StreamBurst.ViewModels;
using StreamBurst.Views;
using System;
using System.IO;
using System.Linq;

namespace StreamBurst
{
    public partial class App : Application
    {
        private ILoggerFactory? _loggerFactory;
        internal ModuleLoader? ModuleLoader { get; private set; }
        public EventBus EventBus { get; private set; } = null!;


        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            InitializeModuleSystem();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                desktop.Exit += (sender, args) =>
                {
                    _loggerFactory?.Dispose();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void InitializeModuleSystem()
        {
            var appDataRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "StreamBurst");

            _loggerFactory = LoggerConfigurator.CreateLoggerFactory(appDataRoot);

            EventBus = new EventBus(_loggerFactory.CreateLogger<EventBus>());
            var catalogRegistry = new EventCatalogRegistry();

            ModuleLoader = new ModuleLoader(appDataRoot, EventBus, catalogRegistry, _loggerFactory);

            ModuleLoader.LoadAllAsync().GetAwaiter().GetResult();
        }
    }
}