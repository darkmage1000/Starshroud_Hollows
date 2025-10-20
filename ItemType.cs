namespace StarshroudHollows.Enums
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
        Obsidian,
        SnowWood,
        JungleWood,
        SwampWood,
        VolcanicWood,
       
        // Resources
        Stick,
        CopperBar,
        IronBar,       // NEW: Iron bar
        SilverBar,
        GoldBar,       // NEW: Gold bar
        PlatinumBar,
        Slime,         // NEW: Slime drop from Ooze enemies
        PieceOfFlesh,  // NEW: Boss summon item from Zombies
        TrollBait,     // Boss summon item - requires 25 Piece of Flesh
        TrollBar,      // Dropped by Cave Troll boss

        // Placeable
        Torch,
        WoodCraftingBench,
        CopperCraftingBench,
        Acorn,
        WoodChest,
        SilverChest,
        MagicChest,
        Bed,
        SummonAltar,   // NEW: Boss summoning altar (craftable, placeable)
        Door,          // NEW: Housing door

        // Background Walls (can also be placed as solid blocks)
        DirtWall,
        StoneWall,
        WoodWall,
        CopperWall,
        IronWall,
        SilverWall,
        GoldWall,
        PlatinumWall,
        SnowWall,

        // Consumables
        HealthPotion,  // Restores player health
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

        // Weapons - Swords
        WoodSword,
        CopperSword,
        IronSword,
        SilverSword,
        GoldSword,
        PlatinumSword,
        RunicSword,
        
        // Weapons - Wands
        WoodWand,
        FireWand,
        LightningWand,
        NatureWand,
        WaterWand,
        HalfMoonWand,
        RunicLaserWand,
        
        // Weapons - Staff
        WoodSummonStaff,
        
        // Tools - Hammer (for walls)
        Hammer,
        
        // Boss Weapons
        TrollClub,     // 5% drop from Cave Troll boss
        
        // Armor - Helmets
        WoodHelmet,
        CopperHelmet,
        IronHelmet,
        SilverHelmet,
        GoldHelmet,
        PlatinumHelmet,
        
        // Armor - Chestplates
        WoodChestplate,
        CopperChestplate,
        IronChestplate,
        SilverChestplate,
        GoldChestplate,
        PlatinumChestplate,
        
        // Armor - Leggings
        WoodLeggings,
        CopperLeggings,
        IronLeggings,
        SilverLeggings,
        GoldLeggings,
        PlatinumLeggings
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
                case ItemType.SummonAltar: return TileType.SummonAltar;
                case ItemType.Door: return TileType.Door;
                
                // Walls (can be placed as solid blocks or backgrounds)
                case ItemType.DirtWall: return TileType.DirtWall;
                case ItemType.StoneWall: return TileType.StoneWall;
                case ItemType.WoodWall: return TileType.WoodWall;
                case ItemType.CopperWall: return TileType.CopperWall;
                case ItemType.IronWall: return TileType.IronWall;
                case ItemType.SilverWall: return TileType.SilverWall;
                case ItemType.GoldWall: return TileType.GoldWall;
                case ItemType.PlatinumWall: return TileType.PlatinumWall;
                case ItemType.SnowWall: return TileType.SnowWall;
                
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
                case TileType.SummonAltar: return ItemType.SummonAltar;
                case TileType.Door: return ItemType.Door;
                
                // Walls
                case TileType.DirtWall: return ItemType.DirtWall;
                case TileType.StoneWall: return ItemType.StoneWall;
                case TileType.WoodWall: return ItemType.WoodWall;
                case TileType.CopperWall: return ItemType.CopperWall;
                case TileType.IronWall: return ItemType.IronWall;
                case TileType.SilverWall: return ItemType.SilverWall;
                case TileType.GoldWall: return ItemType.GoldWall;
                case TileType.PlatinumWall: return ItemType.PlatinumWall;
                case TileType.SnowWall: return ItemType.SnowWall;
                
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
                case ItemType.SummonAltar:
                case ItemType.Door:
                
                // Walls
                case ItemType.DirtWall:
                case ItemType.StoneWall:
                case ItemType.WoodWall:
                case ItemType.CopperWall:
                case ItemType.IronWall:
                case ItemType.SilverWall:
                case ItemType.GoldWall:
                case ItemType.PlatinumWall:
                case ItemType.SnowWall:
                    return true;
                default:
                    return false; // WoodWand is not placeable and is caught here
            }
        }
    }
}
