using Abstractions.Module;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Storage
{
    public sealed class ModuleStorage : IModuleStorage
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private Dictionary<string, JsonElement>? _cache;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public ModuleStorage(string moduleId, string appDataRoot)
        {
            var moduleDir = Path.Combine(appDataRoot, "modules", moduleId);
            Directory.CreateDirectory(moduleDir);
            _filePath = Path.Combine(moduleDir, "storage.json");
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            await EnsureLoadedAsync(ct);

            if (_cache!.TryGetValue(key, out var element))
            {
                return element.Deserialize<T>(JsonOptions);
            }

            return default;
        }

        public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
        {
            await EnsureLoadedAsync(ct);

            await _fileLock.WaitAsync(ct);
            try
            {
                var json = JsonSerializer.SerializeToElement(value, JsonOptions);
                _cache![key] = json;
                await PersistToDiskAsync(ct);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task DeleteAsync(string key, CancellationToken ct = default)
        {
            await EnsureLoadedAsync(ct);

            await _fileLock.WaitAsync(ct);
            try
            {
                _cache!.Remove(key);
                await PersistToDiskAsync(ct);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task EnsureLoadedAsync(CancellationToken ct)
        {
            if (_cache is not null) return;

            await _fileLock.WaitAsync(ct);
            try
            {
                if (_cache is not null) return;

                if (File.Exists(_filePath))
                {
                    var json = await File.ReadAllTextAsync(_filePath, ct);
                    _cache = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                             ?? new Dictionary<string, JsonElement>();
                }
                else
                {
                    _cache = new Dictionary<string, JsonElement>();
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private async Task PersistToDiskAsync(CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(_cache, JsonOptions);

            var tempPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, ct);
            File.Move(tempPath, _filePath, overwrite: true);
        }
    }
}
