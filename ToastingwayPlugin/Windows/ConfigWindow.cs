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
        this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        this.Size = new Vector2(330, 155);
        this.SizeCondition = ImGuiCond.Always;

        this.configuration = plugin.Configuration;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void PreDraw()
    {
    }

    public override void Draw()
    {
        var showInventory = this.configuration.ShowInventory;
        if (ImGui.Checkbox("Show toasts for new inventory items", ref showInventory))
        {
            this.configuration.ShowInventory = showInventory;
            this.configuration.Save();
        }

        var showCurrency = this.configuration.ShowCurrency;
        if (ImGui.Checkbox("Show toasts for new currency", ref showCurrency))
        {
            this.configuration.ShowCurrency = showCurrency;
            this.configuration.Save();
        }

        var showCrystals = this.configuration.ShowCrystals;
        if (ImGui.Checkbox("Show toasts for new crystals", ref showCrystals))
        {
            this.configuration.ShowCrystals = showCrystals;
            this.configuration.Save();
        }

        var selectedPosition = (int)this.configuration.ToastPosition;
        var positionNames = Enum.GetNames<QuestToastPosition>();        
        if (ImGui.Combo("Toast position", ref selectedPosition, positionNames, positionNames.Length))
        {
            this.configuration.ToastPosition = (QuestToastPosition)selectedPosition;
            this.configuration.Save();
        }        
    }
}
