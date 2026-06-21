using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Event
{
    public sealed record ActionDescriptor
    {
        public required string ActionId { get; init; }
        public required string DisplayName { get; init; }
        public required ParamDescriptor[] Parameters { get; init; }
    }
}
