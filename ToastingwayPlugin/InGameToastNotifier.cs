using System;

using Dalamud.Game.Gui.Toast;

using Lumina.Excel.Sheets;

namespace Toastingway;

public class InGameToastNotifier(Configuration configuration) : INotifier
{
    private Configuration Configuration { get; init; } = configuration;

    public void ShowItem(uint itemId, uint quantity, bool isHq)
    {
        Service.PluginLog.Verbose($"Wanting to show item ID: {itemId}, quantity: {quantity}, HQ: {isHq}.");
        
        if (itemId == 0)
        {
            Service.PluginLog.Debug($"Skipping toast. Item ID of zero given: {itemId}.");
            return;
        }
        
        try
        {
            var isFound = Service.DataManager.GetExcelSheet<Item>().TryGetRow(itemId, out var item);
            
            if (!isFound)
            {
                Service.PluginLog.Debug($"Couldn't find item: {itemId}.");
                return;
            }
            
            Service.PluginLog.Verbose($"Searched for item. Found: Name: {item.Name} Icon: {item.Icon}");
        
            if (item.Icon == 0)
            {
                Service.PluginLog.Debug($"Couldn't find icon for item: {itemId}. Using gil icon instead.");
            }
            
            var quantityString = quantity > 1 ? $" ({quantity:N0})" : string.Empty;
            var hqString = isHq ? " (HQ)" : string.Empty;
            var icon = item.Icon == 0 ? (ushort)1 : item.Icon;

            Service.PluginLog.Verbose($"Showing: {item.Name}, HQ: {isHq}{quantityString}");
            Service.ToastGui.ShowQuest(
                $"{item.Name}{hqString}{quantityString}",
                new QuestToastOptions
                    { IconId = icon, PlaySound = false, Position = this.Configuration.ToastPosition });
        }
        catch (Exception e)
        {
            Service.PluginLog.Verbose($"Error looking up: {itemId}.");
            Service.PluginLog.Verbose($"Exception: {e}.");
        }
    } 
}
