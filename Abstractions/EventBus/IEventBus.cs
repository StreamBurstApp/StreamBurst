using StreamBurst.Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.EventBus
{
    public interface IEventBus
    {
        void Publish(ModuleEvent moduleEvent);
        IDisposable Subscribe(string? sourceModuleId, string? eventTypem, Action<ModuleEvent> handler);
        IDisposable SubscribeAsync(string? sourceModuleId, string? eventType, Func<ModuleEvent, CancellationToken, Task> handler);
        ModuleEvent? GetLastValue(string sourceModuleId, string eventType);
    }
}

