using Dalamud.Game.Inventory;

using Toastingway.Models;

namespace Toastingway;

public class NotifierManager(Configuration configuration)
{
    private Configuration Configuration { get; init; } = configuration;
    
    private InGameToastNotifier InGameToastNotifier { get; init; } = new(configuration);

    public void RequestShowItem(ItemNotification itemNotification)
    {
        this.InGameToastNotifier.ShowItem(itemNotification);
    }
}
