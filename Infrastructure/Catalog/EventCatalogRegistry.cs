using Abstractions.Catalog;
using Abstractions.Event;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Catalog
{
    public sealed class EventCatalogRegistry : IEventCatalogRegistry
    {
        private readonly ConcurrentDictionary<(string ModuleId, string EventType), EventDescriptor> _events = new();
        private readonly ConcurrentDictionary<(string ModuleId, string ActionId), ActionDescriptor> _actions = new();

        public event EventHandler<CatalogChangedEventArgs>? GlobalCatalogChanged;

        public IReadOnlyList<EventDescriptor> GetAllEvents() => _events.Values.ToList();

        public IReadOnlyList<ActionDescriptor> GetAllActions() => _actions.Values.ToList();

        public IReadOnlyList<EventDescriptor> GetEventsForModule(string moduleId) =>
            _events.Where(kv => kv.Key.ModuleId == moduleId).Select(kv => kv.Value).ToList();

        public void RegisterStatic(string moduleId, IReadOnlyList<EventDescriptor> events, IReadOnlyList<ActionDescriptor> actions)
        {
            foreach (var ev in events)
                _events[(moduleId, ev.EventType)] = ev;

            foreach (var action in actions)
                _actions[(moduleId, action.ActionId)] = action;

            if (events.Count > 0)
            {
                GlobalCatalogChanged?.Invoke(this, new CatalogChangedEventArgs
                {
                    Kind = CatalogChangeKind.Added,
                    AffectedEntries = events
                });
            }
        }

        public void ApplyDynamicChange(string moduleId, CatalogChangedEventArgs args)
        {
            switch (args.Kind)
            {
                case CatalogChangeKind.Added:
                case CatalogChangeKind.Modified:
                    foreach (var ev in args.AffectedEntries)
                        _events[(moduleId, ev.EventType)] = ev;
                    break;

                case CatalogChangeKind.Removed:
                    foreach (var ev in args.AffectedEntries)
                        _events.TryRemove((moduleId, ev.EventType), out _);
                    break;
            }

            GlobalCatalogChanged?.Invoke(this, args);
        }
    }
}
