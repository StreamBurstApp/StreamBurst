using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Event
{
    public sealed record EventDescriptor
    {
        public required string EventType { get; init; }
        public required string DisplayName { get; init; }
        public required EventValueKind ValueKind { get; init; }
        public string? Description { get; init; }
        public ValueConstraint? Constraint { get; init; }
        public bool IsStatic { get; init; } = true;
    }
}

