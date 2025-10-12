namespace Claude4_5Terraria.Enums
{
    public enum ItemType
    {
        // Blocks
        Dirt,
        Grass,
        Stone,
        Copper,
        Iron,          // NEW: Iron ore
        Silver,
        Gold,          // NEW: Gold ore
        Platinum,
        Wood,
        Coal,

        // Resources
        Stick,
        CopperBar,
        IronBar,       // NEW: Iron bar
        SilverBar,
        GoldBar,       // NEW: Gold bar
        PlatinumBar,
        Slime,         // NEW: Slime drop from Ooze enemies

        // Placeable
        Torch,
        WoodCraftingBench,
        CopperCraftingBench,
        Acorn,
        WoodChest,
        SilverChest,
        MagicChest,
        Bed,

        // Consumables
        RecallPotion,  // Teleports to spawn, 3 uses

        // Tools
        None,
        WoodPickaxe,
        StonePickaxe,
        CopperPickaxe,
        IronPickaxe,   // NEW: Iron pickaxe
        SilverPickaxe,
        GoldPickaxe,   // NEW: Gold pickaxe
        PlatinumPickaxe,
        RunicPickaxe,  // Best pickaxe tier
        EmptyBucket,
        WaterBucket,
        LavaBucket,

        // Weapons
        WoodSword,
        WoodWand // NEW: Wood Wand for Magic System
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
                case ItemType.Iron: return TileType.Iron;
                case ItemType.Silver: return TileType.Silver;
                case ItemType.Gold: return TileType.Gold;
                case ItemType.Platinum: return TileType.Platinum;
                case ItemType.Wood: return TileType.Wood;
                case ItemType.Coal: return TileType.Coal;
                case ItemType.Torch: return TileType.Torch;
                case ItemType.WoodCraftingBench: return TileType.WoodCraftingBench;
                case ItemType.CopperCraftingBench: return TileType.CopperCraftingBench;
                case ItemType.WoodChest: return TileType.WoodChest;
                case ItemType.SilverChest: return TileType.SilverChest;
                case ItemType.MagicChest: return TileType.MagicChest;
                case ItemType.Bed: return TileType.Bed;
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
                case TileType.Iron: return ItemType.Iron;
                case TileType.Silver: return ItemType.Silver;
                case TileType.Gold: return ItemType.Gold;
                case TileType.Platinum: return ItemType.Platinum;
                case TileType.Wood: return ItemType.Wood;
                case TileType.Coal: return ItemType.Coal;
                case TileType.Torch: return ItemType.Torch;
                case TileType.WoodCraftingBench: return ItemType.WoodCraftingBench;
                case TileType.CopperCraftingBench: return ItemType.CopperCraftingBench;
                case TileType.WoodChest: return ItemType.WoodChest;
                case TileType.SilverChest: return ItemType.SilverChest;
                case TileType.MagicChest: return ItemType.MagicChest;
                case TileType.Bed: return ItemType.Bed;
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
                case ItemType.Torch:
                case ItemType.WoodCraftingBench:
                case ItemType.CopperCraftingBench:
                case ItemType.WoodChest:
                case ItemType.SilverChest:
                case ItemType.MagicChest:
                case ItemType.Bed:
                    return true;
                default:
                    return false; // WoodWand is not placeable and is caught here
            }
        }
    }
}