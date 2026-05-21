using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

namespace Toastingway;

public class ItemManager(INotifier notifier, Configuration configuration)
{
    private INotifier Notifier { get; init; } = notifier;

    private Configuration Configuration { get; init; } = configuration;

    private static readonly SimpleGameInventoryItemComparer Comparer = new();

    private readonly Dictionary<GameInventoryItem, uint> inMemoryCounts = new(comparer: Comparer);

    private readonly IReadOnlyList<GameInventoryType> bagInventoryTypes =
    [
        GameInventoryType.Inventory1, GameInventoryType.Inventory2, GameInventoryType.Inventory3,
        GameInventoryType.Inventory4
    ];

    private readonly IReadOnlyList<GameInventoryType> allInventoryTypes =
    [
        GameInventoryType.Crystals, GameInventoryType.Currency, GameInventoryType.Inventory1,
        GameInventoryType.Inventory2, GameInventoryType.Inventory3, GameInventoryType.Inventory4
    ];

    private bool IsPlayerInventory(GameInventoryType type)
    {
        return this.bagInventoryTypes.Contains(type);
    }

    private void SetBagInventory(GameInventoryType bag)
    {
        var items = Service.GameInventory.GetInventoryItems(bag);

        Service.PluginLog.Debug($"SetBagInventory: Processing bag: {bag}.");
        foreach (var item in items)
        {
            Service.PluginLog.Debug(
                $"SetBagInventory: Adding item:Item {item.ItemId}, HQ: {item.IsHq}, Quantity: {item.Quantity}");
            this.UpdateCount(item, false);
        }
    }

    private void SetInventoryCounts()
    {
        foreach (var type in this.allInventoryTypes)
        {
            SetBagInventory(type);
        }
    }

    private void UpdateCount(GameInventoryItem item, bool replace)
    {
        var quantity = (uint)item.Quantity;

        Service.PluginLog.Verbose(
            this.inMemoryCounts.ContainsKey(item)
                ? $"UpdateCount: Dictionary contains Item: {item}"
                : $"UpdateCount: Dictionary does NOT contain: {item}");

        if (!this.inMemoryCounts.TryAdd(item, quantity))
        {
            if (!replace)
            {
                Service.PluginLog.Debug(
                    $"UpdateCount: Adding to existing item count. Item {item}");
                this.inMemoryCounts[item] += quantity;
            }
            else
            {
                Service.PluginLog.Debug(
                    $"UpdateCount: Replacing existing item count. Item {item}");
                this.inMemoryCounts[item] = quantity;
            }
        }
        else
        {
            Service.PluginLog.Debug(
                $"UpdateCount: Added new item: {item}");
        }
    }

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

    public void OnItemRemoved(InventoryItemRemovedArgs data)
    {
        var item = data.Item;

        if (!this.ShouldProcess(data.Inventory) || !this.IsPlayerInventory(data.Inventory))
        {
            return;
        }

        // Race condition here when discarding.
        Service.PluginLog.Verbose(
            $"OnItemRemoved: Item {item.BaseItemId}, HQ: {item.IsHq}, Quantity: {item.Quantity} into bag {item.ContainerType}: removed from {data.Inventory}");
        this.inMemoryCounts[item] = 0;
    }

    public void OnItemMoved(InventoryItemMovedArgs data)
    {
        var item = data.Item;

        if (data.TargetInventory == data.SourceInventory ||
            (this.IsPlayerInventory(data.TargetInventory) && this.IsPlayerInventory(data.SourceInventory)))
        {
            return;
        }

        Service.PluginLog.Verbose(
            $"OnItemMoved: Item {item.BaseItemId}, HQ: {item.IsHq}, Quantity: {item.Quantity}: moved from {data.SourceInventory} to {data.TargetInventory}");
        if (this.IsPlayerInventory(data.SourceInventory))
        {
            Service.PluginLog.Verbose($"OnItemMoved: Removed count for Item {item.BaseItemId}.");
            this.inMemoryCounts[item] = 0;
        }
        else if (this.IsPlayerInventory(data.TargetInventory))
        {
            Service.PluginLog.Verbose($"OnItemMoved: Update count for Item {item.BaseItemId} ({item.Quantity}).");
            this.inMemoryCounts[item] = (uint)item.Quantity;
        }
    }

    public void OnItemAdded(InventoryItemAddedArgs args)
    {
        if (!this.ShouldProcess(args.Inventory))
        {
            return;
        }

        if (this.ShouldShow(args.Inventory))
        {
            Service.PluginLog.Verbose(
                $"OnItemAdded: Item {args.Item}");

            this.UpdateCount(args.Item, true);

            this.HandleItemDisplay(args.Item);
        }
    }

    public void OnItemChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        foreach (var item in events)
        {
            this.OnItemChanged(item);
        }
    }

    public void OnItemChanged(InventoryEventArgs args)
    {
        if (!this.ShouldProcess(args.Item.ContainerType))
        {
            return;
        }

        if (this.ShouldShow(args.Item.ContainerType) && args.Type == GameInventoryEvent.Changed)
        {
            Service.PluginLog.Verbose(
                $"OnItemChanged: Item {args.Item.BaseItemId}, HQ: {args.Item.IsHq}, changed by {args.Item.Quantity} into bag {args.Item.ContainerType}");

            var currentCount = this.inMemoryCounts.GetValueOrDefault(args.Item);
            this.UpdateCount(args.Item, true);

            var difference = args.Item.Quantity - (int)currentCount;

            Service.PluginLog.Verbose(
                $"OnItemChanged: Quantity change: Current: {currentCount}, HQ: {args.Item.IsHq}, New: {args.Item.Quantity}");
            if (difference > 0) // Means something has been gained, so show the toast.
            {
                this.HandleItemDisplay(args.Item);
            }
        }
    }

    private void HandleItemDisplay(GameInventoryItem item)
    {
        var quantity = this.inMemoryCounts.GetValueOrDefault(item);
        this.Notifier.ShowItem(item, quantity);
    }

    public void Init()
    {
        this.SetInventoryCounts();
    }
}
