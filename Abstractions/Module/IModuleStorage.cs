using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Module
{
    public interface IModuleStorage
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task SetAsync<T>(string key, T value, CancellationToken ct = default);
        Task DeleteAsync(string key, CancellationToken ct = default);
    }
}

