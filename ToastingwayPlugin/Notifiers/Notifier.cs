using System.IO;

using Lumina.Excel.Sheets;

using Toastingway.Models;

namespace Toastingway;

public abstract class Notifier
{
    protected Item LuminaItem { get; set; }
    
    protected ItemNotification? ItemNotification { get; set; }

    // Won't be null if it hits this part.
    protected string QuantityString => ItemNotification!.Quantity > 1 ? $" ({ItemNotification.Quantity:N0})" : string.Empty;
    
    // Won't be null if it hits this part.
    protected string HqString => ItemNotification!.Item.IsHq ? " (HQ)" : string.Empty;
    
    protected string ToastMessage => $"{LuminaItem.Name}{HqString}{QuantityString}";
    
    protected ushort Icon => this.LuminaItem.Icon;

    private void SetLuminaItem(GameInventoryItem item)
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

    public void ShowItem(ItemNotification itemNotification)
    {
        Service.PluginLog.Verbose(
            $"Wanting to show item ID: {itemNotification.Item}, quantity: {itemNotification.Quantity}, HQ: {itemNotification.Item.IsHq}.");

        if (itemNotification.Item.ItemId == 0)
        {
            Service.PluginLog.Information($"Skipping toast. Invalid item ID given: {itemNotification.Item}.");
            return;
        }

        SetLuminaItem(itemNotification.Item);
        this.ItemNotification = itemNotification;

        Service.PluginLog.Verbose($"Showing: {this.ToastMessage}");
        
        this.ShowNotification();
    }
}
