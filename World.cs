using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Player;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.UI; // <-- REQUIRED for HUD reference
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Claude4_5Terraria.World
{
    public class World
    {
        public const int WORLD_WIDTH = 6500;
        public const int WORLD_HEIGHT = 2700;
        public const int TILE_SIZE = 32;

        public Dictionary<Point, Chunk> loadedChunks;

        private int chunksWide;
        private int chunksHigh;

        private List<Tree> trees;
        private Dictionary<Point, Tree> tileToTreeMap;
        private Dictionary<Point, Tile> modifiedTiles;
        private List<Sapling> saplings;

        private Random random;

        private bool allowChunkUnloading = false;
        private bool trackTileChanges = false;
        private bool allowWorldUpdates = true;

        private HashSet<Point> exploredTiles;

        // FIX: Public getter to expose explored tiles for HUD/Minimap
        public HashSet<Point> ExploredTiles => exploredTiles;

        // Tile sprites
        private Dictionary<TileType, Texture2D> tileSprites;

        // FIX: HUD reference for flagging minimap updates
        private HUD hudReference;


        public World(HUD hud)
        {
            loadedChunks = new Dictionary<Point, Chunk>();
            chunksWide = (WORLD_WIDTH + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE;
            chunksHigh = (WORLD_HEIGHT + Chunk.CHUNK_SIZE - 1) / Chunk.CHUNK_SIZE;

            trees = new List<Tree>();
            tileToTreeMap = new Dictionary<Point, Tree>();
            saplings = new List<Sapling>();
            exploredTiles = new HashSet<Point>();
            modifiedTiles = new Dictionary<Point, Tile>();
            trackTileChanges = false;

            tileSprites = new Dictionary<TileType, Texture2D>();

            random = new Random();

            // STORE HUD REFERENCE
            hudReference = hud;
        }
        public void LoadTileSprite(TileType tileType, Texture2D sprite)
        {
            tileSprites[tileType] = sprite;
            Logger.Log($"[WORLD] Loaded sprite for {tileType}");
        }

        public void EnableChunkUnloading()
        {
            allowChunkUnloading = true;
            Logger.Log("[WORLD] Chunk unloading enabled - world generation complete");
        }

        public void EnableTileChangeTracking()
        {
            trackTileChanges = true;
            modifiedTiles.Clear();
            Logger.Log("[WORLD] Tile change tracking enabled - now tracking player changes only");
        }

        public void DisableWorldUpdates()
        {
            allowWorldUpdates = false;
            Logger.Log("[WORLD] World updates disabled - preventing sapling growth in loaded save");
        }

        public void AddTree(Tree tree)
        {
            trees.Add(tree);

            foreach (Point pos in tree.TilePositions)
            {
                if (!tileToTreeMap.ContainsKey(pos))
                    tileToTreeMap[pos] = tree;
                else
                    Logger.Log($"[WORLD] Warning: overlapping tree tile at {pos}");
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

        public void AddSapling(int x, int y)
        {
            saplings.Add(new Sapling(x, y));
            SetTile(x, y, new Tile(TileType.Sapling));
            Logger.Log($"[WORLD] Planted sapling at ({x}, {y})");
        }

        public void Update(float deltaTime, WorldGenerator worldGenerator)
        {
            // Don't update saplings/trees if world updates disabled (loaded save)
            if (!allowWorldUpdates) return;

            List<Sapling> grownSaplings = new List<Sapling>();

            foreach (Sapling sapling in saplings)
            {
                sapling.Update(deltaTime);

                if (sapling.IsReadyToGrow())
                {
                    grownSaplings.Add(sapling);
                }
            }

            foreach (Sapling sapling in grownSaplings)
            {
                GrowSaplingIntoTree(sapling.Position.X, sapling.Position.Y);
                saplings.Remove(sapling);
            }
        }

        private void GrowSaplingIntoTree(int x, int y)
        {
            // Check what's below the sapling
            Tile groundTile = GetTile(x, y + 1);

            // Remove the sapling tile - leave the ground as is (dirt or grass)
            SetTile(x, y, new Tile(TileType.Air));

            Logger.Log($"[WORLD] Sapling grew into tree at ({x}, {y}), ground below is {groundTile?.Type}");

            int trunkHeight = random.Next(8, 15);
            var tree = new Tree(x, y + 1, trunkHeight);

            for (int dy = 0; dy < trunkHeight; dy++)
            {
                int treeY = y - dy;
                SetTile(x, treeY, new Tile(TileType.Wood, true));
                tree.AddTile(x, treeY);
            }

            int canopyY = y - trunkHeight;
            int canopyRadius = random.Next(2, 4);

            for (int dx = -canopyRadius; dx <= canopyRadius; dx++)
            {
                for (int dy = -canopyRadius; dy <= canopyRadius; dy++)
                {
                    if (dx * dx + dy * dy <= canopyRadius * canopyRadius + 2)
                    {
                        int leafX = x + dx;
                        int leafY = canopyY + dy;

                        var existingTile = GetTile(leafX, leafY);
                        if (existingTile == null || !existingTile.IsActive || existingTile.Type != TileType.Wood)
                        {
                            SetTile(leafX, leafY, new Tile(TileType.Leaves, true));
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }

            AddTree(tree);
        }

        // In World.cs, in the UpdateLoadedChunks method, comment out or remove the unloading section (around lines ~200-220 based on document):
        public void UpdateLoadedChunks(Camera camera)
        {
            Rectangle visibleArea = camera.GetVisibleArea(TILE_SIZE);
            int preload = 1;
            int pixelsPerChunk = TILE_SIZE * Chunk.CHUNK_SIZE;
            int startChunkX = Math.Max(0, visibleArea.Left / pixelsPerChunk - preload);
            int startChunkY = Math.Max(0, visibleArea.Top / pixelsPerChunk - preload);
            int endChunkX = Math.Min(chunksWide - 1, (visibleArea.Right + pixelsPerChunk - 1) / pixelsPerChunk + preload);
            int endChunkY = Math.Min(chunksHigh - 1, (visibleArea.Bottom + pixelsPerChunk - 1) / pixelsPerChunk + preload);

            // Removed/Commented-out unloading logic (as per previous consensus)

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

        public void SetTile(int worldX, int worldY, Tile tile)
        {
            if (worldX < 0 || worldX >= WORLD_WIDTH || worldY < 0 || worldY >= WORLD_HEIGHT)
                return;

            tile.IsActive = (tile.Type != TileType.Air);

            // DEBUG: Log when player mines/ places
            if (trackTileChanges && tile.Type == TileType.Air)
            {
                Logger.Log($"[WORLD] Player mined tile at ({worldX}, {worldY})");
            }

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

            if (trackTileChanges)
            {
                Point tilePos = new Point(worldX, worldY);
                modifiedTiles[tilePos] = tile;
            }
        }

        // CRITICAL FIX: Ensure chunk is loaded before accessing tile data
        public Tile GetTile(int worldX, int worldY)
        {
            if (worldX < 0 || worldX >= WORLD_WIDTH || worldY < 0 || worldY >= WORLD_HEIGHT)
                return null;

            int chunkX = worldX / Chunk.CHUNK_SIZE;
            int chunkY = worldY / Chunk.CHUNK_SIZE;
            int localX = worldX % Chunk.CHUNK_SIZE;
            int localY = worldY % Chunk.CHUNK_SIZE;

            Point chunkPos = new Point(chunkX, chunkY);

            // FIX: If the map is drawing a tile, we must load/create the chunk containing it.
            if (!loadedChunks.ContainsKey(chunkPos))
            {
                Chunk chunk = new Chunk(chunkX, chunkY);
                chunk.IsLoaded = true;
                loadedChunks[chunkPos] = chunk;
            }

            // Now we can safely return the tile
            return loadedChunks[chunkPos].GetTile(localX, localY);
        }

        public List<TileChangeData> GetModifiedTiles()
        {
            List<TileChangeData> changes = new List<TileChangeData>();

            foreach (var kvp in modifiedTiles)
            {
                changes.Add(new TileChangeData
                {
                    X = kvp.Key.X,
                    Y = kvp.Key.Y,
                    TileType = (int)kvp.Value.Type,
                    IsActive = kvp.Value.IsActive
                });
            }

            Logger.Log($"[WORLD] Saving {changes.Count} modified tiles");
            return changes;
        }

        public void ApplyTileChanges(List<TileChangeData> changes)
        {
            if (changes == null || changes.Count == 0)
            {
                Logger.Log("[WORLD] No tile changes to apply");
                return;
            }

            Logger.Log($"[WORLD] Applying {changes.Count} tile changes from save file");

            // CRITICAL FIX: Temporarily disable tracking while applying saved changes
            bool wasTracking = trackTileChanges;
            trackTileChanges = false;

            foreach (var change in changes)
            {
                Tile tile = new Tile((TileType)change.TileType, change.IsActive);
                SetTile(change.X, change.Y, tile);
            }

            // Re-enable tracking if it was enabled
            trackTileChanges = wasTracking;

            Logger.Log("[WORLD] Tile changes applied successfully");
        }

        public void MarkAreaAsExplored(Vector2 playerCenter)
        {
            int centerTileX = (int)(playerCenter.X / TILE_SIZE);
            int centerTileY = (int)(playerCenter.Y / TILE_SIZE);
            int radius = 3;  // 3 tiles around player

            bool newlyExplored = false;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        // Check if Add was successful (i.e., it's a new tile)
                        if (exploredTiles.Add(new Point(centerTileX + dx, centerTileY + dy)))
                        {
                            newlyExplored = true;
                        }
                    }
                }
            }

            // Flag update only if something new was explored
            if (newlyExplored && hudReference != null)
            {
                hudReference.FlagMinimapUpdate();
            }
        }

        public void MarkTileAsExplored(int x, int y)
        {
            // Check if the tile was actually added (i.e., it wasn't already explored)
            if (exploredTiles.Add(new Point(x, y)))
            {
                if (hudReference != null)
                {
                    hudReference.FlagMinimapUpdate();
                }
            }
        }

        public bool IsTileExplored(int x, int y)
        {
            return exploredTiles.Contains(new Point(x, y));
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, Texture2D pixelTexture, LightingSystem lightingSystem, MiningSystem miningSystem)
        {
            lightingSystem.UpdateTorchCache(camera.Position);

            Rectangle visibleArea = camera.GetVisibleArea(TILE_SIZE);
            int startTileX = Math.Max(0, (int)(visibleArea.Left / TILE_SIZE));
            int endTileX = Math.Min(WORLD_WIDTH - 1, (int)(visibleArea.Right / TILE_SIZE));
            int startTileY = Math.Max(0, (int)(visibleArea.Top / TILE_SIZE));
            int endTileY = Math.Min(WORLD_HEIGHT - 1, (int)(visibleArea.Bottom / TILE_SIZE));

            // Draw underground background
            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    // Check if there are solid blocks above (underground)
                    bool hasBlocksAbove = false;
                    for (int checkY = y - 1; checkY >= 0; checkY--)
                    {
                        Tile checkTile = GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsActive)
                        {
                            if (checkTile.Type != TileType.Leaves && checkTile.Type != TileType.Wood &&
                                checkTile.Type != TileType.Torch && checkTile.Type != TileType.Sapling)
                            {
                                hasBlocksAbove = true;
                                break;
                            }
                        }
                    }

                    if (hasBlocksAbove)
                    {
                        Rectangle destRect = new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);

                        // Underground background color
                        Color bgColor;
                        if (y < 210)
                        {
                            bgColor = new Color(101, 67, 33);  // Brown dirt background
                        }
                        else
                        {
                            bgColor = new Color(64, 64, 64);   // Dark gray stone background
                        }

                        float lightLevel = lightingSystem.GetLightLevel(x, y);
                        bgColor = new Color(
                            (byte)(bgColor.R * lightLevel),
                            (byte)(bgColor.G * lightLevel),
                            (byte)(bgColor.B * lightLevel),
                            (byte)255
                        );

                        spriteBatch.Draw(pixelTexture, destRect, bgColor);
                    }
                }
            }

            // Draw tiles
            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    Tile tile = GetTile(x, y);

                    if (tile == null || !tile.IsActive)
                        continue;

                    Rectangle destRect = new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);

                    Color tileColor = GetTileColor(tile.Type);
                    tileColor.A = 255;

                    float lightLevel = lightingSystem.GetLightLevel(x, y);

                    // Mark tiles as explored if they have sufficient light (lower threshold for torches)
                    if (lightLevel > 0.25f)
                    {
                        MarkTileAsExplored(x, y);
                    }

                    // Apply lighting to color
                    Color litColor = new Color(
                        (byte)(tileColor.R * lightLevel),
                        (byte)(tileColor.G * lightLevel),
                        (byte)(tileColor.B * lightLevel),
                        (byte)255
                    );

                    // Draw tile - use sprite if available, otherwise use colored pixel
                    if (tileSprites.ContainsKey(tile.Type))
                    {
                        // Draw the tile sprite with lighting applied
                        spriteBatch.Draw(tileSprites[tile.Type], destRect, Color.White * lightLevel);
                    }
                    else
                    {
                        // Fallback to colored pixel
                        spriteBatch.Draw(pixelTexture, destRect, litColor);
                    }

                    // Draw mining overlay on top of tile
                    if (miningSystem != null && miningSystem.GetTargetedTile().HasValue)
                    {
                        Point? targeted = miningSystem.GetTargetedTile();
                        if (targeted.HasValue && x == targeted.Value.X && y == targeted.Value.Y)
                        {
                            float progress = miningSystem.GetMiningProgress();

                            Rectangle barBg = new Rectangle(x * TILE_SIZE, (y - 1) * TILE_SIZE - 4, TILE_SIZE, 8);
                            spriteBatch.Draw(pixelTexture, barBg, Color.Black * 0.8f);

                            Rectangle barFill = new Rectangle(x * TILE_SIZE + 2, (y - 1) * TILE_SIZE - 2,
                                (int)((TILE_SIZE - 4) * (1f - progress)), 4);
                            Color barColor = progress < 0.3f ? Color.Green : (progress < 0.7f ? Color.Yellow : Color.Red);
                            spriteBatch.Draw(pixelTexture, barFill, barColor);
                            DrawBorder(spriteBatch, pixelTexture, barBg, 1, Color.White * 0.5f);

                            Color crackColor = Color.Black * 0.6f;
                            int crackStage = (int)(progress * 3f);
                            if (crackStage >= 1)
                            {
                                for (int c = 0; c < 3; c++)
                                {
                                    int startX = (int)(random.NextDouble() * TILE_SIZE);
                                    int startY = (int)(random.NextDouble() * TILE_SIZE);
                                    int length = (int)(TILE_SIZE * 0.3f);
                                    int endX = startX + (int)(random.NextDouble() * length - length / 2);
                                    int endY = startY + (int)(random.NextDouble() * length - length / 2);
                                    DrawLine(spriteBatch, pixelTexture,
                                        new Vector2(x * TILE_SIZE + startX, y * TILE_SIZE + startY),
                                        new Vector2(x * TILE_SIZE + endX, y * TILE_SIZE + endY),
                                        crackColor, 2);
                                }
                            }
                            if (crackStage >= 2)
                            {
                                for (int c = 0; c < 5; c++)
                                {
                                    int startX = (int)(random.NextDouble() * TILE_SIZE);
                                    int startY = (int)(random.NextDouble() * TILE_SIZE);
                                    int length = (int)(TILE_SIZE * 0.5f);
                                    int endX = startX + (int)(random.NextDouble() * length - length / 2);
                                    int endY = startY + (int)(random.NextDouble() * length - length / 2);
                                    DrawLine(spriteBatch, pixelTexture,
                                        new Vector2(x * TILE_SIZE + startX, y * TILE_SIZE + startY),
                                        new Vector2(x * TILE_SIZE + endX, y * TILE_SIZE + endY),
                                        crackColor, 2);
                                }
                                Rectangle centerCrack = new Rectangle(x * TILE_SIZE + TILE_SIZE / 3,
                                    y * TILE_SIZE + TILE_SIZE / 3, TILE_SIZE / 3, TILE_SIZE / 3);
                                spriteBatch.Draw(pixelTexture, centerCrack, crackColor * 0.3f);
                            }
                        }
                    }
                }
            }
        }

        // FIX: Change to public so HUD can use it for minimap
        public Color GetTileColor(TileType type)
        {
            switch (type)
            {
                case TileType.Grass: return new Color(34, 139, 34);
                case TileType.Dirt: return new Color(150, 75, 0);
                case TileType.Stone: return new Color(128, 128, 128);
                case TileType.Copper: return new Color(255, 140, 0);
                case TileType.Silver: return new Color(192, 192, 192);
                case TileType.Platinum: return new Color(144, 238, 144);
                case TileType.Wood: return new Color(101, 67, 33);
                case TileType.Leaves: return new Color(34, 139, 34);
                case TileType.Coal: return new Color(40, 40, 40);
                case TileType.WoodCraftingBench: return new Color(120, 80, 40);
                case TileType.CopperCraftingBench: return new Color(200, 100, 20);
                case TileType.Torch: return new Color(255, 200, 100);
                case TileType.Sapling: return new Color(100, 200, 100);
                case TileType.WoodChest: return new Color(139, 69, 19);
                case TileType.SilverChest: return new Color(192, 192, 192);
                case TileType.MagicChest: return new Color(138, 43, 226);
                default: return Color.White;
            }
        }
        // DEPRECATED: This method is no longer used by the optimized HUD.
        public Color[,] GetMinimapData()
        {
            // This method is left blank or removed as the HUD now updates data incrementally.
            return new Color[0, 0];
        }

        public int GetSurfaceHeight(int x)
        {
            for (int y = 50; y < WORLD_HEIGHT; y++)
            {
                Tile tile = GetTile(x, y);
                if (tile != null && tile.IsActive &&
                    (tile.Type == TileType.Dirt || tile.Type == TileType.Grass || tile.Type == TileType.Stone))
                {
                    return y;
                }
            }
            return 100;
        }

        public bool IsSolidAtPosition(int x, int y)
        {
            if (x <= 0 || x >= WORLD_WIDTH - 1) return true;
            if (y <= 0 || y >= WORLD_HEIGHT - 1) return true;

            Tile tile = GetTile(x, y);
            if (tile == null || !tile.IsActive) return false;
            if (tile.IsPartOfTree || tile.Type == TileType.Torch || tile.Type == TileType.Sapling) return false;

            return true;
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
                null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int thickness, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}