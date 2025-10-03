using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Systems;


namespace Claude4_5Terraria.World
{
    public class World
    {
        public const int WORLD_WIDTH = 500;
        public const int WORLD_HEIGHT = 1000;
        public const int TILE_SIZE = 32;

        private Dictionary<Point, Chunk> loadedChunks;
        private int chunksWide;
        private int chunksHigh;

        // Tree tracking
        private List<Tree> trees;
        private Dictionary<Point, Tree> tileToTreeMap;

        public World()
        {
            loadedChunks = new Dictionary<Point, Chunk>();
            chunksWide = (WORLD_WIDTH + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE;
            chunksHigh = (WORLD_HEIGHT + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE;

            trees = new List<Tree>();
            tileToTreeMap = new Dictionary<Point, Tree>();
        }

        public void AddTree(Tree tree)
        {
            trees.Add(tree);

            foreach (Point pos in tree.TilePositions)
            {
                tileToTreeMap[pos] = tree;
            }
        }

        public void RemoveTree(int tileX, int tileY)
        {
            Point tilePos = new Point(tileX, tileY);

            if (tileToTreeMap.ContainsKey(tilePos))
            {
                Tree tree = tileToTreeMap[tilePos];

                foreach (Point pos in tree.TilePositions)
                {
                    SetTile(pos.X, pos.Y, new Tile(TileType.Air));
                    tileToTreeMap.Remove(pos);
                }

                trees.Remove(tree);
            }
        }

        public void UpdateLoadedChunks(Camera camera)
        {
            Rectangle visibleArea = camera.GetVisibleArea(TILE_SIZE);

            int startChunkX = visibleArea.Left / Chunk.CHUNK_SIZE;
            int startChunkY = visibleArea.Top / Chunk.CHUNK_SIZE;
            int endChunkX = (visibleArea.Right + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE;
            int endChunkY = (visibleArea.Bottom + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE;

            startChunkX = MathHelper.Clamp(startChunkX, 0, chunksWide - 1);
            startChunkY = MathHelper.Clamp(startChunkY, 0, chunksHigh - 1);
            endChunkX = MathHelper.Clamp(endChunkX, 0, chunksWide - 1);
            endChunkY = MathHelper.Clamp(endChunkY, 0, chunksHigh - 1);

            for (int cx = startChunkX; cx <= endChunkX; cx++)
            {
                for (int cy = startChunkY; cy <= endChunkY; cy++)
                {
                    Point chunkPos = new Point(cx, cy);
                    if (!loadedChunks.ContainsKey(chunkPos))
                    {
                        Chunk chunk = new Chunk(cx, cy);
                        chunk.IsLoaded = true;
                        loadedChunks[chunkPos] = chunk;
                    }
                }
            }
        }

        public Tile GetTile(int worldX, int worldY)
        {
            if (worldX < 0 || worldX >= WORLD_WIDTH || worldY < 0 || worldY >= WORLD_HEIGHT)
                return null;

            int chunkX = worldX / Chunk.CHUNK_SIZE;
            int chunkY = worldY / Chunk.CHUNK_SIZE;
            int localX = worldX % Chunk.CHUNK_SIZE;
            int localY = worldY % Chunk.CHUNK_SIZE;

            Point chunkPos = new Point(chunkX, chunkY);
            if (loadedChunks.ContainsKey(chunkPos))
            {
                return loadedChunks[chunkPos].GetTile(localX, localY);
            }

            return null;
        }

        public void SetTile(int worldX, int worldY, Tile tile)
        {
            if (worldX < 0 || worldX >= WORLD_WIDTH || worldY < 0 || worldY >= WORLD_HEIGHT)
                return;

            int chunkX = worldX / Chunk.CHUNK_SIZE;
            int chunkY = worldY / Chunk.CHUNK_SIZE;
            int localX = worldX % Chunk.CHUNK_SIZE;
            int localY = worldY % Chunk.CHUNK_SIZE;

            Point chunkPos = new Point(chunkX, chunkY);

            if (!loadedChunks.ContainsKey(chunkPos))
            {
                Chunk chunk = new Chunk(chunkX, chunkY);
                chunk.IsLoaded = true;
                loadedChunks[chunkPos] = chunk;
            }

            loadedChunks[chunkPos].SetTile(localX, localY, tile);
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, Texture2D pixelTexture, LightingSystem lightingSystem)
        {
            // Use camera's GetVisibleArea which properly handles centered camera
            Rectangle visibleArea = camera.GetVisibleArea(TILE_SIZE);

            int startTileX = Math.Max(0, visibleArea.Left);
            int endTileX = Math.Min(WORLD_WIDTH - 1, visibleArea.Right);
            int startTileY = Math.Max(0, visibleArea.Top);
            int endTileY = Math.Min(WORLD_HEIGHT - 1, visibleArea.Bottom);

            // Update torch lighting cache
            if (lightingSystem != null)
            {
                lightingSystem.UpdateTorchCache(camera.Position);
            }

            // Draw all visible tiles
            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    Tile tile = GetTile(x, y);
                    if (tile == null || !tile.IsActive)
                        continue;

                    Rectangle destRect = new Rectangle(
                        x * TILE_SIZE,
                        y * TILE_SIZE,
                        TILE_SIZE,
                        TILE_SIZE
                    );

                    Color tileColor = GetTileColor(tile.Type);

                    // Apply lighting
                    if (lightingSystem != null)
                    {
                        tileColor = lightingSystem.ApplyLighting(tileColor, x, y);
                    }

                    spriteBatch.Draw(pixelTexture, destRect, tileColor);
                }
            }
        }

        private Color GetTileColor(TileType type)
        {
            switch (type)
            {
                case TileType.Grass:
                    return new Color(34, 139, 34);  // Forest green (same as leaves for now)
                case TileType.Dirt:
                    return new Color(150, 75, 0);
                case TileType.Stone:
                    return new Color(128, 128, 128);
                case TileType.Copper:
                    return new Color(255, 140, 0);
                case TileType.Silver:
                    return new Color(192, 192, 192);
                case TileType.Platinum:
                    return new Color(144, 238, 144);
                case TileType.Wood:
                    return new Color(101, 67, 33);
                case TileType.Leaves:
                    return new Color(34, 139, 34);
                case TileType.Coal:
                    return new Color(40, 40, 40);  // Dark gray/black
                case TileType.WoodCraftingBench:
                    return new Color(120, 80, 40);
                case TileType.Torch:
                    return new Color(255, 200, 100);  // Bright yellow-orange
                default:
                    return Color.White;
            }
        }

        public int GetSurfaceHeight(int x)
        {
            // Start from a known surface area and go down to find ground
            for (int y = 50; y < WORLD_HEIGHT; y++)
            {
                Tile tile = GetTile(x, y);
                if (tile != null && tile.IsActive && (tile.Type == TileType.Dirt || tile.Type == TileType.Grass || tile.Type == TileType.Stone))
                {
                    return y;
                }
            }
            return 100;
        }
        public bool IsSolidAtPosition(int x, int y)
        {
            // Check world borders
            if (x <= 0 || x >= WORLD_WIDTH - 1)
            {
                return true;
            }

            if (y >= WORLD_HEIGHT - 1)
            {
                return true;
            }

            // Check tile
            Tile tile = GetTile(x, y);
            if (tile == null || !tile.IsActive)
            {
                return false;
            }

            // Tree parts and torches are walk-through
            if (tile.IsPartOfTree || tile.Type == TileType.Torch)
            {
                return false;
            }

            return true;
        }
    }
}