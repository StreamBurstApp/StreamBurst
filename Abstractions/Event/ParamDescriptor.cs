using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Event
{
    public sealed record ParamDescriptor
    {
        public required string Name { get; init; }
        public required string DisplayName { get; init; }
        public required EventValueKind ValueKind { get; init; }
        public bool IsRequired { get; init; } = true;
        public ValueConstraint? Constraint { get; init; }
    }
}

