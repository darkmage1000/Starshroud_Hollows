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
        private ChestSystem chestSystem;

        private const int SURFACE_LEVEL = 200;
        private const int DIRT_LAYER_THICKNESS = 10;
        private const int CAVE_START_DEPTH = 230;

        public Action<float, string> OnProgressUpdate;
        public int GetSeed() => seed;

        public WorldGenerator(Claude4_5Terraria.World.World world, int seed = 0, ChestSystem chestSystem = null)
        {
            this.world = world;
            this.seed = seed == 0 ? Environment.TickCount : seed;
            this.random = new Random(this.seed);
            this.chestSystem = chestSystem;
        }

        public void Generate()
        {
            DateTime startTime = DateTime.Now;

            try
            {
                Logger.Log("[WORLDGEN] Starting world generation...");

                // Preload all chunks to ensure SetTile persists during generation
                Logger.Log("[WORLDGEN] Preloading all chunks...");
                int chunksWide = (Claude4_5Terraria.World.World.WORLD_WIDTH + Claude4_5Terraria.World.Chunk.CHUNK_SIZE - 1) / Claude4_5Terraria.World.Chunk.CHUNK_SIZE;
                int chunksHigh = (Claude4_5Terraria.World.World.WORLD_HEIGHT + Claude4_5Terraria.World.Chunk.CHUNK_SIZE - 1) / Claude4_5Terraria.World.Chunk.CHUNK_SIZE;
                for (int cx = 0; cx < chunksWide; cx++)
                {
                    for (int cy = 0; cy < chunksHigh; cy++)
                    {
                        Point chunkPos = new Point(cx, cy);
                        if (!world.loadedChunks.ContainsKey(chunkPos))
                        {
                            Claude4_5Terraria.World.Chunk chunk = new Claude4_5Terraria.World.Chunk(cx, cy);
                            chunk.IsLoaded = true;
                            world.loadedChunks[chunkPos] = chunk;
                        }
                    }
                }
                Logger.Log($"[WORLDGEN] Preloaded {world.loadedChunks.Count} chunks");

                OnProgressUpdate?.Invoke(0.0f, "Generating terrain...");
                GenerateTerrain();

                OnProgressUpdate?.Invoke(0.15f, "Carving caves...");
                GenerateCaves();

                // NEW STEP: Guarantee at least one chest per layer's major starting area
                OnProgressUpdate?.Invoke(0.25f, "Placing guaranteed chests...");
                PlaceGuaranteedChests();

                OnProgressUpdate?.Invoke(0.3f, "Placing torches...");
                PlaceCaveTorches();

                OnProgressUpdate?.Invoke(0.4f, "Adding dirt pockets...");
                GenerateDirtPockets();

                OnProgressUpdate?.Invoke(0.5f, "Generating ores...");
                GenerateOres();

                OnProgressUpdate?.Invoke(0.65f, "Placing random chests...");
                GenerateChests(); // Renamed to "random chests" for clarity

                OnProgressUpdate?.Invoke(0.7f, "Generating water...");
                GenerateWater();

                OnProgressUpdate?.Invoke(0.75f, "Generating lava caves...");
                GenerateLava();

                OnProgressUpdate?.Invoke(0.8f, "Converting lava+water to obsidian...");
                ConvertLavaWaterToObsidian();

                OnProgressUpdate?.Invoke(0.85f, "Planting trees...");
                GenerateTrees();

                OnProgressUpdate?.Invoke(0.92f, "Growing grass...");
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

        // This list will store the spawn points of the major tunnels for guaranteed chest placement
        private System.Collections.Generic.List<Point> caveStartPoints = new System.Collections.Generic.List<Point>();

        private void GenerateCaves()
        {
            int maxSafeDepth = Claude4_5Terraria.World.World.WORLD_HEIGHT - 150;
            // Counts increased by 20%
            caveStartPoints.Clear(); // Clear before generating
            CarveCaves(936, 20, 40, 1, 2, CAVE_START_DEPTH, Math.Min(600, maxSafeDepth));
            CarveCaves(468, 40, 80, 2, 4, CAVE_START_DEPTH, Math.Min(600, maxSafeDepth));
            CarveCaves(702, 60, 120, 2, 5, 600, Math.Min(1500, maxSafeDepth));
            CarveCaves(562, 50, 100, 3, 5, 1000, Math.Min(2000, maxSafeDepth));
            CarveCaves(655, 80, 150, 3, 6, 1500, Math.Min(2500, maxSafeDepth));
            if (maxSafeDepth > 2500)
            {
                CarveCaves(390, 100, 180, 4, 7, 2500, maxSafeDepth);
            }
        }

        private void CarveCaves(int count, int minLength, int maxLength, int minRadius, int maxRadius, int minY, int maxY)
        {
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

                // Track the start point of major tunnels for guaranteed chest placement
                // We'll track roughly 1/10th of the starting points as "major" spots for chests
                if (i % 10 == 0)
                {
                    caveStartPoints.Add(new Point(startX, startY));
                }
            }
        }

        private void PlaceGuaranteedChests()
        {
            if (chestSystem == null)
            {
                Logger.Log("[WORLDGEN] Warning: ChestSystem is null, skipping guaranteed chest generation");
                return;
            }

            int guaranteedChestsPlaced = 0;
            foreach (Point startPoint in caveStartPoints)
            {
                ChestTier tier = ChestTier.Wood;
                TileType tileType = TileType.WoodChest;
                int maxSearchY = 500;

                // Determine chest type based on the general depth layer of the tunnel's start point
                if (startPoint.Y >= 1900)
                {
                    tier = ChestTier.Magic;
                    tileType = TileType.MagicChest;
                    maxSearchY = 2300;
                }
                else if (startPoint.Y >= 1000)
                {
                    tier = ChestTier.Silver;
                    tileType = TileType.SilverChest;
                    maxSearchY = 1500;
                }

                // We'll search in a small area around the start point for a cave floor
                Point? floorPos = FindCaveFloorInArea(startPoint.X, startPoint.Y, maxSearchY, 20);

                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new Claude4_5Terraria.World.Tile(tileType));
                    chestSystem.PlaceChest(floorPos.Value, tier, true);
                    Chest chest = chestSystem.GetChest(floorPos.Value);
                    if (chest != null) chestSystem.GenerateChestLoot(chest, random);
                    guaranteedChestsPlaced++;
                }
            }
            Logger.Log($"[WORLDGEN] Placed {guaranteedChestsPlaced} guaranteed layer chests from {caveStartPoints.Count} tunnel start points.");
        }

        private Point? FindCaveFloorInArea(int centerX, int centerY, int maxY, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    Point? floorPos = FindCaveFloor(x, y, maxY);
                    if (floorPos.HasValue)
                        return floorPos.Value;
                }
            }
            return null;
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
                if (currentY < 700) direction += 0.05;

                int moveSpeed = random.Next(1, 3);
                currentX += (int)(Math.Cos(direction) * moveSpeed);
                currentY += (int)(Math.Sin(direction) * moveSpeed);

                if (currentX < 50 || currentX >= Claude4_5Terraria.World.World.WORLD_WIDTH - 50 ||
                    currentY < CAVE_START_DEPTH || currentY >= Claude4_5Terraria.World.World.WORLD_HEIGHT - 150)
                    break;
            }
        }

        private void PlaceCaveTorches()
        {
            int torchesPlaced = 0;
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
                                    if (world.IsSolidAtPosition(torchX - 1, torchY) ||
                                        world.IsSolidAtPosition(torchX + 1, torchY) ||
                                        world.IsSolidAtPosition(torchX, torchY - 1) ||
                                        world.IsSolidAtPosition(torchX, torchY + 1))
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
            for (int i = 0; i < 3900; i++)
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
            PlaceOreType(TileType.Coal, 46800, 210, 1900, 5, 9);
            PlaceOreType(TileType.Copper, 42900, 210, 1800, 5, 7);
            PlaceOreType(TileType.Silver, 23400, 1150, Claude4_5Terraria.World.World.WORLD_HEIGHT - 200, 5, 7);
            PlaceOreType(TileType.Platinum, 19500, 1900, Claude4_5Terraria.World.World.WORLD_HEIGHT - 200, 5, 7);
        }

        private void PlaceOreType(TileType oreType, int veinCount, int minDepth, int maxDepth, int minVeinSize, int maxVeinSize)
        {
            for (int i = 0; i < veinCount; i++)
            {
                int startX = random.Next(0, Claude4_5Terraria.World.World.WORLD_WIDTH);
                int startY = random.Next(minDepth, maxDepth);
                int veinLength = random.Next(minVeinSize, maxVeinSize + 1);
                int veinThickness = random.Next(1, 3);
                double direction = random.NextDouble() * Math.PI * 2;
                int currentX = startX;
                int currentY = startY;

                for (int step = 0; step < veinLength; step++)
                {
                    for (int dx = -veinThickness / 2; dx <= veinThickness / 2; dx++)
                    {
                        for (int dy = -veinThickness / 2; dy <= veinThickness / 2; dy++)
                        {
                            if (dx * dx + dy * dy <= (veinThickness / 2) * (veinThickness / 2))
                            {
                                int oreX = currentX + dx;
                                int oreY = currentY + dy;
                                if (oreX >= 0 && oreX < Claude4_5Terraria.World.World.WORLD_WIDTH &&
                                    oreY >= 0 && oreY < Claude4_5Terraria.World.World.WORLD_HEIGHT)
                                {
                                    var tile = world.GetTile(oreX, oreY);
                                    if (tile != null && tile.Type == TileType.Stone)
                                        world.SetTile(oreX, oreY, new Claude4_5Terraria.World.Tile(oreType));
                                }
                            }
                        }
                    }
                    direction += (random.NextDouble() - 0.5) * 0.8;
                    if (random.NextDouble() < 0.3) direction += 0.1;
                    int moveStep = random.Next(1, 3);
                    currentX += (int)(Math.Cos(direction) * moveStep);
                    currentY += (int)(Math.Sin(direction) * moveStep);
                    if (currentX < 0 || currentX >= Claude4_5Terraria.World.World.WORLD_WIDTH ||
                        currentY < minDepth || currentY > maxDepth) break;
                }
            }
        }

        private void GenerateChests()
        {
            if (chestSystem == null)
            {
                Logger.Log("[WORLDGEN] Warning: ChestSystem is null, skipping chest generation");
                return;
            }

            int woodChestsPlaced = 0;
            int silverChestsPlaced = 0;
            int magicChestsPlaced = 0;

            // Updated Wood Chest attempts to 1200
            for (int attempt = 0; attempt < 1200; attempt++)
            {
                int x = random.Next(50, Claude4_5Terraria.World.World.WORLD_WIDTH - 50);
                int y = random.Next(250, 500);
                Point? floorPos = FindCaveFloor(x, y, 250);
                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new Claude4_5Terraria.World.Tile(TileType.WoodChest));
                    chestSystem.PlaceChest(floorPos.Value, ChestTier.Wood, true);
                    Chest chest = chestSystem.GetChest(floorPos.Value);
                    if (chest != null) chestSystem.GenerateChestLoot(chest, random);
                    woodChestsPlaced++;
                }
            }

            // Updated Silver Chest attempts to 500
            for (int attempt = 0; attempt < 500; attempt++)
            {
                int x = random.Next(50, Claude4_5Terraria.World.World.WORLD_WIDTH - 50);
                int y = random.Next(1250, 1500);
                Point? floorPos = FindCaveFloor(x, y, 1250);
                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new Claude4_5Terraria.World.Tile(TileType.SilverChest));
                    chestSystem.PlaceChest(floorPos.Value, ChestTier.Silver, true);
                    Chest chest = chestSystem.GetChest(floorPos.Value);
                    if (chest != null) chestSystem.GenerateChestLoot(chest, random);
                    silverChestsPlaced++;
                }
            }

            // Magic Chest attempts remain at 50 (rare)
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = random.Next(50, Claude4_5Terraria.World.World.WORLD_WIDTH - 50);
                int y = random.Next(2000, Math.Min(2300, Claude4_5Terraria.World.World.WORLD_HEIGHT - 150));
                Point? floorPos = FindCaveFloor(x, y, 2000);
                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new Claude4_5Terraria.World.Tile(TileType.MagicChest));
                    chestSystem.PlaceChest(floorPos.Value, ChestTier.Magic, true);
                    Chest chest = chestSystem.GetChest(floorPos.Value);
                    if (chest != null) chestSystem.GenerateChestLoot(chest, random);
                    magicChestsPlaced++;
                }
            }

            Logger.Log($"[WORLDGEN] Placed {woodChestsPlaced} random wood chests, {silverChestsPlaced} random silver chests, {magicChestsPlaced} random magic chests");
        }

        private Point? FindCaveFloor(int startX, int startY, int maxY)
        {
            for (int checkY = startY; checkY <= maxY && checkY < Claude4_5Terraria.World.World.WORLD_HEIGHT - 1; checkY++)
            {
                var currentTile = world.GetTile(startX, checkY);
                var belowTile = world.GetTile(startX, checkY + 1);
                var aboveTile = world.GetTile(startX, checkY - 1);
                if ((currentTile == null || !currentTile.IsActive) &&
                    belowTile != null && belowTile.IsActive &&
                    aboveTile != null && !aboveTile.IsActive)
                {
                    return new Point(startX, checkY);
                }
            }
            return null;
        }

        private bool IsValidChestSpot(Point position)
        {
            var tile = world.GetTile(position.X, position.Y);
            var above = world.GetTile(position.X, position.Y - 1);
            var below = world.GetTile(position.X, position.Y + 1);
            if (tile != null && tile.IsActive) return false;
            if (above != null && above.IsActive) return false;
            if (below == null || !below.IsActive) return false;

            for (int dx = -10; dx <= 10; dx++)
            {
                for (int dy = -10; dy <= 10; dy++)
                {
                    var checkTile = world.GetTile(position.X + dx, position.Y + dy);
                    if (checkTile != null && (checkTile.Type == TileType.WoodChest ||
                        checkTile.Type == TileType.SilverChest ||
                        checkTile.Type == TileType.MagicChest))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void GenerateTrees()
        {
            int treesPlaced = 0;
            for (int i = 0; i < 600; i++)  // INCREASED from 200 to 600
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

        private void GenerateLava()
        {
            int lavaCavesPlaced = 0;
            int lavaPoolsPlaced = 0;
            
            // 1. Generate large lava caves at deep depths (1800-2500)
            for (int i = 0; i < 30; i++)
            {
                int startX = random.Next(100, Claude4_5Terraria.World.World.WORLD_WIDTH - 100);
                int startY = random.Next(1800, Math.Min(2500, Claude4_5Terraria.World.World.WORLD_HEIGHT - 200));
                int caveLength = random.Next(60, 120);
                
                if (CarveLavaCave(startX, startY, caveLength))
                {
                    lavaCavesPlaced++;
                }
            }
            
            // 2. Generate lava pools in existing caves (1500+)
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = random.Next(100, Claude4_5Terraria.World.World.WORLD_WIDTH - 100);
                int y = random.Next(1500, Claude4_5Terraria.World.World.WORLD_HEIGHT - 200);
                
                // Check if this is an open cave area
                var tile = world.GetTile(x, y);
                if (tile == null || !tile.IsActive)
                {
                    // Try to create a lava pool here
                    if (CreateLavaPool(x, y))
                    {
                        lavaPoolsPlaced++;
                    }
                }
            }
            
            Logger.Log($"[WORLDGEN] Generated {lavaCavesPlaced} lava caves and {lavaPoolsPlaced} lava pools");
        }
        
        private bool CarveLavaCave(int startX, int startY, int length)
        {
            double direction = random.NextDouble() * Math.PI * 2;
            int currentX = startX;
            int currentY = startY;
            bool lavaPlaced = false;
            
            for (int step = 0; step < length; step++)
            {
                int radius = random.Next(3, 6);
                
                // Carve out the cave
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
                            {
                                world.SetTile(tileX, tileY, new Claude4_5Terraria.World.Tile(TileType.Air));
                            }
                        }
                    }
                }
                
                // Fill bottom portion with lava
                int lavaRadius = radius - 1;
                for (int dx = -lavaRadius; dx <= lavaRadius; dx++)
                {
                    // Only fill the bottom 1/3 of the cave with lava
                    for (int dy = 0; dy <= lavaRadius / 2; dy++)
                    {
                        int tileX = currentX + dx;
                        int tileY = currentY + dy;
                        
                        var tile = world.GetTile(tileX, tileY);
                        if (tile == null || !tile.IsActive)
                        {
                            world.SetTile(tileX, tileY, new Claude4_5Terraria.World.Tile(TileType.Lava));
                            lavaPlaced = true;
                        }
                    }
                }
                
                // Random direction change
                direction += (random.NextDouble() - 0.5) * 0.4;
                // Slight tendency to go down
                direction += 0.03;
                
                int moveSpeed = random.Next(2, 4);
                currentX += (int)(Math.Cos(direction) * moveSpeed);
                currentY += (int)(Math.Sin(direction) * moveSpeed);
                
                // Stop if out of bounds
                if (currentX < 100 || currentX >= Claude4_5Terraria.World.World.WORLD_WIDTH - 100 ||
                    currentY < 1800 || currentY >= Claude4_5Terraria.World.World.WORLD_HEIGHT - 200)
                    break;
            }
            
            return lavaPlaced;
        }
        
        private bool CreateLavaPool(int centerX, int centerY)
        {
            // Find the floor below this position
            int floorY = centerY;
            bool foundFloor = false;
            
            for (int checkY = centerY; checkY < Math.Min(centerY + 30, Claude4_5Terraria.World.World.WORLD_HEIGHT - 1); checkY++)
            {
                var tile = world.GetTile(centerX, checkY);
                if (tile != null && tile.IsActive)
                {
                    floorY = checkY;
                    foundFloor = true;
                    break;
                }
            }
            
            if (!foundFloor) return false;
            
            // Create a pool of lava on the floor
            int poolWidth = random.Next(4, 10);
            int poolDepth = random.Next(2, 4);
            bool lavaPlaced = false;
            
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = -poolDepth; dy < 0; dy++)
                {
                    int lavaX = centerX + dx;
                    int lavaY = floorY + dy;
                    
                    var tile = world.GetTile(lavaX, lavaY);
                    if (tile == null || !tile.IsActive)
                    {
                        world.SetTile(lavaX, lavaY, new Claude4_5Terraria.World.Tile(TileType.Lava));
                        lavaPlaced = true;
                    }
                }
            }
            
            return lavaPlaced;
        }

        private void GenerateWater()
        {
            int surfacePoolsPlaced = 0;
            int smallUndergroundPoolsPlaced = 0;
            int mediumUndergroundPoolsPlaced = 0;
            int largeUndergroundPoolsPlaced = 0;
            
            // 1. Generate surface water pools (small ponds)
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = random.Next(100, Claude4_5Terraria.World.World.WORLD_WIDTH - 100);
                int surfaceY = world.GetSurfaceHeight(x);
                
                // Only place on relatively flat areas
                bool isFlat = true;
                for (int checkX = x - 10; checkX <= x + 10; checkX++)
                {
                    int checkY = world.GetSurfaceHeight(checkX);
                    if (Math.Abs(checkY - surfaceY) > 3)
                    {
                        isFlat = false;
                        break;
                    }
                }
                
                if (isFlat && CreateSurfaceWaterPool(x, surfaceY))
                {
                    surfacePoolsPlaced++;
                }
            }
            
            // 2. Generate small underground pools (depth 300-1000)
            for (int attempt = 0; attempt < 150; attempt++)
            {
                int x = random.Next(100, Claude4_5Terraria.World.World.WORLD_WIDTH - 100);
                int y = random.Next(300, 1000);
                
                if (CreateSmallUndergroundPool(x, y))
                {
                    smallUndergroundPoolsPlaced++;
                }
            }
            
            // 3. Generate medium underground pools (depth 800-1800)
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int x = random.Next(100, Claude4_5Terraria.World.World.WORLD_WIDTH - 100);
                int y = random.Next(800, 1800);
                
                if (CreateMediumUndergroundPool(x, y))
                {
                    mediumUndergroundPoolsPlaced++;
                }
            }
            
            // 4. Generate large underground pools/lakes (depth 1200-2200)
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = random.Next(150, Claude4_5Terraria.World.World.WORLD_WIDTH - 150);
                int y = random.Next(1200, 2200);
                
                if (CreateLargeUndergroundPool(x, y))
                {
                    largeUndergroundPoolsPlaced++;
                }
            }
            
            Logger.Log($"[WORLDGEN] Generated {surfacePoolsPlaced} surface pools, {smallUndergroundPoolsPlaced} small underground pools, {mediumUndergroundPoolsPlaced} medium pools, {largeUndergroundPoolsPlaced} large pools");
        }
        
        private bool CreateSurfaceWaterPool(int centerX, int surfaceY)
        {
            int poolWidth = random.Next(8, 16);
            int poolDepth = random.Next(3, 6);
            bool waterPlaced = false;
            
            // Dig out the pool area
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                // Create rounded edges
                int edgeDepth = poolDepth;
                if (Math.Abs(dx) > poolWidth / 3)
                {
                    edgeDepth = poolDepth / 2;
                }
                
                for (int dy = 1; dy <= edgeDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;
                    
                    var tile = world.GetTile(poolX, poolY);
                    if (tile != null && tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new Claude4_5Terraria.World.Tile(TileType.Air));
                    }
                }
            }
            
            // Fill with water
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                int edgeDepth = poolDepth;
                if (Math.Abs(dx) > poolWidth / 3)
                {
                    edgeDepth = poolDepth / 2;
                }
                
                for (int dy = 1; dy <= edgeDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;
                    
                    var tile = world.GetTile(poolX, poolY);
                    if (tile == null || !tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new Claude4_5Terraria.World.Tile(TileType.Water));
                        waterPlaced = true;
                    }
                }
            }
            
            return waterPlaced;
        }
        
        private bool CreateSmallUndergroundPool(int centerX, int centerY)
        {
            int floorY = -1;
            bool foundCave = false;
            
            for (int checkY = centerY; checkY < Math.Min(centerY + 50, Claude4_5Terraria.World.World.WORLD_HEIGHT - 1); checkY++)
            {
                var tile = world.GetTile(centerX, checkY);
                var tileBelow = world.GetTile(centerX, checkY + 1);
                
                if ((tile == null || !tile.IsActive) && tileBelow != null && tileBelow.IsActive)
                {
                    floorY = checkY;
                    foundCave = true;
                    break;
                }
            }
            
            if (!foundCave) return false;
            
            int poolWidth = random.Next(4, 8);
            int poolDepth = random.Next(2, 3);
            bool waterPlaced = false;
            
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = -poolDepth; dy < 0; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    
                    var tile = world.GetTile(poolX, poolY);
                    if (tile == null || !tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new Claude4_5Terraria.World.Tile(TileType.Water));
                        waterPlaced = true;
                    }
                }
            }
            
            return waterPlaced;
        }
        
        private bool CreateMediumUndergroundPool(int centerX, int centerY)
        {
            int floorY = -1;
            bool foundCave = false;
            
            for (int checkY = centerY; checkY < Math.Min(centerY + 50, Claude4_5Terraria.World.World.WORLD_HEIGHT - 1); checkY++)
            {
                var tile = world.GetTile(centerX, checkY);
                var tileBelow = world.GetTile(centerX, checkY + 1);
                
                if ((tile == null || !tile.IsActive) && tileBelow != null && tileBelow.IsActive)
                {
                    floorY = checkY;
                    foundCave = true;
                    break;
                }
            }
            
            if (!foundCave) return false;
            
            int poolWidth = random.Next(10, 16);
            int poolDepth = random.Next(4, 6);
            bool waterPlaced = false;
            
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = -poolDepth; dy < 0; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    
                    var tile = world.GetTile(poolX, poolY);
                    if (tile == null || !tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new Claude4_5Terraria.World.Tile(TileType.Water));
                        waterPlaced = true;
                    }
                }
            }
            
            return waterPlaced;
        }
        
        private bool CreateLargeUndergroundPool(int centerX, int centerY)
        {
            int floorY = -1;
            bool foundCave = false;
            
            for (int checkY = centerY; checkY < Math.Min(centerY + 60, Claude4_5Terraria.World.World.WORLD_HEIGHT - 1); checkY++)
            {
                var tile = world.GetTile(centerX, checkY);
                var tileBelow = world.GetTile(centerX, checkY + 1);
                
                if ((tile == null || !tile.IsActive) && tileBelow != null && tileBelow.IsActive)
                {
                    floorY = checkY;
                    foundCave = true;
                    break;
                }
            }
            
            if (!foundCave) return false;
            
            int poolWidth = random.Next(20, 35);
            int poolDepth = random.Next(8, 12);
            bool waterPlaced = false;
            
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                float edgeFactor = 1.0f - (Math.Abs(dx) / (float)(poolWidth / 2));
                int localDepth = (int)(poolDepth * (0.5f + edgeFactor * 0.5f));
                
                for (int dy = -localDepth; dy < 0; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    
                    var tile = world.GetTile(poolX, poolY);
                    if (tile == null || !tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new Claude4_5Terraria.World.Tile(TileType.Water));
                        waterPlaced = true;
                    }
                }
            }
            
            return waterPlaced;
        }
        
        private void ConvertLavaWaterToObsidian()
        {
            int obsidianCreated = 0;
            
            for (int x = 1; x < Claude4_5Terraria.World.World.WORLD_WIDTH - 1; x++)
            {
                for (int y = 1; y < Claude4_5Terraria.World.World.WORLD_HEIGHT - 1; y++)
                {
                    var tile = world.GetTile(x, y);
                    
                    if (tile != null && tile.IsActive && tile.Type == TileType.Lava)
                    {
                        bool hasAdjacentWater = false;
                        
                        var left = world.GetTile(x - 1, y);
                        var right = world.GetTile(x + 1, y);
                        var up = world.GetTile(x, y - 1);
                        var down = world.GetTile(x, y + 1);
                        
                        if ((left != null && left.IsActive && left.Type == TileType.Water) ||
                            (right != null && right.IsActive && right.Type == TileType.Water) ||
                            (up != null && up.IsActive && up.Type == TileType.Water) ||
                            (down != null && down.IsActive && down.Type == TileType.Water))
                        {
                            hasAdjacentWater = true;
                        }
                        
                        if (hasAdjacentWater)
                        {
                            world.SetTile(x, y, new Claude4_5Terraria.World.Tile(TileType.Obsidian));
                            obsidianCreated++;
                        }
                    }
                }
            }
            
            Logger.Log($"[WORLDGEN] Created {obsidianCreated} obsidian blocks from lava+water interactions");
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
                            goto SpawnFound;
                        }
                    }
                }
            SpawnFound:
                if (groundTile == null || !groundTile.IsActive) surfaceY = SURFACE_LEVEL;
            }

            int surfacePixelY = surfaceY * Claude4_5Terraria.World.World.TILE_SIZE;
            int spawnPixelY = surfacePixelY - playerPixelHeight - 2;
            int spawnPixelX = spawnX * Claude4_5Terraria.World.World.TILE_SIZE;
            return new Vector2(spawnPixelX, spawnPixelY);
        }
    }
}