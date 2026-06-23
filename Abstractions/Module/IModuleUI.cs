using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamBurst.Abstractions.Module
{
    public interface IModuleUI : IModule
    {
        Control CreateSettingsView();
    }
}

