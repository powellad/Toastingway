using Dalamud.Game.Inventory;

namespace Toastingway;

public interface INotifier
{
    void ShowItem(GameInventoryItem item, uint quantity);
}
