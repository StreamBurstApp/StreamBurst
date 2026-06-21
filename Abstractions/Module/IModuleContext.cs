using Abstractions.Catalog;
using Abstractions.EventBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Module
{
    public interface IModuleContext
    {
        IEventBus EventBus { get; }
        IEventCatalogRegistry CatalogRegistry { get; }
        ILogger Logger { get; }
        IModuleStorage ModuleStorage { get; }
    }
}
