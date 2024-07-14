using System;
using System.Numerics;
using Dalamud.Game.Gui.Toast;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly ItemToastsPlugin plugin;
    private bool displayed = false;

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(ItemToastsPlugin plugin, string goatImagePath)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    private void ShowToast()
    {
        if (plugin != null && !displayed) {
            ItemToastsPlugin.ToastGui.ShowQuest("Test toast", new QuestToastOptions { IconId = 60858, PlaySound = false, Position = QuestToastPosition.Left });
            displayed = true;
        }
    }

    public void Dispose() { }

    public override void Draw()
    {
        ShowToast();            
    }
}
