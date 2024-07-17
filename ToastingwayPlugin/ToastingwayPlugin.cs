using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Toastingway.Windows;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Inventory;
using System.Collections.Generic;
using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets;

// Wish list:
// 1. Beast tribe reputation and currency (not in Dalamud API yet)
// 2. Commendations (not in Dalamud API yet)
// 3. HQ/NQ distinction

// Current issues:
// 1. Double display on gathering
// 2. Venture coffers don't work
// 3. Collectibles not quite working. I think toasts need to work off my inMemory dict.

namespace Toastingway;

public sealed class ToastingwayPlugin : IDalamudPlugin
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


    private const string CommandName = "/tw";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("ToastingwayPlugin");
    private ConfigWindow ConfigWindow { get; init; }

    private readonly Dictionary<uint, uint> inMemoryCounts = [];
    
    public ToastingwayPlugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open configuration"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

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
            UpdateCount(item.ItemId, item.Quantity, false);
        }
    }

    private unsafe void UpdateCount(uint itemId, uint quantity, bool replace = true)
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
        return ShouldShowInventory(inventory) ||
            ShouldShowCurrency(inventory) ||
            ShouldShowCrystals(inventory);
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

    private void OnItemAdded(InventoryItemAddedArgs args)
    {
        if (ShouldShow(args.Inventory))
        {
            PluginLog.Verbose($"OnItemAdded: Item {args.Item.ItemId}, Quantity: {args.Item.Quantity} into bag {args.Item.ContainerType}");

            UpdateCount(args.Item.ItemId, args.Item.Quantity);

            HandleItemDisplay(args.Item.ItemId);
        }
    }

    private void OnItemChanged(InventoryEventArgs args)
    {
        if (ShouldShow(args.Item.ContainerType) && args.Type == GameInventoryEvent.Changed)
        {
            PluginLog.Verbose($"OnItemChanged: Item {args.Item.ItemId} changed by {args.Item.Quantity} into bag {args.Item.ContainerType}");

            var currentCount = inMemoryCounts.GetValueOrDefault(args.Item.ItemId);
            UpdateCount(args.Item.ItemId, args.Item.Quantity);

            var difference = (int)args.Item.Quantity - (int)currentCount;

            PluginLog.Verbose($"Current: {currentCount}, New: {args.Item.Quantity}");
            if (difference > 0) // Means something has been gained, so show the toast.
            {
                HandleItemDisplay(args.Item.ItemId);
            }

        }

    }

    private void HandleItemDisplay(uint itemId)
    {
        var item = DataManager.GetExcelSheet<Item>()?.GetRow(itemId);
        var quantity = inMemoryCounts.GetValueOrDefault(itemId);

        if (item is null || quantity == 0)
        {
            PluginLog.Verbose($"Skipping toast. Couldn't find item: {itemId}.");
            return;
        }

        var quantityString = quantity > 1 ? $"({quantity:N0})" : string.Empty;

        PluginLog.Verbose($"Showing: {item.Name} with quantity {quantityString}");
        ToastGui.ShowQuest($"{item.Name} {quantityString}", new QuestToastOptions { IconId = item.Icon, PlaySound = false, Position = Configuration.ToastPosition });
    }
}
