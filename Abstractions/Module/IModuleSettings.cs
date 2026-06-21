using Abstractions.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abstractions.Module
{
    public interface IModuleSettings
    {
        IReadOnlyList<SettingDescriptor> GetSettingsSchema();
        object GetSettingsState();
        void ApplySettingsState(object newState);
    }
}
