using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Event
{
    public sealed record ValueConstraint
    {
        public double? Min { get; init; }
        public double? Max { get; init; }
        public string[]? EnumValues { get; init; }
    }
}

