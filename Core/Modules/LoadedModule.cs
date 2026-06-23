using StreamBurst.Abstractions.Module;
using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Modules
{
    internal sealed class LoadedModule
    {
        public required ModuleManifest Manifest { get; init; }
        public required IModule Instance { get; init; }
        public required ModuleLoadContext LoadContext { get; init; }
        public required IModuleContext Context { get; init; }
        public ModuleStatus Status { get; set; } = ModuleStatus.Loaded;
        public string? ErrorMessage { get; set; }
    }

    public enum ModuleStatus
    {
        Loaded,
        Initialized,
        Failed,
        ShutDown
    }
}

