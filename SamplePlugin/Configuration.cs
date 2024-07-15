using Dalamud.Configuration;
using System;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ShowInventory { get; set; } = true;
    
    public bool ShowCurrency { get; set; } = true;

    public bool ShowCrystals { get; set; } = true;

    public bool ShowKeyItems { get; set; } = true;

    public bool ShowCommendations { get; set; } = false;

    public bool ShowReputation { get; set; } = false;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        ItemToastsPlugin.PluginInterface.SavePluginConfig(this);
    }
}
