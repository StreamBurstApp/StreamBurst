using StreamBurst.Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Module
{
    public interface IModule
    {
        string Id { get; }
        string DisplayName { get; }
        Version Version { get; }
        IReadOnlyList<EventDescriptor> GetEventCatalog();
        IReadOnlyList<ActionDescriptor> GetActionCatalog();
        Task InitializeAsync(IModuleContext context, CancellationToken ct);
        Task ShutdownAsync(CancellationToken ct);
    }
}

