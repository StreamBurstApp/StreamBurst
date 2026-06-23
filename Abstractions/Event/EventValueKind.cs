using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Event
{
    public enum EventValueKind
    {
        None,
        Bool,
        Number,
        String,
        Json,
        Trigger
    }
}

