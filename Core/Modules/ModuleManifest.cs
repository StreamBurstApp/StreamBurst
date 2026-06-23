using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Core.Modules
{
    internal sealed class ModuleManifest
    {
        [JsonPropertyName("id")]
        public required string Id { get; init; }

        [JsonPropertyName("displayName")]
        public required string DisplayName { get; init; }

        [JsonPropertyName("version")]
        public required string Version { get; init; }

        [JsonPropertyName("entryAssembly")]
        public required string EntryAssembly { get; init; }

        [JsonPropertyName("entryType")]
        public required string EntryType { get; init; }

        [JsonPropertyName("sdkVersion")]
        public required string SdkVersion { get; init; }
    }
}
