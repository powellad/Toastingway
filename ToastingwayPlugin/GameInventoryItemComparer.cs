using System.Collections.Generic;

using Dalamud.Game.Inventory;

namespace Toastingway;

public class SimpleGameInventoryItemComparer : IEqualityComparer<GameInventoryItem>
{
    public bool Equals(GameInventoryItem i1, GameInventoryItem i2)
    {
        return i1.ItemId == i2.ItemId
               && i1.BaseItemId == i2.BaseItemId;
    }

    public int GetHashCode(GameInventoryItem obj)
    {
        return 17 * (int)obj.ItemId * (int)obj.BaseItemId;
    }
}
