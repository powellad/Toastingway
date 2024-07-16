using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Inventory;
using System.Collections.Generic;
using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

// Current issues:
// 1. Double display on gathering
// 2. Changed also means removed, which means it'll show when things are removed (e.g. gil or cereleum tanks)
// 3. Commendations doesn't seem possible
// 4. Is reputation possible?

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

    private readonly Dictionary<uint, uint> inMemoryCounts = [];
    
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

        //GameInventory.InventoryChangedRaw += OnItemChangedRaw;
        GameInventory.InventoryChanged += OnItemChanged;
        GameInventory.ItemAddedExplicit += OnItemAdded;

        SetInventoryCounts();
    }

    private unsafe void SetInventoryCounts()
    {
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1));
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2));
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3));
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4));
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Crystals));
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency));
        SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.KeyItems));
    }

    private unsafe void SetBagInventory(InventoryContainer* bag)
    {
        PluginLog.Debug($"Processing bag {bag->Type}.");
        for (var index = 0; index < bag->Size; index++)
        {
            var item = bag->Items[index];

            //PluginLog.Debug($"  Item: {item.ItemId}, Quantity: {item.Quantity}");
            UpdateCount(item.ItemId, item.Quantity);
        }
    }

    private unsafe void UpdateCount(uint itemId, uint quantity, bool replace = false)
    {
        if (itemId > 0)
        {
            if (!inMemoryCounts.TryAdd(itemId, quantity))
            {
                if (!replace)
                {
                    inMemoryCounts[itemId] += quantity;
                }
                else
                {
                    inMemoryCounts[itemId] = quantity;
                }
            }
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
        return Configuration.ShowKeyItems && incomingType == GameInventoryType.KeyItems;
    }

    private void OnItemAdded(InventoryItemAddedArgs args)
    {
        PluginLog.Verbose($"Added: Item type: {args.Type} by {args.Item.Quantity} in bag {args.Item.ContainerType}");
        if (ShouldShow(args.Inventory))
        {
            PluginLog.Verbose($"Showing: {args.Item.ItemId}");
            HandleItemDisplay(args.Item.ItemId, args.Item.Quantity);
        }

        UpdateCount(args.Item.ItemId, args.Item.Quantity, true);
    }

    private void OnItemChanged(InventoryEventArgs args)
    {
        PluginLog.Verbose($"Changed: Item type: {args.Type} by {args.Item.Quantity} in bag {args.Item.ContainerType}");
        if (ShouldShow(args.Item.ContainerType) && args.Type == GameInventoryEvent.Changed)
        {
            if (inMemoryCounts.TryGetValue(args.Item.ItemId, out var currentCount))
            {
                var difference = (int)args.Item.Quantity - (int)currentCount;

                PluginLog.Verbose($"Current: {currentCount}, New: {args.Item.Quantity}");
                if (difference > 0) // Means something has been gained, so show the toast.
                {
                    PluginLog.Verbose($"Showing: {args.Item.ItemId}");
                    HandleItemDisplay(args.Item.ItemId, args.Item.Quantity);
                }
            }
        }

        UpdateCount(args.Item.ItemId, args.Item.Quantity, true);
    }

    private static void HandleItemDisplay(uint itemId, uint quantity)
    {
        var item = DataManager.GetExcelSheet<Item>()?.GetRow(itemId);

        if (item is null)
        {
            PluginLog.Verbose($"Couldn't find item: {itemId}.");
            return;
        }

        var quantityString = quantity > 1 ? $"({quantity})" : string.Empty;

        ToastGui.ShowQuest($"{item.Name} {quantityString}", new QuestToastOptions { IconId = item.Icon, PlaySound = false, Position = QuestToastPosition.Left });
    }
}
