using StarshroudHollows.Enums;

namespace StarshroudHollows.World
{
    public class Tile
    {
        public TileType Type { get; set; }
        public float Health { get; set; }
        public bool IsPartOfTree { get; set; }
        public bool IsDoorOpen { get; set; } // NEW: Track door state

        // NEW: Background wall support
        public TileType WallType { get; set; }
        public bool HasWall => WallType != TileType.Air;

        // NEW: Liquid Volume property (0.0 to 1.0)
        public float LiquidVolume { get; set; }

        // NEW: Read-only property to replace IsActive for non-liquid tiles
        public bool IsActive => Type != TileType.Air && LiquidVolume < 0.1f;
        // Note: For solid blocks, IsActive is true. For liquid blocks, IsActive is false (liquid flow is managed by volume).

        public Tile()
        {
            Type = TileType.Air;
            Health = 1.0f;
            IsPartOfTree = false;
            LiquidVolume = 0.0f;
            WallType = TileType.Air; // NEW: No wall by default
            IsDoorOpen = false; // Doors start closed
        }

        public Tile(TileType type, bool isPartOfTree = false)
        {
            Type = type;
            Health = 1.0f;
            IsPartOfTree = isPartOfTree;
            WallType = TileType.Air; // NEW: No wall by default
            IsDoorOpen = false; // Doors start closed

            if (type == TileType.Water || type == TileType.Lava)
            {
                LiquidVolume = 1.0f;
            }
            else if (type != TileType.Air)
            {
                LiquidVolume = 0.0f;
            }
            else
            {
                LiquidVolume = 0.0f;
            }
        }
    }
}