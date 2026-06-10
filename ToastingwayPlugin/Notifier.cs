using System.IO;

using Dalamud.Game.Inventory;

using Lumina.Excel.Sheets;

namespace Toastingway;

public abstract class Notifier
{
    protected Item LuminaItem { get; set; }
    
    protected GameInventoryItem Item { get; set; }
    
    protected uint Quantity { get; set; }

    protected string QuantityString => Quantity > 1 ? $" ({Quantity:N0})" : string.Empty;
    
    protected string HqString => Item.IsHq ? " (HQ)" : string.Empty;
    
    protected string ToastMessage => $"{LuminaItem.Name}{HqString}{QuantityString}";
    
    protected ushort Icon => this.LuminaItem.Icon;

    protected void SetLuminaItem(GameInventoryItem item)
    {
        // Check item ID. Generally will fail if item is HQ or collectible.
        var isFound = Service.DataManager.GetExcelSheet<Item>().TryGetRow(item.ItemId, out var excelItem);

        if (isFound)
        {
            this.LuminaItem = excelItem;
            return;
        }

        Service.PluginLog.Debug($"Couldn't find item: {item.ItemId}. Searching for base item instead.");

        // Try the base item ID instead.
        var isBaseFound = Service.DataManager.GetExcelSheet<Item>().TryGetRow(item.BaseItemId, out var baseItem);

        if (isBaseFound)
        {
            this.LuminaItem = baseItem;
            return;
        }

        Service.PluginLog.Debug($"Couldn't find base item: {item.ItemId}.");
        throw new InvalidDataException($"Couldn't find base item: {item.ItemId}.");
    }
    
    protected abstract void ShowNotification();

    public void ShowItem(GameInventoryItem item, uint quantity)
    {
        Service.PluginLog.Verbose($"Wanting to show item ID: {item}, quantity: {quantity}, HQ: {item.IsHq}.");

        if (item.ItemId == 0)
        {
            Service.PluginLog.Information($"Skipping toast. Invalid item ID given: {item}.");
            return;
        }

        SetLuminaItem(item);
        this.Item = item;

        Service.PluginLog.Verbose($"Showing: {this.ToastMessage}");
        
        this.ShowNotification();
    }
}
