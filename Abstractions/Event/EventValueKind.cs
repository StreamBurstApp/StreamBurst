using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Event
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
