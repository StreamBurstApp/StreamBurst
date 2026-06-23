using StreamBurst.Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Settings
{
    public class SettingDescriptor
    {
        public required string Key { get; init; }
        public required string DisplayName { get; init; }
        public required SettingType Type { get; init; }
        public ValueConstraint? Constraint { get; init; }
    }
}

