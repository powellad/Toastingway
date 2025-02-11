using Dalamud.Game.Gui.Toast;

using Lumina.Excel.Sheets;

namespace Toastingway;

public class InGameToastNotifier(Configuration configuration) : INotifier
{
    private Configuration Configuration { get; init; } = configuration;

    public void ShowItem(uint itemId, uint quantity, bool isHq)
    {
        Service.PluginLog.Verbose($"Wanting to show item ID: {itemId}, quantity: {quantity}, HQ: {isHq}.");
        
        var item = Service.DataManager.GetExcelSheet<Item>().GetRow(itemId);
        var asdf = Service.DataManager.GetExcelSheet<Item>();
        Service.PluginLog.Verbose($"Looked for item. Found: Name: {item.Name} Icon: {item.Icon}");

        if (itemId == 0)
        {
            Service.PluginLog.Debug($"Skipping toast. Couldn't find item: {itemId}.");
            return;
        }

        var quantityString = quantity is > 1 ? $" ({quantity:N0})" : string.Empty;
        var hqString = isHq ? " (HQ)" : string.Empty;

        Service.PluginLog.Verbose($"Showing: {item.Name}, HQ: {isHq}, with quantity {quantityString}");
        Service.ToastGui.ShowQuest(
            $"{item.Name}{hqString}{quantityString}",
            new QuestToastOptions
                { IconId = item.Icon, PlaySound = false, Position = this.Configuration.ToastPosition });
    } 
}
