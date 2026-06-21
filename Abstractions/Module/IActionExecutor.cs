using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Module
{
    public interface IActionExecutor
    {
        Task ExecuteAsync(string actionId, IReadOnlyDictionary<string, object?>? parameters, CancellationToken ct);
    }
}
