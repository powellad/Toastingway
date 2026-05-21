using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;

using Toastingway.Windows;

namespace Toastingway;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ToastingwayPlugin : IDalamudPlugin
{
    private const string CommandName = "/tw";

    public Configuration Configuration { get; init; }

    public ItemManager ItemManager { get; init; }

    public readonly WindowSystem WindowSystem = new("Toastingway");

    private ConfigWindow ConfigWindow { get; init; }

    private InGameToastNotifier Notifier { get; init; }

    public ToastingwayPlugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        this.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Notifier = new InGameToastNotifier(this.Configuration);
        this.ItemManager = new ItemManager(this.Notifier, this.Configuration);

        this.ConfigWindow = new ConfigWindow(this);

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

        Service.GameInventory.InventoryChanged += this.ItemManager.OnItemChanged;
        Service.GameInventory.ItemAddedExplicit += this.ItemManager.OnItemAdded;
        Service.GameInventory.ItemMovedExplicit += this.ItemManager.OnItemMoved;
        Service.GameInventory.ItemRemovedExplicit += this.ItemManager.OnItemRemoved;

        Service.ClientState.Login += this.OnLogin;
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        this.ConfigWindow.Dispose();

        Service.CommandManager.RemoveHandler(CommandName);

        Service.PluginInterface.UiBuilder.Draw -= this.DrawUi;

        Service.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        Service.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleConfigUi;

        Service.GameInventory.ItemAddedExplicit -= this.ItemManager.OnItemAdded;
        Service.GameInventory.ItemChangedExplicit -= this.ItemManager.OnItemChanged;
        Service.GameInventory.ItemMovedExplicit -= this.ItemManager.OnItemMoved;
        Service.GameInventory.ItemRemovedExplicit -= this.ItemManager.OnItemRemoved;

        if (Service.ClientState.IsLoggedIn)
        {
            Service.Framework.RunOnFrameworkThread(OnLogin);
        }

        Service.ClientState.Login -= this.OnLogin;
    }

    private void OnLogin()
    {
        this.ItemManager.Init();
    }

    private void OnCommand(string command, string args)
    {
        this.ToggleConfigUi();
    }

    private void DrawUi() => this.WindowSystem.Draw();

    public void ToggleConfigUi() => this.ConfigWindow.Toggle();
}
