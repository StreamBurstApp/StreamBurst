using Abstractions.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Catalog
{
    public sealed class CatalogChangedEventArgs : EventArgs
    {
        public required CatalogChangeKind Kind { get; init; }
        public required IReadOnlyList<EventDescriptor> AffectedEntries { get; init; }
    }

    public enum CatalogChangeKind
    {
        Added,
        Removed,
        Modified
    }
}
