using Claude4_5Terraria.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Claude4_5Terraria.World
{
    public class Chunk
    {
        public const int CHUNK_SIZE = 32;

        public int ChunkX { get; private set; }
        public int ChunkY { get; private set; }

        private Tile[,] tiles;
        public bool IsLoaded { get; set; }

        public Chunk(int chunkX, int chunkY)
        {
            ChunkX = chunkX;
            ChunkY = chunkY;
            tiles = new Tile[CHUNK_SIZE, CHUNK_SIZE];
            IsLoaded = false;

            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    tiles[x, y] = new Tile();
                }
            }
        }

        public Tile GetTile(int localX, int localY)
        {
            if (localX < 0 || localX >= CHUNK_SIZE || localY < 0 || localY >= CHUNK_SIZE)
                return null;

            return tiles[localX, localY];
        }

        public void SetTile(int localX, int localY, Tile tile)
        {
            if (localX < 0 || localX >= CHUNK_SIZE || localY < 0 || localY >= CHUNK_SIZE)
                return;

            tiles[localX, localY] = tile;
        }

        public void Draw(SpriteBatch spriteBatch, int tileSize, Camera camera)
        {
            // Placeholder - not used currently
        }
    }
}