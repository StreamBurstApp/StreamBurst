using Abstractions.Event;
using Abstractions.EventBus;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace Infrastructure.EventBus
{
    public sealed class EventBus : IEventBus, IDisposable
    {
        private const int MaxConsecutiveErrors = 5;

        private sealed record Subscription(
        string? SourceModuleIdFilter,
        string? EventTypePattern,
        Func<ModuleEvent, CancellationToken, Task> Handler);

        private readonly Channel<ModuleEvent> _channel = Channel.CreateUnbounded<ModuleEvent>();
        private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();
        private readonly ConcurrentDictionary<(string, string), ModuleEvent> _lastValues = new();
        private readonly ConcurrentDictionary<Guid, int> _errorCounts = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _dispatchLoopTask;
        private readonly ILogger<EventBus> _logger;

        public EventBus(ILogger<EventBus> logger)
        {
            _logger = logger;
            _dispatchLoopTask = Task.Run(() => DispatchLoopAsync(_cts.Token));
        }

        public void Publish(ModuleEvent ev)
        {
            if (ev.ValueKind != EventValueKind.Trigger)
            {
                _lastValues[(ev.SourceModuleId, ev.EventType)] = ev;
            }

            _channel.Writer.TryWrite(ev);
        }

        public IDisposable Subscribe(string? sourceModuleIdFilter, string? eventTypePattern, Action<ModuleEvent> handler)
        {
            return SubscribeAsync(sourceModuleIdFilter, eventTypePattern, (ev, _) =>
            {
                handler(ev);
                return Task.CompletedTask;
            });
        }

        public IDisposable SubscribeAsync(string? sourceModuleIdFilter, string? eventTypePattern,
            Func<ModuleEvent, CancellationToken, Task> handler)
        {
            var id = Guid.NewGuid();
            _subscriptions[id] = new Subscription(sourceModuleIdFilter, eventTypePattern, handler);
            return new Unsubscriber(() => _subscriptions.TryRemove(id, out _));
        }

        public ModuleEvent? GetLastValue(string sourceModuleId, string eventType) =>
            _lastValues.TryGetValue((sourceModuleId, eventType), out var ev) ? ev : null;

        private async Task DispatchLoopAsync(CancellationToken ct)
        {
            await foreach (var ev in _channel.Reader.ReadAllAsync(ct))
            {
                foreach (var (subId, sub) in _subscriptions)
                {
                    if (!Matches(sub, ev)) continue;

                    _ = InvokeSafelyAsync(subId, sub, ev, ct);
                }
            }
        }

        private async Task InvokeSafelyAsync(Guid subId, Subscription sub, ModuleEvent ev, CancellationToken ct)
        {
            try
            {
                await sub.Handler(ev, ct);
                _errorCounts.TryRemove(subId, out _);
            }
            catch (Exception ex)
            {
                var consecutiveErrors = _errorCounts.AddOrUpdate(subId, 1, (_, prev) => prev + 1);

                _logger.LogError(ex,
                    "EventBus: handler for subscription {SubId} (module filter: '{ModuleFilter}', event type: '{EventType}') threw an exception. " +
                    "Consecutive errors: {Count}/{Max}.",
                    subId,
                    sub.SourceModuleIdFilter ?? "*",
                    sub.EventTypePattern ?? "*",
                    consecutiveErrors,
                    MaxConsecutiveErrors);

                if (consecutiveErrors >= MaxConsecutiveErrors)
                {
                    _subscriptions.TryRemove(subId, out _);
                    _errorCounts.TryRemove(subId, out _);

                    _logger.LogWarning(
                        "EventBus: subscription {SubId} (module filter: '{ModuleFilter}', event type: '{EventType}') " +
                        "has been auto-disabled after {Max} consecutive errors.",
                        subId,
                        sub.SourceModuleIdFilter ?? "*",
                        sub.EventTypePattern ?? "*",
                        MaxConsecutiveErrors);
                }
            }
        }

        private static bool Matches(Subscription sub, ModuleEvent ev)
        {
            if (sub.SourceModuleIdFilter is not null && sub.SourceModuleIdFilter != ev.SourceModuleId)
                return false;

            if (sub.EventTypePattern is null)
                return true;

            if (sub.EventTypePattern.EndsWith('*'))
            {
                var prefix = sub.EventTypePattern[..^1];
                return ev.EventType.StartsWith(prefix, StringComparison.Ordinal);
            }

            return sub.EventTypePattern == ev.EventType;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _channel.Writer.TryComplete();
            try { _dispatchLoopTask.Wait(TimeSpan.FromSeconds(2)); } catch { /* shutdown best-effort */ }
            _cts.Dispose();
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public Unsubscriber(Action onDispose) => _onDispose = onDispose;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _onDispose();
            }
        }
    }
}
