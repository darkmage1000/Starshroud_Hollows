using StarshroudHollows;
using StarshroudHollows.Enums;
using StarshroudHollows.Systems;
using StarshroudHollows.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// 1. ALIAS THE PLAYER NAMESPACE to 'P' to resolve ambiguity (Player namespace vs Player class)
using P = StarshroudHollows.Player;


namespace StarshroudHollows.World
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

        public HashSet<Point> ExploredTiles => exploredTiles;

        private Dictionary<TileType, Texture2D> tileSprites;

        private HUD hudReference;

        // NEW: Player reference for access by HUD/Systems
        private P.Player playerReference; // CORRECTED using alias P

        // NEW: Reference to the Liquid System
        private LiquidSystem liquidSystemReference;

        // Liquid spreading system fields (Kept for compatibility, but logic is delegated)
        private const int MAX_LIQUID_UPDATES_PER_CHUNK = 20;

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

            hudReference = hud;
        }

        // NEW: Setter for LiquidSystem reference
        public void SetLiquidSystem(LiquidSystem liquidSystem)
        {
            liquidSystemReference = liquidSystem;
        }

        // NEW: Setter for player reference (used by Game1 on load/generation)
        public void SetPlayer(P.Player player) // CORRECTED using alias P
        {
            playerReference = player;
        }

        // NEW: Getter for player reference (used by HUD)
        public P.Player GetPlayer() // CORRECTED using alias P
        {
            return playerReference;
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

        // UPDATED: Now calls the time-sliced liquid update for smooth flow.
        public void Update(float deltaTime, WorldGenerator worldGenerator)
        {
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

        // NEW: Method to stabilize liquids during world generation or loading (delegated)
        public void StabilizeLiquids(float progressStart, float progressEnd, Action<float, string> updateCallback)
        {
            if (liquidSystemReference != null)
            {
                liquidSystemReference.StabilizeLiquids(progressStart, progressEnd, updateCallback);
            }
        }

        // FIX: Restoring the GrowSaplingIntoTree implementation here 
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
                // CRITICAL FIX: Mark tree parts so they don't get background tiles
                var newTile = new Tile(TileType.Wood, true);
                newTile.IsPartOfTree = true;
                SetTile(x, treeY, newTile);
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
                        if (existingTile == null || existingTile.Type != TileType.Wood)
                        {
                            // CRITICAL FIX: Mark leaves so they don't get background tiles
                            var newTile = new Tile(TileType.Leaves, true);
                            newTile.IsPartOfTree = true;
                            SetTile(leafX, leafY, newTile);
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }

            AddTree(tree);
        }

        // FIX: Restoring public UpdateLoadedChunks method 
        public void UpdateLoadedChunks(Camera camera)
        {
            Rectangle visibleArea = camera.GetVisibleArea(TILE_SIZE);
            int preload = 1;
            int pixelsPerChunk = TILE_SIZE * Chunk.CHUNK_SIZE;
            int startChunkX = Math.Max(0, visibleArea.Left / pixelsPerChunk - preload);
            int startChunkY = Math.Max(0, visibleArea.Top / pixelsPerChunk - preload);
            int endChunkX = Math.Min(chunksWide - 1, (visibleArea.Right + pixelsPerChunk - 1) / pixelsPerChunk + preload);
            int endChunkY = Math.Min(chunksHigh - 1, (visibleArea.Bottom + pixelsPerChunk - 1) / pixelsPerChunk + preload);

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

            // CRITICAL FIX: If placing a SOLID tile over liquid/air, reset the target tile's volume to 0.
            if (tile.Type != TileType.Air && tile.Type != TileType.Water && tile.Type != TileType.Lava)
            {
                Tile existingTile = GetTile(worldX, worldY);
                if (existingTile != null)
                {
                    // Aggressively reset volume of the existing tile if it wasn't liquid
                    existingTile.LiquidVolume = 0.0f;
                    existingTile.Type = tile.Type;
                }
            }

            // CRITICAL FIX: SetTile itself MUST NOT trigger recursion. Delegate to LiquidSystem.
            if (trackTileChanges && (tile.Type == TileType.Air || tile.Type == TileType.Water || tile.Type == TileType.Lava))
            {
                TriggerLiquidSpreadCheck(worldX, worldY);
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

            // Note: Tile.IsActive/LiquidVolume is set by the constructor of the passed tile object.
            loadedChunks[chunkPos].SetTile(localX, localY, tile);

            if (trackTileChanges)
            {
                Point tilePos = new Point(worldX, worldY);
                modifiedTiles[tilePos] = tile;
            }
        }

        // NEW: Trigger immediate liquid spread check when block is mined/placed
        private void TriggerLiquidSpreadCheck(int changedX, int changedY)
        {
            // Delegate triggering to the LiquidSystem
            if (liquidSystemReference != null)
            {
                liquidSystemReference.TriggerLocalFlow(changedX, changedY);
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

            if (!loadedChunks.ContainsKey(chunkPos))
            {
                Chunk chunk = new Chunk(chunkX, chunkY);
                chunk.IsLoaded = true;
                loadedChunks[chunkPos] = chunk;
            }

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
                    IsActive = kvp.Value.IsActive,
                    LiquidVolume = kvp.Value.LiquidVolume // NEW: Save liquid volume
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

            bool wasTracking = trackTileChanges;
            trackTileChanges = false;

            foreach (var change in changes)
            {
                Tile tile = new Tile((TileType)change.TileType, change.IsActive);
                tile.LiquidVolume = change.LiquidVolume; // NEW: Restore liquid volume
                SetTile(change.X, change.Y, tile);
            }

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
                        if (exploredTiles.Add(new Point(centerTileX + dx, centerTileY + dy)))
                        {
                            newlyExplored = true;
                        }
                    }
                }
            }

            if (newlyExplored && hudReference != null)
            {
                hudReference.FlagMinimapUpdate();
            }
        }

        public void MarkTileAsExplored(int x, int y)
        {
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

        public void Draw(SpriteBatch spriteBatch, Camera camera, Texture2D pixelTexture, LightingSystem lightingSystem, MiningSystem miningSystem, bool debugMode = false)
        {
            lightingSystem.UpdateTorchCache(camera.Position);

            Rectangle visibleArea = camera.GetVisibleArea(TILE_SIZE);
            int startTileX = Math.Max(0, visibleArea.Left / TILE_SIZE);
            int endTileX = Math.Min(WORLD_WIDTH - 1, visibleArea.Right / TILE_SIZE);
            int startTileY = Math.Max(0, visibleArea.Top / TILE_SIZE);
            int endTileY = Math.Min(WORLD_HEIGHT - 1, visibleArea.Bottom / TILE_SIZE);

            // Draw underground background
            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    // CRITICAL FIX: Skip background rendering for tree tiles
                    Tile currentTile = GetTile(x, y);
                    if (currentTile != null && currentTile.IsPartOfTree)
                    {
                        continue; // Don't draw background behind trees
                    }

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

                    // Skip if the tile is pure air
                    if (tile == null || tile.Type == TileType.Air)
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

                    // NEW LIQUID DRAWING LOGIC: Handle drawing based on LiquidVolume
                    if (tile.Type == TileType.Water || tile.Type == TileType.Lava)
                    {
                        Color liquidColor = GetTileColor(tile.Type);

                        // If volume is < 1.0, draw only the bottom portion of the tile
                        int liquidHeight = (int)(TILE_SIZE * tile.LiquidVolume);
                        Rectangle liquidRect = new Rectangle(
                            destRect.X,
                            destRect.Y + TILE_SIZE - liquidHeight,
                            TILE_SIZE,
                            liquidHeight
                        );

                        // Apply light to the liquid
                        Color finalLiquidColor = new Color(
                            (byte)(liquidColor.R * lightLevel),
                            (byte)(liquidColor.G * lightLevel),
                            (byte)(liquidColor.B * lightLevel)
                        );

                        spriteBatch.Draw(pixelTexture, liquidRect, finalLiquidColor);
                    }
                    else
                    {
                        // Draw solid tile - use sprite if available, otherwise use colored pixel
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
                    }


                    // NEW: Debug Mode highlighting for ores and chests
                    if (debugMode && tile.IsActive)
                    {
                        Color highlightColor = Color.Transparent;
                        bool shouldHighlight = false;

                        // Highlight ores
                        if (tile.Type == TileType.Coal)
                        {
                            highlightColor = new Color(255, 255, 255, 100); // White
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.Copper)
                        {
                            highlightColor = new Color(255, 140, 0, 120); // Orange
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.Iron)
                        {
                            highlightColor = new Color(169, 169, 169, 120); // Dark gray
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.Silver)
                        {
                            highlightColor = new Color(192, 192, 192, 120); // Silver
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.Gold)
                        {
                            highlightColor = new Color(255, 215, 0, 120); // Gold yellow
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.Platinum)
                        {
                            highlightColor = new Color(144, 238, 144, 120); // Light green
                            shouldHighlight = true;
                        }
                        // Highlight chests
                        else if (tile.Type == TileType.WoodChest)
                        {
                            highlightColor = new Color(139, 69, 19, 150); // Brown
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.SilverChest)
                        {
                            highlightColor = new Color(192, 192, 192, 150); // Silver
                            shouldHighlight = true;
                        }
                        else if (tile.Type == TileType.MagicChest)
                        {
                            highlightColor = new Color(138, 43, 226, 150); // Purple
                            shouldHighlight = true;
                        }

                        if (shouldHighlight)
                        {
                            Rectangle highlightRect = new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                            spriteBatch.Draw(pixelTexture, highlightRect, highlightColor);
                            // Draw a bright border for extra visibility
                            DrawBorder(spriteBatch, pixelTexture, highlightRect, 2, new Color(highlightColor, 200));
                        }
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
                            Color barColor = progress < 0.3f ? Color.Green : progress < 0.7f ? Color.Yellow : Color.Red;
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

        public Color GetTileColor(TileType type)
        {
            switch (type)
            {
                case TileType.Grass: return new Color(34, 139, 34);
                case TileType.Dirt: return new Color(150, 75, 0);
                case TileType.Stone: return new Color(128, 128, 128);
                case TileType.Copper: return new Color(255, 140, 0);
                case TileType.Iron: return new Color(169, 169, 169); // Dark gray
                case TileType.Silver: return new Color(192, 192, 192);
                case TileType.Gold: return new Color(255, 215, 0); // Gold yellow
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
                case TileType.Lava: return new Color(255, 100, 0);
                case TileType.Water: return new Color(30, 144, 255);
                case TileType.Obsidian: return new Color(20, 20, 30);
                case TileType.Bed: return new Color(200, 50, 50);
                case TileType.Snow: return new Color(240, 248, 255); // White/light blue snow
                case TileType.SnowGrass: return new Color(230, 240, 255); // Slightly bluish white
                case TileType.Ice: return new Color(175, 238, 238); // Light cyan ice
                case TileType.Icicle: return new Color(200, 230, 255); // Pale blue icicle
                case TileType.SnowyLeaves: return new Color(220, 240, 245); // Frosty white-blue leaves
                default: return Color.White;
            }
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
            if (tile == null) return false;

            // Solid if IsActive is true (i.e., not liquid and not air)
            if (tile.IsActive)
            {
                if (tile.IsPartOfTree || tile.Type == TileType.Torch || tile.Type == TileType.Sapling) return false;
                return true;
            }

            // If it's a liquid tile, it's not solid (unless its volume is high enough, but that logic is in LiquidSystem)
            if (tile.Type == TileType.Water || tile.Type == TileType.Lava) return false;

            return false;
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
