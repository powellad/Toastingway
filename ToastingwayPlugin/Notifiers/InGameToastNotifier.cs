using Dalamud.Game.Gui.Toast;

namespace Toastingway;

public sealed class InGameToastNotifier : Notifier
{
    protected override void ShowNotification()
    {
        Service.ToastGui.ShowQuest(
            this.ToastMessage,
            new QuestToastOptions
                { IconId = Icon, PlaySound = false, Position = Service.Configuration.ToastPosition });
    }
}
