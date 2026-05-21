using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;

using FFXIVClientStructs.FFXIV.Client.Game;

namespace Toastingway;

public class ItemManager(INotifier notifier, Configuration configuration)
{
    private INotifier Notifier { get; init; } = notifier;

    private Configuration Configuration { get; init; } = configuration;

    private readonly Dictionary<(uint, bool), uint> inMemoryCounts = new();

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
        Service.PluginLog.Debug($"Processing bag {bag->Type}.");
        for (var index = 0; index < bag->Size; index++)
        {
            var item = bag->Items[index];

            if (item.ItemId > 0)
            {
                Service.PluginLog.Debug($"Adding item:Item {item.ItemId}, HQ: {item.IsHighQuality()}, Quantity: {item.Quantity}");
                this.UpdateCount(item.ItemId, (uint)item.Quantity, false, item.IsHighQuality());
            }
        }
    }

    private void UpdateCount(uint itemId, uint quantity, bool replace, bool hq)
    {
        if (itemId > 0)
        {
            if (!this.inMemoryCounts.TryAdd((itemId, hq), quantity))
            {
                if (!replace)
                {
                    Service.PluginLog.Debug($"Update count: Adding. Item {itemId}, HQ: {hq}, Quantity: {quantity}");
                    this.inMemoryCounts[(itemId, hq)] += quantity;
                }
                else
                {
                    Service.PluginLog.Debug($"Update count: Replacing. Item {itemId}, HQ: {hq}, Quantity: {quantity}");
                    this.inMemoryCounts[(itemId, hq)] = quantity;
                }
            }
            else
            {
                Service.PluginLog.Verbose($"Failed adding: item {itemId}, HQ: {hq}, Quantity: {quantity}");
            }
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
        this.inMemoryCounts[(item.BaseItemId, item.IsHq)] = 0;
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
            this.inMemoryCounts[(item.BaseItemId, item.IsHq)] = 0;
        }
        else if (this.IsPlayerInventory(data.TargetInventory))
        {
            Service.PluginLog.Verbose($"OnItemMoved: Update count for Item {item.BaseItemId} ({item.Quantity}).");
            this.inMemoryCounts[(item.BaseItemId, item.IsHq)] = (uint)item.Quantity;
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
                $"OnItemAdded: Item {args.Item.BaseItemId}, HQ: {args.Item.IsHq}, Quantity: {args.Item.Quantity} into bag {args.Item.ContainerType}");

            this.UpdateCount(args.Item.BaseItemId, (uint)args.Item.Quantity, true, args.Item.IsHq);

            this.HandleItemDisplay(args.Item.BaseItemId, args.Item.IsHq);
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

            var currentCount = this.inMemoryCounts.GetValueOrDefault((args.Item.BaseItemId, args.Item.IsHq));
            this.UpdateCount(args.Item.BaseItemId, (uint)args.Item.Quantity, true, args.Item.IsHq);

            var difference = args.Item.Quantity - (int)currentCount;

            Service.PluginLog.Verbose($"Quantity change: Current: {currentCount}, HQ: {args.Item.IsHq}, New: {args.Item.Quantity}");
            if (difference > 0) // Means something has been gained, so show the toast.
            {
                this.HandleItemDisplay(args.Item.BaseItemId, args.Item.IsHq);
            }
        }
    }

    private void HandleItemDisplay(uint itemId, bool isHq)
    {
        var quantity = this.inMemoryCounts.GetValueOrDefault((itemId, isHq));
        this.Notifier.ShowItem(itemId, quantity, isHq);
    }

    public void Init()
    {
        this.SetInventoryCounts();
    }
}
