using System;
using System.Numerics;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Toastingway.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(ToastingwayPlugin plugin) : base("Toastingway Config")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(330, 155);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
    }

    public override void Draw()
    {
        var showInventory = configuration.ShowInventory;
        if (ImGui.Checkbox("Show toasts for new inventory items", ref showInventory))
        {
            configuration.ShowInventory = showInventory;
            configuration.Save();
        }

        var showCurrency = configuration.ShowCurrency;
        if (ImGui.Checkbox("Show toasts for new currency", ref showCurrency))
        {
            configuration.ShowCurrency = showCurrency;
            configuration.Save();
        }

        var showCrystals = configuration.ShowCrystals;
        if (ImGui.Checkbox("Show toasts for new crystals", ref showCrystals))
        {
            configuration.ShowCrystals = showCrystals;
            configuration.Save();
        }

        var selectedPosition = (int)configuration.ToastPosition;
        var positionNames = Enum.GetNames<QuestToastPosition>();        
        if (ImGui.Combo("Toast position", ref selectedPosition, positionNames, positionNames.Length))
        {
            configuration.ToastPosition = (QuestToastPosition)selectedPosition;
            configuration.Save();
        }        
    }
}
