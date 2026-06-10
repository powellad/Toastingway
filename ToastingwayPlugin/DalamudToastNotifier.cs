using Dalamud.Interface.ImGuiNotification;

namespace Toastingway;

public class DalamudToastNotifier(Configuration configuration) : Notifier(configuration)
{
    protected override void ShowNotification()
    {        
        var notification = new Notification
        {
            Content = this.ToastMessage,
            Title = string.Empty
        };
        
        Service.NotificationManager.AddNotification(notification);
    }
}
