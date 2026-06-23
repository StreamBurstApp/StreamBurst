using StreamBurst.Abstractions.Catalog;
using StreamBurst.Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Module
{
    public interface IModuleDynamicCatalog
    {
        IReadOnlyList<EventDescriptor> GetCurrentInstances();

        event EventHandler<CatalogChangedEventArgs> CatalogChanged;
    }
}

