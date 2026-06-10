using Toastingway.Enums;
using Toastingway.Models;

namespace Toastingway;

public class NotifierManager
{
    private DalamudToastNotifier DalamudToastNotifier { get; init; } = new();
    
    private InGameToastNotifier InGameToastNotifier { get; init; } = new();

    public void RequestShowItem(ItemNotification itemNotification)
    {
        switch (Service.Configuration.NotifierProvider)
        {
            case NotifierProvider.InGame:
                this.InGameToastNotifier.ShowItem(itemNotification);
                return;
            
            case NotifierProvider.DalamudDefault:
                this.DalamudToastNotifier.ShowItem(itemNotification);
                return;
            default:
                // Shouldn't happen.
                return;
        }
    }
}
