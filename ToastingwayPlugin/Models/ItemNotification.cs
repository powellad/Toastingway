using Dalamud.Game.Inventory;

namespace Toastingway.Models;

public record ItemNotification(GameInventoryItem Item, uint Quantity);
