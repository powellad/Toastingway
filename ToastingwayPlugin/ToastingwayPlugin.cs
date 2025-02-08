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

using FFXIVClientStructs.FFXIV.Client.Game;

using System.Linq;

using Lumina.Excel.Sheets;

namespace Toastingway;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class ToastingwayPlugin : IDalamudPlugin
{
    private const string CommandName = "/tw";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Toastingway");

    private ConfigWindow ConfigWindow { get; init; }

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

    public ToastingwayPlugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Service>();
        this.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

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

        Service.GameInventory.InventoryChanged += this.OnItemChanged;
        Service.GameInventory.ItemAddedExplicit += this.OnItemAdded;
        Service.GameInventory.ItemMovedExplicit += this.OnItemMoved;
        Service.GameInventory.ItemRemovedExplicit += this.OnItemRemoved;

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

        Service.GameInventory.ItemAddedExplicit -= this.OnItemAdded;
        Service.GameInventory.ItemChangedExplicit -= this.OnItemChanged;
        Service.GameInventory.ItemMovedExplicit -= this.OnItemMoved;
        Service.GameInventory.ItemRemovedExplicit -= this.OnItemRemoved;
        
        if (Service.ClientState.IsLoggedIn) {
            Service.Framework.RunOnFrameworkThread(OnLogin);
        }

        Service.ClientState.Login -= this.OnLogin;
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
        Service.PluginLog.Debug($"Processing bag {bag->Type}.");
        for (var index = 0; index < bag->Size; index++)
        {
            var item = bag->Items[index];

            if (item.ItemId > 0)
            {
                Service.PluginLog.Verbose($"Adding item:Item {item.ItemId}, HQ: {item.IsHighQuality()}, Quantity: {item.Quantity}");
                this.UpdateCount(item.ItemId, (uint)item.Quantity, false, item.IsHighQuality());
            }
        }
    }

    private void UpdateCount(uint itemId, uint quantity, bool replace = true, bool hq = false)
    {
        if (itemId > 0)
        {
            if (!this.inMemoryCounts.TryAdd((itemId, hq), quantity))
            {
                if (!replace)
                {
                    this.inMemoryCounts[(itemId, hq)] += quantity;
                }
                else
                {
                    this.inMemoryCounts[(itemId, hq)] = quantity;
                }
            }
        }
    }

    private void OnCommand(string command, string args)
    {
        this.ToggleConfigUi();
    }

    private void DrawUi() => this.WindowSystem.Draw();

    public void ToggleConfigUi() => this.ConfigWindow.Toggle();

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
        Service.PluginLog.Verbose(
            $"OnItemRemoved: Item {item.ItemId}, HQ: {item.IsHq}, Quantity: {item.Quantity} into bag {item.ContainerType}: removed from {data.Inventory}");
        this.inMemoryCounts[(item.ItemId, item.IsHq)] = 0;
    }

    private void OnItemMoved(InventoryItemMovedArgs data)
    {
        var item = data.Item;

        if ((data.TargetInventory == data.SourceInventory) ||
            (this.IsPlayerInventory(data.TargetInventory) && (this.IsPlayerInventory(data.SourceInventory))))
        {
            return;
        }

        Service.PluginLog.Verbose(
            $"OnItemMoved: Item {item.ItemId}, HQ: {item.IsHq}, Quantity: {item.Quantity}: moved from {data.SourceInventory} to {data.TargetInventory}");
        if (this.IsPlayerInventory(data.SourceInventory))
        {
            Service.PluginLog.Verbose($"OnItemMoved: Removed count for Item {item.ItemId}.");
            this.inMemoryCounts[(item.ItemId, item.IsHq)] = 0;
        }
        else if (this.IsPlayerInventory(data.TargetInventory))
        {
            Service.PluginLog.Verbose($"OnItemMoved: Update count for Item {item.ItemId} ({item.Quantity}).");
            this.inMemoryCounts[(item.ItemId, item.IsHq)] = (uint)item.Quantity;
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
            Service.PluginLog.Verbose(
                $"OnItemAdded: Item {args.Item.ItemId}, HQ: {args.Item.IsHq}, Quantity: {args.Item.Quantity} into bag {args.Item.ContainerType}");

            this.UpdateCount(args.Item.ItemId, (uint)args.Item.Quantity);

            this.HandleItemDisplay(args.Item.ItemId, args.Item.IsHq);
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
            Service.PluginLog.Verbose(
                $"OnItemChanged: Item {args.Item.ItemId}, HQ: {args.Item.IsHq}, changed by {args.Item.Quantity} into bag {args.Item.ContainerType}");

            var currentCount = this.inMemoryCounts.GetValueOrDefault((args.Item.ItemId, args.Item.IsHq));
            this.UpdateCount(args.Item.ItemId, (uint)args.Item.Quantity);

            var difference = args.Item.Quantity - (int)currentCount;

            Service.PluginLog.Verbose($"Quantity change: Current: {currentCount}, HQ: {args.Item.IsHq}, New: {args.Item.Quantity}");
            if (difference > 0) // Means something has been gained, so show the toast.
            {
                this.HandleItemDisplay(args.Item.ItemId, args.Item.IsHq);
            }
        }
    }

    private void HandleItemDisplay(uint itemId, bool isHq)
    {
        var item = Service.DataManager.GetExcelSheet<Item>().GetRow(itemId);
        var quantity = this.inMemoryCounts.GetValueOrDefault((itemId, isHq));

        if (quantity == 0)
        {
            Service.PluginLog.Debug($"Skipping toast. Couldn't find item: {itemId}.");
            return;
        }

        var quantityString = quantity is > 1 or 0 ? $" ({quantity:N0})" : string.Empty;
        var hqString = isHq ? " (HQ)" : string.Empty;

        Service.PluginLog.Verbose($"Showing: {item.Name}, HQ: {isHq}, with quantity {quantityString}");
        Service.ToastGui.ShowQuest(
            $"{item.Name}{hqString}{quantityString}",
            new QuestToastOptions
                { IconId = item.Icon, PlaySound = false, Position = this.Configuration.ToastPosition });
    }
}
