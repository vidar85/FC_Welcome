using Dalamud.Configuration;
using Dalamud.Plugin;
using FCWelcome;
using System;
using System.Collections.Generic;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public MessageTypeSettings LoginMessages { get; set; } = new MessageTypeSettings();
    public MessageTypeSettings JoinMessages { get; set; } = new MessageTypeSettings();

    public MessageTypeSettings ReLoginMessages { get; set; } = new MessageTypeSettings();

    public int ReLoginThresholdMinutes { get; set; } = 30;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
