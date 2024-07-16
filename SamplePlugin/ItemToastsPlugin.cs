using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Inventory;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System;
//using Lumina.Excel.GeneratedSheets2;


namespace SamplePlugin;

public sealed class ItemToastsPlugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IToastGui ToastGui { get; private set; } = null!;
    [PluginService] internal static IAddonEventManager EventManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameInventory GameInventory { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;


    private const string CommandName = "/it";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("ItemToastsPlugin");
    private ConfigWindow ConfigWindow { get; init; }
    
    public ItemToastsPlugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Some simple toasts to display new items, crystals, currency, and reputation."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        // Always open config.
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        GameInventory.InventoryChanged += OnItemChanged;
        //GameInventory.ItemAddedExplicit += OnItemAdded;

    private void OnItemChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var item in events)
        {
            //PluginLog.Info($"Item change type: {item.Type}");
            OnItemChanged(item);
        }
    }

    private void OnItemChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var item in events)
        {
            //PluginLog.Info($"Item change type: {item.Type}");
            OnItemChanged(item);
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        GameInventory.ItemAddedExplicit -= OnItemAdded;
        GameInventory.ItemChangedExplicit -= OnItemChanged;
    }

    private void OnCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private bool ShouldShow(GameInventoryType inventory)
    {
        // TODO: Missing rep still
        // TODO: Commendations?
        return ShouldShowInventory(inventory) ||
            ShouldShowCurrency(inventory) ||
            ShouldShowCrystals(inventory) ||
            ShouldShowKeyItems(inventory);
    }

    private bool ShouldShowInventory(GameInventoryType incomingType)
    {
        return Configuration.ShowInventory &&
            (incomingType == GameInventoryType.Inventory1 ||
            incomingType == GameInventoryType.Inventory2 ||
            incomingType == GameInventoryType.Inventory3 ||
            incomingType == GameInventoryType.Inventory4);
    }

    private bool ShouldShowCurrency(GameInventoryType incomingType)
    {
        return Configuration.ShowCurrency && incomingType == GameInventoryType.Currency;
    }

    private bool ShouldShowCrystals(GameInventoryType incomingType)
    {
        return Configuration.ShowCrystals && incomingType == GameInventoryType.Crystals;
    }

    private bool ShouldShowKeyItems(GameInventoryType incomingType)
    {
        return incomingType == GameInventoryType.KeyItems;
        //return Configuration.ShowKeyItems && incomingType == GameInventoryType.KeyItems;
    }

    private void OnItemAdded(InventoryItemAddedArgs args)
    {
        if (ShouldShow(args.Inventory))
        {
            PluginLog.Info($"Added: Item type: {args.Type}");
            HandleItemDisplay(args.Item.ItemId, args.Item.Quantity);
        }
    }

    private void OnItemChanged(InventoryEventArgs args)
    {
        PluginLog.Debug($"Changed: Item type: {args}");
        if (ShouldShow(args.Item.ContainerType) && args.Type == GameInventoryEvent.Changed)
        {
            PluginLog.Info($"Changed: Item type: {args.Type} by {args.Item.Quantity}");
            HandleItemDisplay(args.Item.ItemId, args.Item.Quantity);
        }
    }

    private void HandleItemDisplay(uint itemId, uint quantity)
    {
        var item = DataManager.GetExcelSheet<Item>()?.GetRow(itemId);

        if (item is null)
        {
            return;
        }

        var quantityString = quantity > 1 ? $"({quantity})" : string.Empty;

        ToastGui.ShowQuest($"{item.Name} {quantityString}", new QuestToastOptions { IconId = item.Icon, PlaySound = false, Position = QuestToastPosition.Left });
    }
}
