using Dalamud.Configuration;
using Dalamud.Game.Gui.Toast;
using System;

using Toastingway.Enums;

namespace Toastingway;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ShowInventory { get; set; } = true;
    
    public bool ShowCurrency { get; set; } = true;

    public bool ShowCrystals { get; set; } = true;

    public QuestToastPosition ToastPosition { get; set; } = QuestToastPosition.Left;

    public NotifierProvider NotifierProvider { get; set; } = NotifierProvider.InGame;

    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }
}
