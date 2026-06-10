using Dalamud.Game.Gui.Toast;

namespace Toastingway;

public sealed class InGameToastNotifier(Configuration configuration) : Notifier
{
    private Configuration Configuration { get; init; } = configuration;

    protected override void ShowNotification()
    {
        Service.ToastGui.ShowQuest(
            this.ToastMessage,
            new QuestToastOptions
                { IconId = Icon, PlaySound = false, Position = this.Configuration.ToastPosition });
    }
}
