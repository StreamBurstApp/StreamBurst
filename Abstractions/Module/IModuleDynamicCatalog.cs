using Abstractions.Catalog;
using Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Module
{
    public interface IModuleDynamicCatalog
    {
        IReadOnlyList<EventDescriptor> GetCurrentInstances();

        event EventHandler<CatalogChangedEventArgs> CatalogChanged;
    }
}
