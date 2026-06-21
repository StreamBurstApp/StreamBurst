using Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.EventBus
{
    public interface IEventBus
    {
        void Publish(ModuleEvent moduleEvent);
        IDisposable Subscribe(string? sourceModuleId, string? eventTypem, Action<ModuleEvent> handler);
        IDisposable SubscribeAsync(string? sourceModuleId, string? eventType, Func<ModuleEvent, Task> handler);
        ModuleEvent? GetLastValue(string sourceModuleId, string eventType);
    }
}
