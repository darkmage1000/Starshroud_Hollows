namespace Claude4_5Terraria.Enums
{
    public enum ItemType
    {
        // Blocks (same as TileType for now)
        Dirt,
        Grass,
        Stone,
        Copper,
        Silver,
        Platinum,
        Wood,
        Coal,

        // Crafted Items
        Stick,
        Torch,
        WoodCraftingBench,
        WoodSword
    }

    public static class ItemTypeExtensions
    {
        public static TileType ToTileType(this ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Dirt: return TileType.Dirt;
                case ItemType.Grass: return TileType.Grass;
                case ItemType.Stone: return TileType.Stone;
                case ItemType.Copper: return TileType.Copper;
                case ItemType.Silver: return TileType.Silver;
                case ItemType.Platinum: return TileType.Platinum;
                case ItemType.Wood: return TileType.Wood;
                case ItemType.Coal: return TileType.Coal;
                case ItemType.WoodCraftingBench: return TileType.WoodCraftingBench;  // ADD THIS
                default: return TileType.Air;
            }
        }

        public static ItemType FromTileType(TileType tileType)
        {
            switch (tileType)
            {
                case TileType.Dirt: return ItemType.Dirt;
                case TileType.Grass: return ItemType.Grass;
                case TileType.Stone: return ItemType.Stone;
                case TileType.Copper: return ItemType.Copper;
                case TileType.Silver: return ItemType.Silver;
                case TileType.Platinum: return ItemType.Platinum;
                case TileType.Wood: return ItemType.Wood;
                case TileType.Coal: return ItemType.Coal;
                case TileType.WoodCraftingBench: return ItemType.WoodCraftingBench;  // ADD THIS
                default: return ItemType.Dirt;
            }
        }

        public static bool IsPlaceable(this ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Dirt:
                case ItemType.Grass:
                case ItemType.Stone:
                case ItemType.Wood:
                case ItemType.WoodCraftingBench:
                    return true;
                default:
                    return false;
            }
        }
    }
}