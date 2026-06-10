using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;

using Toastingway.Windows;

namespace Toastingway;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ToastingwayPlugin : IDalamudPlugin
{
    private const string CommandName = "/tw";

    public readonly WindowSystem WindowSystem = new("Toastingway");

    private ConfigWindow ConfigWindow { get; init; }

    public ToastingwayPlugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();

        Service.Configuration = new Configuration();
        Service.ItemManager = new ItemManager();
        Service.NotifierManager = new NotifierManager();
        
        this.ConfigWindow = new ConfigWindow();

        this.WindowSystem.AddWindow(this.ConfigWindow);

        Service.CommandManager.AddHandler(
            CommandName,
            new CommandInfo(this.OnCommand)
            {
                HelpMessage = "Open configuration"
            });

        Service.PluginInterface.UiBuilder.Draw += this.DrawUi;

        Service.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi += this.ToggleConfigUi;

        Service.GameInventory.InventoryChanged += Service.ItemManager.OnItemChanged;
        Service.GameInventory.ItemAddedExplicit += Service.ItemManager.OnItemAdded;
        Service.GameInventory.ItemMovedExplicit += Service.ItemManager.OnItemMoved;
        Service.GameInventory.ItemRemovedExplicit += Service.ItemManager.OnItemRemoved;

        Service.ClientState.Login += OnLogin;
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        this.ConfigWindow.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);

        Service.PluginInterface.UiBuilder.Draw -= this.DrawUi;

        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleConfigUi;

        Service.GameInventory.ItemAddedExplicit -= Service.ItemManager.OnItemAdded;
        Service.GameInventory.ItemChangedExplicit -= Service.ItemManager.OnItemChanged;
        Service.GameInventory.ItemMovedExplicit -= Service.ItemManager.OnItemMoved;
        Service.GameInventory.ItemRemovedExplicit -= Service.ItemManager.OnItemRemoved;

        if (Service.ClientState.IsLoggedIn)
        {
            Service.Framework.RunOnFrameworkThread(OnLogin);
        }

        Service.ClientState.Login -= OnLogin;
    }

    private static void OnLogin()
    {
        Service.ItemManager.Init();
    }

    private void OnCommand(string command, string args)
    {
        this.ToggleConfigUi();
    }

    private void DrawUi() => this.WindowSystem.Draw();

    public void ToggleConfigUi() => this.ConfigWindow.Toggle();
}
