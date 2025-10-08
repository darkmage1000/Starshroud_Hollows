using System;
using Microsoft.Xna.Framework;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Systems;

namespace Claude4_5Terraria.Systems
{
    public class WorldGenerator
    {
        private Random random;
        private Claude4_5Terraria.World.World world;
        private int seed;

        private const int SURFACE_LEVEL = 200;
        private const int DIRT_LAYER_THICKNESS = 10;
        private const int CAVE_START_DEPTH = 230;

        public Action<float, string> OnProgressUpdate;
        public int GetSeed() => seed;

        public WorldGenerator(Claude4_5Terraria.World.World world, int seed = 0)
        {
            this.world = world;
            this.seed = seed == 0 ? Environment.TickCount : seed;
            this.random = new Random(this.seed);
        }

        public void Generate()
        {
            DateTime startTime = DateTime.Now;

            try
            {
                Logger.Log("[WORLDGEN] Starting world generation...");

                OnProgressUpdate?.Invoke(0.0f, "Generating terrain...");
                GenerateTerrain();

                OnProgressUpdate?.Invoke(0.2f, "Carving caves...");
                GenerateCaves();

                OnProgressUpdate?.Invoke(0.4f, "Placing torches...");
                PlaceCaveTorches();

                OnProgressUpdate?.Invoke(0.5f, "Adding dirt pockets...");
                GenerateDirtPockets();

                OnProgressUpdate?.Invoke(0.6f, "Generating ores...");
                GenerateOres();

                OnProgressUpdate?.Invoke(0.8f, "Planting trees...");
                GenerateTrees();

                OnProgressUpdate?.Invoke(0.9f, "Growing grass...");
                GenerateGrass();

                OnProgressUpdate?.Invoke(1.0f, "Complete!");

                TimeSpan duration = DateTime.Now - startTime;
                Logger.Log($"[WORLDGEN] World generated in {duration.TotalSeconds:F2}s");
                Logger.Log($"[WORLDGEN] Loaded chunks: {world.loadedChunks.Count}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[ERROR] World generation failed: {ex.Message}");
                Logger.Log($"[ERROR] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private void GenerateTerrain()
        {
            int centerX = Claude4_5Terraria.World.World.WORLD_WIDTH / 2;
            int flatZoneRadius = 25;
            int transitionZone = 25;

            for (int x = 0; x < Claude4_5Terraria.World.World.WORLD_WIDTH; x++)
            {
                double noise1 = Math.Sin(x * 0.015) * 12;
                double noise2 = Math.Sin(x * 0.04) * 6;
                double noise3 = Math.Sin(x * 0.1) * 3;

                int surfaceHeight = SURFACE_LEVEL + (int)(noise1 + noise2 + noise3);

                int distanceFromCenter = Math.Abs(x - centerX);

                if (distanceFromCenter < flatZoneRadius)
                {
                    surfaceHeight = SURFACE_LEVEL;
                }
                else if (distanceFromCenter < flatZoneRadius + transitionZone)
                {
                    float blendFactor = (float)(distanceFromCenter - flatZoneRadius) / transitionZone;
                    blendFactor = blendFactor * blendFactor * (3f - 2f * blendFactor);
                    surfaceHeight = (int)Math.Round(SURFACE_LEVEL * (1 - blendFactor) + surfaceHeight * blendFactor);
                }

                // FIXED: Fill all the way to the bottom of the map
                for (int y = 0; y < Claude4_5Terraria.World.World.WORLD_HEIGHT; y++)
                {
                    if (y < surfaceHeight) continue;
                    else if (y == surfaceHeight)
                        world.SetTile(x, y, new Claude4_5Terraria.World.Tile(TileType.Grass));
                    else if (y < surfaceHeight + DIRT_LAYER_THICKNESS)
                        world.SetTile(x, y, new Claude4_5Terraria.World.Tile(TileType.Dirt));
                    else
                        world.SetTile(x, y, new Claude4_5Terraria.World.Tile(TileType.Stone));
                }
            }
        }

        private void GenerateCaves()
        {
            // Calculate safe maximum depth
            int maxSafeDepth = Claude4_5Terraria.World.World.WORLD_HEIGHT - 150;
            
            // Shallow caves
            CarveCaves(100, 40, 80, 2, 4, CAVE_START_DEPTH, Math.Min(600, maxSafeDepth));
            // Mid-depth caves
            CarveCaves(150, 60, 120, 2, 5, 600, Math.Min(1500, maxSafeDepth));
            // Deep caves
            CarveCaves(140, 80, 150, 3, 6, 1500, Math.Min(2500, maxSafeDepth));
            // Very deep caves (NEW - more space for platinum/silver)
            CarveCaves(120, 100, 180, 4, 7, 2500, Math.Min(3500, maxSafeDepth));
            // Bottom caves - FIXED: Stop before the absolute bottom to prevent falling off
            if (maxSafeDepth > 3500)
            {
                CarveCaves(80, 100, 200, 4, 8, 3500, maxSafeDepth);
            }
        }

        private void CarveCaves(int count, int minLength, int maxLength, int minRadius, int maxRadius, int minY, int maxY)
        {
            // Safety check to prevent invalid range
            if (minY >= maxY)
            {
                Logger.Log($"[WORLDGEN] Skipping cave layer - invalid depth range: {minY} to {maxY}");
                return;
            }
            
            for (int i = 0; i < count; i++)
            {
                int startX = random.Next(50, Claude4_5Terraria.World.World.WORLD_WIDTH - 50);
                int startY = random.Next(minY, maxY);
                CarveCaveTunnel(startX, startY, random.Next(minLength, maxLength), minRadius, maxRadius);
            }
        }

        private void CarveCaveTunnel(int startX, int startY, int length, int minRadius, int maxRadius)
        {
            double direction = random.NextDouble() * Math.PI * 2;
            int currentX = startX;
            int currentY = startY;

            for (int step = 0; step < length; step++)
            {
                int radius = random.Next(minRadius, maxRadius);

                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (dx * dx + dy * dy <= radius * radius)
                        {
                            int tileX = currentX + dx;
                            int tileY = currentY + dy;

                            var tile = world.GetTile(tileX, tileY);
                            if (tile != null && tile.IsActive && tile.Type != TileType.Grass)
                                world.SetTile(tileX, tileY, new Claude4_5Terraria.World.Tile(TileType.Air));
                        }
                    }
                }

                if (random.NextDouble() < 0.08 && step > 20)
                {
                    double branchDirection = direction + (random.NextDouble() - 0.5) * Math.PI;
                    int branchLength = random.Next(length / 3, length / 2);
                    CarveCaveTunnel(currentX, currentY, branchLength, minRadius, maxRadius);
                }

                direction += (random.NextDouble() - 0.5) * 0.5;

                if (currentY < 700)
                    direction += 0.05;

                int moveSpeed = random.Next(1, 3);
                currentX += (int)(Math.Cos(direction) * moveSpeed);
                currentY += (int)(Math.Sin(direction) * moveSpeed);

                // FIXED: Stop caves before reaching the absolute bottom
                if (currentX < 50 || currentX >= Claude4_5Terraria.World.World.WORLD_WIDTH - 50 ||
                    currentY < CAVE_START_DEPTH || currentY >= Claude4_5Terraria.World.World.WORLD_HEIGHT - 150)
                    break;
            }
        }

        private void PlaceCaveTorches()
        {
            int torchesPlaced = 0;

            // UPDATED: Place torches throughout the entire cave system
            for (int x = 50; x < Claude4_5Terraria.World.World.WORLD_WIDTH - 50; x += 8)
            {
                for (int y = CAVE_START_DEPTH; y < Claude4_5Terraria.World.World.WORLD_HEIGHT - 150; y += 8)
                {
                    var tile = world.GetTile(x, y);

                    if (tile == null || !tile.IsActive)
                    {
                        if (random.Next(0, 25) == 0)
                        {
                            int torchCount = random.Next(3, 8);

                            for (int i = 0; i < torchCount; i++)
                            {
                                int torchX = x + random.Next(-15, 16);
                                int torchY = y + random.Next(-10, 11);

                                var torchSpot = world.GetTile(torchX, torchY);
                                if (torchSpot == null || !torchSpot.IsActive)
                                {
                                    bool hasAdjacent = false;
                                    if (world.IsSolidAtPosition(torchX - 1, torchY) ||
                                        world.IsSolidAtPosition(torchX + 1, torchY) ||
                                        world.IsSolidAtPosition(torchX, torchY - 1) ||
                                        world.IsSolidAtPosition(torchX, torchY + 1))
                                    {
                                        hasAdjacent = true;
                                    }

                                    if (hasAdjacent)
                                    {
                                        world.SetTile(torchX, torchY, new Claude4_5Terraria.World.Tile(TileType.Torch));
                                        torchesPlaced++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Logger.Log($"[WORLDGEN] Placed {torchesPlaced} cave torches");
        }

        private void GenerateDirtPockets()
        {
            // UPDATED: More dirt pockets throughout the entire depth
            for (int i = 0; i < 1000; i++)
            {
                int centerX = random.Next(0, Claude4_5Terraria.World.World.WORLD_WIDTH);
                int centerY = random.Next(CAVE_START_DEPTH, Claude4_5Terraria.World.World.WORLD_HEIGHT - 150);
                int pocketSize = random.Next(6, 15);

                for (int j = 0; j < pocketSize; j++)
                {
                    int dirtX = centerX + random.Next(-4, 5);
                    int dirtY = centerY + random.Next(-4, 5);

                    var tile = world.GetTile(dirtX, dirtY);
                    if (tile != null && tile.Type == TileType.Stone)
                        world.SetTile(dirtX, dirtY, new Claude4_5Terraria.World.Tile(TileType.Dirt));
                }
            }
        }

        private void GenerateOres()
        {
            // UPDATED: Ore generation for full map depth
            // Coal - common, found early
            PlaceOreType(TileType.Coal, 12000, 210, Claude4_5Terraria.World.World.WORLD_HEIGHT - 150, 5, 15);
            
            // Copper - common, shallow to mid depth
            PlaceOreType(TileType.Copper, 5000, 230, 1800, 3, 10);
            
            // Silver - uncommon, mid to deep (FIXED: Much deeper range)
            PlaceOreType(TileType.Silver, 3500, 1500, 3000, 3, 8);
            
            // Platinum - rare, deep only (FIXED: Even deeper range)
            PlaceOreType(TileType.Platinum, 2000, 2500, Claude4_5Terraria.World.World.WORLD_HEIGHT - 200, 2, 6);
        }

        private void PlaceOreType(TileType oreType, int veinCount, int minDepth, int maxDepth, int minVeinSize, int maxVeinSize)
        {
            for (int i = 0; i < veinCount; i++)
            {
                int veinX = random.Next(0, Claude4_5Terraria.World.World.WORLD_WIDTH);
                int veinY = random.Next(minDepth, maxDepth);
                int veinSize = random.Next(minVeinSize, maxVeinSize);

                for (int j = 0; j < veinSize; j++)
                {
                    int oreX = veinX + random.Next(-4, 5);
                    int oreY = veinY + random.Next(-4, 5);

                    var tile = world.GetTile(oreX, oreY);
                    if (tile != null && tile.Type == TileType.Stone)
                        world.SetTile(oreX, oreY, new Claude4_5Terraria.World.Tile(oreType));
                }
            }
        }

        private void GenerateTrees()
        {
            int treeAttempts = 200;
            int treesPlaced = 0;

            for (int i = 0; i < treeAttempts; i++)
            {
                int x = random.Next(10, Claude4_5Terraria.World.World.WORLD_WIDTH - 10);
                int surfaceY = world.GetSurfaceHeight(x);

                var groundTile = world.GetTile(x, surfaceY);

                if (groundTile != null && (groundTile.Type == TileType.Grass || groundTile.Type == TileType.Dirt))
                {
                    bool hasSpace = true;
                    for (int checkY = surfaceY - 1; checkY > surfaceY - 15; checkY--)
                    {
                        var checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsActive) { hasSpace = false; break; }
                    }

                    if (!hasSpace) continue;

                    bool tooClose = false;
                    for (int checkX = x - 4; checkX <= x + 4; checkX++)
                    {
                        var checkTile = world.GetTile(checkX, surfaceY - 1);
                        if (checkTile != null && checkTile.Type == TileType.Wood) { tooClose = true; break; }
                    }

                    if (!tooClose)
                    {
                        PlaceTree(x, surfaceY);
                        treesPlaced++;
                    }
                }
            }

            Logger.Log($"[WORLDGEN] Generated {treesPlaced} trees");
        }

        private void PlaceTree(int baseX, int baseY)
        {
            int trunkHeight = random.Next(8, 15);
            var tree = new Claude4_5Terraria.World.Tree(baseX, baseY, trunkHeight);

            for (int y = 0; y < trunkHeight; y++)
            {
                int treeY = baseY - 1 - y;
                world.SetTile(baseX, treeY, new Claude4_5Terraria.World.Tile(TileType.Wood, true));
                tree.AddTile(baseX, treeY);
            }

            int canopyY = baseY - trunkHeight;
            int canopyRadius = random.Next(2, 4);

            for (int dx = -canopyRadius; dx <= canopyRadius; dx++)
            {
                for (int dy = -canopyRadius; dy <= canopyRadius; dy++)
                {
                    if (dx * dx + dy * dy <= canopyRadius * canopyRadius + 2)
                    {
                        int leafX = baseX + dx;
                        int leafY = canopyY + dy;

                        var existingTile = world.GetTile(leafX, leafY);
                        if (existingTile == null || !existingTile.IsActive || existingTile.Type != TileType.Wood)
                        {
                            world.SetTile(leafX, leafY, new Claude4_5Terraria.World.Tile(TileType.Leaves, true));
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }

            world.AddTree(tree);
        }

        private void GenerateGrass()
        {
            for (int x = 0; x < Claude4_5Terraria.World.World.WORLD_WIDTH; x++)
            {
                int surfaceY = world.GetSurfaceHeight(x);
                var surfaceTile = world.GetTile(x, surfaceY);

                if (surfaceTile != null && surfaceTile.Type == TileType.Dirt)
                {
                    var aboveTile = world.GetTile(x, surfaceY - 1);
                    if (aboveTile == null || !aboveTile.IsActive)
                        world.SetTile(x, surfaceY, new Claude4_5Terraria.World.Tile(TileType.Grass));
                }
            }
        }

        public Vector2 GetSpawnPosition(int playerPixelHeight)
        {
            int spawnX = Claude4_5Terraria.World.World.WORLD_WIDTH / 2;
            int surfaceY = world.GetSurfaceHeight(spawnX);

            Logger.Log($"[SPAWN] Initial spawn X: {spawnX}, Surface tile Y: {surfaceY}");

            // Clear any trees at spawn point (5 tile radius)
            for (int dx = -5; dx <= 5; dx++)
            {
                for (int dy = -15; dy <= 1; dy++)
                {
                    int clearX = spawnX + dx;
                    int clearY = surfaceY + dy;
                    var tile = world.GetTile(clearX, clearY);
                    if (tile != null && (tile.Type == TileType.Wood || tile.Type == TileType.Leaves))
                    {
                        world.RemoveTree(clearX, clearY);
                        Logger.Log($"[SPAWN] Cleared tree at spawn area: ({clearX}, {clearY})");
                    }
                }
            }

            Claude4_5Terraria.World.Tile groundTile = world.GetTile(spawnX, surfaceY);
            if (groundTile == null || !groundTile.IsActive)
            {
                for (int searchRadius = 1; searchRadius <= 50; searchRadius++)
                {
                    for (int dx = -searchRadius; dx <= searchRadius; dx++)
                    {
                        int checkX = spawnX + dx;
                        int checkY = world.GetSurfaceHeight(checkX);
                        Claude4_5Terraria.World.Tile checkGround = world.GetTile(checkX, checkY);

                        if (checkGround != null && checkGround.IsActive &&
                            (checkGround.Type == TileType.Grass || checkGround.Type == TileType.Dirt || checkGround.Type == TileType.Stone))
                        {
                            spawnX = checkX;
                            surfaceY = checkY;
                            groundTile = checkGround;
                            Logger.Log($"[SPAWN] Found valid ground at X: {spawnX}, Surface tile Y: {surfaceY}");
                            goto SpawnFound;
                        }
                    }
                }

            SpawnFound:
                if (groundTile == null || !groundTile.IsActive)
                {
                    surfaceY = SURFACE_LEVEL;
                    Logger.Log($"[SPAWN] No ground found, using default surface level: {surfaceY}");
                }
            }

            // Place player so bottom edge sits exactly on top of surface tile
            // Subtract 2 pixels to ensure player is clearly above ground and not stuck
            int surfacePixelY = surfaceY * Claude4_5Terraria.World.World.TILE_SIZE;
            int spawnPixelY = surfacePixelY - playerPixelHeight - 2; // -2 to ensure clearance
            int spawnPixelX = spawnX * Claude4_5Terraria.World.World.TILE_SIZE;

            Vector2 spawnPos = new Vector2(spawnPixelX, spawnPixelY);

            Logger.Log($"[SPAWN] Final spawn position: {spawnPos}");
            Logger.Log($"[SPAWN] Player bottom: {spawnPixelY + playerPixelHeight}, Surface top: {surfacePixelY}");

            return spawnPos;
        }
    }
}
