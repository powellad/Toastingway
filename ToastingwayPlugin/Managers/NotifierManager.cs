using Toastingway.Enums;
using Toastingway.Models;

namespace Toastingway;

public class NotifierManager(Configuration configuration)
{
    private Configuration Configuration { get; init; } = configuration;

    private DalamudToastNotifier DalamudToastNotifier { get; init; } = new(configuration);
    
    private InGameToastNotifier InGameToastNotifier { get; init; } = new(configuration);

    public void RequestShowItem(ItemNotification itemNotification)
    {
        switch (this.Configuration.NotifierProvider)
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
