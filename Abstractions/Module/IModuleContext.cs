using StreamBurst.Abstractions.Catalog;
using StreamBurst.Abstractions.EventBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Module
{
    public interface IModuleContext
    {
        IEventBus EventBus { get; }
        IEventCatalogRegistry CatalogRegistry { get; }
        ILogger Logger { get; }
        IModuleStorage ModuleStorage { get; }
    }
}

