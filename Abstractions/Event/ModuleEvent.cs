using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Event
{
    public sealed record ModuleEvent
    {
        public required string SourceModuleId { get; init; }
        public required string EventType { get; init; }
        public required EventValueKind ValueKind { get; init; }
        public object? Value { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public IReadOnlyDictionary<string, string>? Tags { get; init; }
    }
}

