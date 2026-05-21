using System.IO;

using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Inventory;

using Lumina.Excel.Sheets;

namespace Toastingway;

public class InGameToastNotifier(Configuration configuration) : INotifier
{
    private Configuration Configuration { get; init; } = configuration;

    private static Item GetLuminaItem(GameInventoryItem item)
    {
        // Check item ID. Generally will fail if item is HQ or collectible.
        var isFound = Service.DataManager.GetExcelSheet<Item>().TryGetRow(item.ItemId, out var excelItem);

        if (isFound)
        {
            return excelItem;
        }

        Service.PluginLog.Debug($"Couldn't find item: {item.ItemId}. Searching for base item instead.");

        // Try the base item ID instead.
        var isBaseFound = Service.DataManager.GetExcelSheet<Item>().TryGetRow(item.BaseItemId, out var baseItem);

        if (isBaseFound)
        {
            return baseItem;
        }

        Service.PluginLog.Debug($"Couldn't find base item: {item.ItemId}.");
        throw new InvalidDataException($"Couldn't find base item: {item.ItemId}.");
    }

    public void ShowItem(GameInventoryItem item, uint quantity)
    {
        Service.PluginLog.Verbose($"Wanting to show item ID: {item}, quantity: {quantity}, HQ: {item.IsHq}.");

        if (item.ItemId == 0)
        {
            Service.PluginLog.Information($"Skipping toast. Invalid item ID given: {item}.");
            return;
        }

        var luminaItem = GetLuminaItem(item);

        var quantityString = quantity > 1 ? $" ({quantity:N0})" : string.Empty;
        var hqString = item.IsHq ? " (HQ)" : string.Empty;

        Service.PluginLog.Verbose($"Showing: {luminaItem.Name}, HQ: {item.IsHq}{quantityString}");
        Service.ToastGui.ShowQuest(
            $"{luminaItem.Name}{hqString}{quantityString}",
            new QuestToastOptions
                { IconId = luminaItem.Icon, PlaySound = false, Position = this.Configuration.ToastPosition });
    }
}
