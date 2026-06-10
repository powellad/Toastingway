using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

using Toastingway.Enums;

namespace Toastingway.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Dictionary<NotifierProvider, string> providerTitles = new()
    {
        { NotifierProvider.InGame, "FFXIV" },
        { NotifierProvider.DalamudDefault, "Dalamud" }
    };

    public ConfigWindow(ToastingwayPlugin plugin) : base("Toastingway Config")
    {
        this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                     ImGuiWindowFlags.NoScrollWithMouse;

        this.Size = new Vector2(330, 170);
        this.SizeCondition = ImGuiCond.Always;
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
        var configuration = Service.Configuration;
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

        ImGui.Separator();

        var selectedProvider = (int)configuration.NotifierProvider;
        var notifierNames = Enum.GetValues<NotifierProvider>().Select(v => this.providerTitles[v]).ToList();
        if (ImGui.Combo("Toast provider", ref selectedProvider, notifierNames, notifierNames.Count))
        {
            configuration.NotifierProvider = (NotifierProvider)selectedProvider;
            configuration.Save();
        }

        if (configuration.NotifierProvider == NotifierProvider.InGame)
        {
            var selectedPosition = (int)configuration.ToastPosition;
            var positionNames = Enum.GetNames<QuestToastPosition>();
            if (ImGui.Combo("Toast position", ref selectedPosition, positionNames, positionNames.Length))
            {
                configuration.ToastPosition = (QuestToastPosition)selectedPosition;
                configuration.Save();
            }
        }
    }
}
