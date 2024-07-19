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
using System.Linq;

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
    [PluginService] public static IClientState ClientState { get; private set; } = null!;

    private const string CommandName = "/tw";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Toastingway");

    private ConfigWindow ConfigWindow { get; init; }

    private readonly Dictionary<uint, uint> inMemoryCounts = [];

    private readonly IReadOnlyList<GameInventoryType> bagInventoryTypes = [GameInventoryType.Inventory1, GameInventoryType.Inventory2, GameInventoryType.Inventory3, GameInventoryType.Inventory4];
    private readonly IReadOnlyList<GameInventoryType> allInventoryTypes = [GameInventoryType.Crystals, GameInventoryType.Currency, GameInventoryType.Inventory1, GameInventoryType.Inventory2, GameInventoryType.Inventory3, GameInventoryType.Inventory4];
    
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
        GameInventory.ItemMovedExplicit += this.OnItemMoved;
        GameInventory.ItemRemovedExplicit += this.OnItemRemoved;

        ClientState.Login += this.OnLogin;
    }

    public void Dispose()
    {
        this.WindowSystem.RemoveAllWindows();

        this.ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= this.DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= this.ToggleConfigUI;

        GameInventory.ItemAddedExplicit -= this.OnItemAdded;
        GameInventory.ItemChangedExplicit -= this.OnItemChanged;
        GameInventory.ItemMovedExplicit -= this.OnItemMoved;
        GameInventory.ItemRemovedExplicit -= this.OnItemRemoved;

        ClientState.Login -= this.OnLogin;
    }

    private void OnLogin()
    {
        this.SetInventoryCounts();
    }

    private bool IsPlayerInventory(GameInventoryType type)
    {
        return this.bagInventoryTypes.Contains(type);
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

            if (item.ItemId > 0)
            {
                PluginLog.Verbose($"Adding item: Item {item.ItemId}, Quantity: {item.Quantity}");
                this.UpdateCount(item.ItemId, item.Quantity, false);
            }
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

    private void OnCommand(string command, string args)
    {
        this.ToggleConfigUI();
    }

    private void DrawUI() => this.WindowSystem.Draw();

    public void ToggleConfigUI() => this.ConfigWindow.Toggle();

    private bool ShouldProcess(GameInventoryType inventory)
    {
        return this.allInventoryTypes.Contains(inventory);
    }

    private bool ShouldShow(GameInventoryType inventory)
    {
        return this.ShouldShowInventory(inventory) ||
            this.ShouldShowCurrency(inventory) ||
            this.ShouldShowCrystals(inventory);
    }

    private bool ShouldShowInventory(GameInventoryType incomingType)
    {
        return this.Configuration.ShowInventory && this.IsPlayerInventory(incomingType);
    }

    private bool ShouldShowCurrency(GameInventoryType incomingType)
    {
        return this.Configuration.ShowCurrency && incomingType == GameInventoryType.Currency;
    }

    private bool ShouldShowCrystals(GameInventoryType incomingType)
    {
        return this.Configuration.ShowCrystals && incomingType == GameInventoryType.Crystals;
    }

    private void OnItemRemoved(InventoryItemRemovedArgs data)
    {
        var item = data.Item;

        if (!this.ShouldProcess(data.Inventory) || !this.IsPlayerInventory(data.Inventory))
        {
            return;
        }

        // Race condition here when discarding.
        PluginLog.Verbose($"OnItemRemoved: Item {item.ItemId}, Quantity: {item.Quantity} into bag {item.ContainerType}: removed from {data.Inventory}");
        this.inMemoryCounts[item.ItemId] = 0;
    }

    private void OnItemMoved(InventoryItemMovedArgs data)
    {
        var item = data.Item;

        if ((data.TargetInventory == data.SourceInventory) || (this.IsPlayerInventory(data.TargetInventory) && (this.IsPlayerInventory(data.SourceInventory))))
        {
            return;
        }

        PluginLog.Verbose($"OnItemMoved: Item {item.ItemId}, Quantity: {item.Quantity}: moved from {data.SourceInventory} to {data.TargetInventory}");
        if (this.IsPlayerInventory(data.SourceInventory))
        {
            PluginLog.Verbose($"OnItemMoved: Removed count for Item {item.ItemId}.");
            this.inMemoryCounts[item.ItemId] = 0;
        }
        else if (this.IsPlayerInventory(data.TargetInventory))
        {
            PluginLog.Verbose($"OnItemMoved: Update count for Item {item.ItemId} ({item.Quantity}).");
            this.inMemoryCounts[item.ItemId] = item.Quantity;
        }
    }

    private void OnItemAdded(InventoryItemAddedArgs args)
    {
        if (!this.ShouldProcess(args.Inventory))
        {
            return;
        }

        if (this.ShouldShow(args.Inventory))
        {
            PluginLog.Verbose($"OnItemAdded: Item {args.Item.ItemId}, Quantity: {args.Item.Quantity} into bag {args.Item.ContainerType}");

            this.UpdateCount(args.Item.ItemId, args.Item.Quantity);

            this.HandleItemDisplay(args.Item.ItemId);
        }
    }

    private void OnItemChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var item in events)
        {
            this.OnItemChanged(item);
        }
    }

    private void OnItemChanged(InventoryEventArgs args)
    {
        if (!this.ShouldProcess(args.Item.ContainerType))
        {
            return;
        }

        if (this.ShouldShow(args.Item.ContainerType) && args.Type == GameInventoryEvent.Changed)
        {
            PluginLog.Verbose($"OnItemChanged: Item {args.Item.ItemId} changed by {args.Item.Quantity} into bag {args.Item.ContainerType}");

            var currentCount = this.inMemoryCounts.GetValueOrDefault(args.Item.ItemId);
            this.UpdateCount(args.Item.ItemId, args.Item.Quantity);

            var difference = (int)args.Item.Quantity - (int)currentCount;

            PluginLog.Verbose($"Quantity change: Current: {currentCount}, New: {args.Item.Quantity}");
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
            PluginLog.Debug($"Skipping toast. Couldn't find item: {itemId}.");
            return;
        }

        var quantityString = quantity > 1 || quantity == default ? $"({quantity:N0})" : string.Empty;

        PluginLog.Verbose($"Showing: {item.Name} with quantity {quantityString}");
        ToastGui.ShowQuest($"{item.Name} {quantityString}", new QuestToastOptions { IconId = item.Icon, PlaySound = false, Position = this.Configuration.ToastPosition });
    }
}
