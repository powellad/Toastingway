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
        this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        this.ConfigWindow = new ConfigWindow(this);

        this.WindowSystem.AddWindow(this.ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open configuration"
        });

        PluginInterface.UiBuilder.Draw += this.DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += this.ToggleConfigUI;

        GameInventory.InventoryChanged += this.OnItemChanged;
        GameInventory.ItemAddedExplicit += this.OnItemAdded;

        this.SetInventoryCounts();
    }

    private unsafe void SetInventoryCounts()
    {
        this.SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory1));
        this.SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory2));
        this.SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory3));
        this.SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Inventory4));
        this.SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Crystals));
        this.SetBagInventory(InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency));
    }

    private unsafe void SetBagInventory(InventoryContainer* bag)
    {
        PluginLog.Debug($"Processing bag {bag->Type}.");
        for (var index = 0; index < bag->Size; index++)
        {
            var item = bag->Items[index];
            this.UpdateCount(item.ItemId, item.Quantity, false);
        }
    }

    private unsafe void UpdateCount(uint itemId, uint quantity, bool replace = true)
    {
        if (itemId > 0)
        {
            if (!this.inMemoryCounts.TryAdd(itemId, quantity))
            {
                if (!replace)
                {
                    this.inMemoryCounts[itemId] += quantity;
                }
                else
                {
                    this.inMemoryCounts[itemId] = quantity;
                }
            }
        }
    }

    private void OnItemChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var item in events)
        {
            this.OnItemChanged(item);
        }
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        this.ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        GameInventory.ItemAddedExplicit -= this.OnItemAdded;
        GameInventory.ItemChangedExplicit -= this.OnItemChanged;
    }

    private void OnCommand(string command, string args)
    {
        this.ToggleConfigUI();
    }

    private void DrawUI() => this.WindowSystem.Draw();

    public void ToggleConfigUI() => this.ConfigWindow.Toggle();

    private bool ShouldShow(GameInventoryType inventory)
    {
        return this.ShouldShowInventory(inventory) ||
            this.ShouldShowCurrency(inventory) ||
            this.ShouldShowCrystals(inventory);
    }

    private bool ShouldShowInventory(GameInventoryType incomingType)
    {
        return this.Configuration.ShowInventory &&
            (incomingType == GameInventoryType.Inventory1 ||
            incomingType == GameInventoryType.Inventory2 ||
            incomingType == GameInventoryType.Inventory3 ||
            incomingType == GameInventoryType.Inventory4);
    }

    private bool ShouldShowCurrency(GameInventoryType incomingType)
    {
        return this.Configuration.ShowCurrency && incomingType == GameInventoryType.Currency;
    }

    private bool ShouldShowCrystals(GameInventoryType incomingType)
    {
        return this.Configuration.ShowCrystals && incomingType == GameInventoryType.Crystals;
    }

    private void OnItemAdded(InventoryItemAddedArgs args)
    {
        if (this.ShouldShow(args.Inventory))
        {
            PluginLog.Verbose($"OnItemAdded: Item {args.Item.ItemId}, Quantity: {args.Item.Quantity} into bag {args.Item.ContainerType}");

            this.UpdateCount(args.Item.ItemId, args.Item.Quantity);

            this.HandleItemDisplay(args.Item.ItemId);
        }
    }

    private void OnItemChanged(InventoryEventArgs args)
    {
        if (this.ShouldShow(args.Item.ContainerType) && args.Type == GameInventoryEvent.Changed)
        {
            PluginLog.Verbose($"OnItemChanged: Item {args.Item.ItemId} changed by {args.Item.Quantity} into bag {args.Item.ContainerType}");

            var currentCount = this.inMemoryCounts.GetValueOrDefault(args.Item.ItemId);
            this.UpdateCount(args.Item.ItemId, args.Item.Quantity);

            var difference = (int)args.Item.Quantity - (int)currentCount;

            PluginLog.Verbose($"Current: {currentCount}, New: {args.Item.Quantity}");
            if (difference > 0) // Means something has been gained, so show the toast.
            {
                this.HandleItemDisplay(args.Item.ItemId);
            }
        }
    }

    private void HandleItemDisplay(uint itemId)
    {
        var item = DataManager.GetExcelSheet<Item>()?.GetRow(itemId);
        var quantity = this.inMemoryCounts.GetValueOrDefault(itemId);

        if (item is null || quantity == 0)
        {
            PluginLog.Verbose($"Skipping toast. Couldn't find item: {itemId}.");
            return;
        }

        var quantityString = quantity > 1 ? $"({quantity:N0})" : string.Empty;

        PluginLog.Verbose($"Showing: {item.Name} with quantity {quantityString}");
        ToastGui.ShowQuest($"{item.Name} {quantityString}", new QuestToastOptions { IconId = item.Icon, PlaySound = false, Position = this.Configuration.ToastPosition });
    }
}
