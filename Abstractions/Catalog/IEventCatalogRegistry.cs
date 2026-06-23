using StreamBurst.Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Catalog
{
    public interface IEventCatalogRegistry
    {
        IReadOnlyList<EventDescriptor> GetAllEvents();
        IReadOnlyList<ActionDescriptor> GetAllActions();
        IReadOnlyList<EventDescriptor> GetEventsForModule(string moduleId);

        event EventHandler<CatalogChangedEventArgs> GlobalCatalogChanged;
    }
}

