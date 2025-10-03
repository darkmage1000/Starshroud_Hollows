using Claude4_5Terraria.Enums;

namespace Claude4_5Terraria.World
{
    public class Tile
    {
        public TileType Type { get; set; }
        public bool IsActive { get; set; }
        public float Health { get; set; }
        public bool IsPartOfTree { get; set; }

        public Tile()
        {
            Type = TileType.Air;
            IsActive = false;
            Health = 1.0f;
            IsPartOfTree = false;
        }

        public Tile(TileType type, bool isPartOfTree = false)
        {
            Type = type;
            IsActive = type != TileType.Air;
            Health = 1.0f;
            IsPartOfTree = isPartOfTree;
        }
    }
}