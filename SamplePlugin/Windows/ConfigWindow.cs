using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SamplePlugin.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(ItemToastsPlugin plugin) : base("Toastingway Config###ToastingwayID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 300);
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
    }
}
