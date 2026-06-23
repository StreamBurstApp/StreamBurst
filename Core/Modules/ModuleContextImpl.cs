using StreamBurst.Abstractions.Catalog;
using StreamBurst.Abstractions.EventBus;
using StreamBurst.Abstractions.Module;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Modules
{
    internal sealed class ModuleContextImpl : IModuleContext
    {
        public IEventBus EventBus { get; }
        public IEventCatalogRegistry CatalogRegistry { get; }
        public ILogger Logger { get; }
        public IModuleStorage ModuleStorage { get; }

        public ModuleContextImpl(
            IEventBus eventBus,
            IEventCatalogRegistry catalogRegistry,
            ILogger logger,
            IModuleStorage storage)
        {
            EventBus = eventBus;
            CatalogRegistry = catalogRegistry;
            Logger = logger;
            ModuleStorage = storage;
        }
    }
}

