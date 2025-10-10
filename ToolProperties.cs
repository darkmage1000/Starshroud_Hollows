using Claude4_5Terraria.Enums;

namespace Claude4_5Terraria.Systems
{
    public static class ToolProperties
    {
        public static bool CanMine(ItemType tool, TileType tile)
        {
            // Torches, benches, chests - always mineable with anything
            if (tile == TileType.Torch ||
                tile == TileType.WoodCraftingBench ||
                tile == TileType.CopperCraftingBench ||
                tile == TileType.WoodChest ||
                tile == TileType.SilverChest ||
                tile == TileType.MagicChest)
            {
                return true;
            }

            // If not a pickaxe, treat as fists
            if (!IsPickaxe(tool))
            {
                tool = ItemType.None;
            }

            switch (tool)
            {
                case ItemType.None:  // Fists - soft blocks + trees
                    return tile == TileType.Dirt ||
                           tile == TileType.Grass ||
                           tile == TileType.Wood ||
                           tile == TileType.Leaves;

                case ItemType.WoodPickaxe:
                    return tile == TileType.Dirt || tile == TileType.Grass ||
                           tile == TileType.Stone || tile == TileType.Wood ||
                           tile == TileType.Leaves || tile == TileType.Coal;

                case ItemType.StonePickaxe:
                    return CanMine(ItemType.WoodPickaxe, tile) ||
                           tile == TileType.Copper;

                case ItemType.CopperPickaxe:
                    return CanMine(ItemType.StonePickaxe, tile) ||
                           tile == TileType.Silver;

                case ItemType.SilverPickaxe:
                    return CanMine(ItemType.CopperPickaxe, tile) ||
                           tile == TileType.Platinum;

                case ItemType.PlatinumPickaxe:
                    return true;  // All blocks

                case ItemType.RunicPickaxe:
                    return true;  // All blocks - best tier

                default:
                    return false;
            }
        }

        public static float GetMiningSpeed(ItemType tool)
        {
            // If not a pickaxe, treat as fists
            if (!IsPickaxe(tool))
            {
                tool = ItemType.None;
            }

            switch (tool)
            {
                case ItemType.None:
                    return 0.5f;
                case ItemType.WoodPickaxe:
                    return 1.0f;
                case ItemType.StonePickaxe:
                    return 1.5f;
                case ItemType.CopperPickaxe:
                    return 2.0f;
                case ItemType.SilverPickaxe:
                    return 2.5f;
                case ItemType.PlatinumPickaxe:
                    return 3.0f;
                case ItemType.RunicPickaxe:
                    return 4.0f;  // Fastest mining speed
                default:
                    return 0f;
            }
        }

        private static bool IsPickaxe(ItemType tool)
        {
            return tool == ItemType.WoodPickaxe ||
                   tool == ItemType.StonePickaxe ||
                   tool == ItemType.CopperPickaxe ||
                   tool == ItemType.SilverPickaxe ||
                   tool == ItemType.PlatinumPickaxe ||
                   tool == ItemType.RunicPickaxe;
        }
    }
}
