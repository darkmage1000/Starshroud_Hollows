using System;
using Microsoft.Xna.Framework;
using StarshroudHollows.Enums;
using StarshroudHollows.Systems;

namespace StarshroudHollows.Systems
{
    public class WorldGenerator
    {
        private Random random;
        private StarshroudHollows.World.World world;
        private int seed;
        private ChestSystem chestSystem;
        private System.Collections.Generic.HashSet<Point> modifiedTilePositions; // NEW: Track player-modified tiles

        private const int SURFACE_LEVEL = 200;
        private const int DIRT_LAYER_THICKNESS = 10;
        private const int CAVE_START_DEPTH = 230;

        // Snow biome fields
        private int snowBiomeStartX;
        private int snowBiomeEndX;
        private const int SNOW_BIOME_DEPTH = 2000;

        // Swamp biome fields
        private int swampBiomeStartX;
        private int swampBiomeEndX;
        private const int SWAMP_BIOME_DEPTH = 2000;

        // Jungle biome fields
        private int jungleBiomeStartX;
        private int jungleBiomeEndX;
        private const int JUNGLE_BIOME_DEPTH = 2000;

        // Volcanic biome fields
        private int volcanicBiomeStartX;
        private int volcanicBiomeEndX;
        private const int VOLCANIC_BIOME_DEPTH = 2000;

        public Action<float, string> OnProgressUpdate;
        public int GetSeed() => seed;
        public int GetSnowBiomeStartX() => snowBiomeStartX;
        public int GetSnowBiomeEndX() => snowBiomeEndX;
        public int GetSwampBiomeStartX() => swampBiomeStartX;
        public int GetSwampBiomeEndX() => swampBiomeEndX;
        public int GetJungleBiomeStartX() => jungleBiomeStartX;
        public int GetJungleBiomeEndX() => jungleBiomeEndX;
        public int GetVolcanicBiomeStartX() => volcanicBiomeStartX;
        public int GetVolcanicBiomeEndX() => volcanicBiomeEndX;

        public WorldGenerator(StarshroudHollows.World.World world, int seed = 0, ChestSystem chestSystem = null)
        {
            this.world = world;
            this.seed = seed == 0 ? Environment.TickCount : seed;
            this.random = new Random(this.seed);
            this.chestSystem = chestSystem;
            this.modifiedTilePositions = new System.Collections.Generic.HashSet<Point>();
        }
        
        // NEW: Set modified tile positions before generation to prevent tree spawning
        public void SetModifiedTilePositions(System.Collections.Generic.HashSet<Point> positions)
        {
            modifiedTilePositions = positions ?? new System.Collections.Generic.HashSet<Point>();
            Logger.Log($"[WORLDGEN] Loaded {modifiedTilePositions.Count} modified tile positions to protect from tree spawning");
        }

        public void Generate()
        {
            DateTime startTime = DateTime.Now;

            try
            {
                Logger.Log("[WORLDGEN] Starting world generation...");

                // Preload all chunks to ensure SetTile persists during generation
                Logger.Log("[WORLDGEN] Preloading all chunks...");
                int chunksWide = (StarshroudHollows.World.World.WORLD_WIDTH + StarshroudHollows.World.Chunk.CHUNK_SIZE - 1) / StarshroudHollows.World.Chunk.CHUNK_SIZE;
                int chunksHigh = (StarshroudHollows.World.World.WORLD_HEIGHT + StarshroudHollows.World.Chunk.CHUNK_SIZE - 1) / StarshroudHollows.World.Chunk.CHUNK_SIZE;
                for (int cx = 0; cx < chunksWide; cx++)
                {
                    for (int cy = 0; cy < chunksHigh; cy++)
                    {
                        Point chunkPos = new Point(cx, cy);
                        if (!world.loadedChunks.ContainsKey(chunkPos))
                        {
                            StarshroudHollows.World.Chunk chunk = new StarshroudHollows.World.Chunk(cx, cy);
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
                
                OnProgressUpdate?.Invoke(0.2f, "Placing natural walls...");
                GenerateNaturalWalls();

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

                // Liquid Generation Steps
                OnProgressUpdate?.Invoke(0.7f, "Generating water...");
                GenerateWater();

                OnProgressUpdate?.Invoke(0.75f, "Generating lava caves...");
                GenerateLava();

                OnProgressUpdate?.Invoke(0.80f, "Converting lava+water to obsidian...");
                ConvertLavaWaterToObsidian();

                // CRITICAL NEW STEP: Stabilize all liquids
                float stabilizeStart = 0.85f;
                float stabilizeEnd = 0.90f;
                //OnProgressUpdate?.Invoke(stabilizeStart, "Stabilizing liquid flow...");
               // world.StabilizeLiquids(stabilizeStart, stabilizeEnd, OnProgressUpdate);

                // Final steps
                // CRITICAL FIX: Generate biomes BEFORE trees so tree placement knows what biome it's in
                OnProgressUpdate?.Invoke(0.93f, "Growing grass...");
                GenerateGrass();

                OnProgressUpdate?.Invoke(0.94f, "Generating snow biome...");
                GenerateSnowBiome();

                OnProgressUpdate?.Invoke(0.95f, "Generating swamp biome...");
                GenerateSwampBiome();

                OnProgressUpdate?.Invoke(0.96f, "Generating jungle biome...");
                GenerateJungleBiome();

                OnProgressUpdate?.Invoke(0.97f, "Generating volcanic biome...");
                GenerateVolcanicBiome();

                // NOW plant trees - they will detect biome and place correct type
                OnProgressUpdate?.Invoke(0.98f, "Planting trees...");
                GenerateTrees();

                OnProgressUpdate?.Invoke(0.99f, "Cleaning water around trees...");
                RemoveWaterAroundTrees();

                // NEW: Underground mini-biomes
                OnProgressUpdate?.Invoke(0.996f, "Generating spider nests...");
                GenerateSpiderBiomes();

                OnProgressUpdate?.Invoke(0.998f, "Generating worm colonies...");
                GenerateWormBiomes();

                // CRITICAL FIX: Remove floating liquids after biome generation
                OnProgressUpdate?.Invoke(0.997f, "Cleaning up floating liquids...");
                RemoveFloatingLiquids();

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
            int centerX = StarshroudHollows.World.World.WORLD_WIDTH / 2;
            int flatZoneRadius = 25;
            int transitionZone = 25;

            for (int x = 0; x < StarshroudHollows.World.World.WORLD_WIDTH; x++)
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

                for (int y = 0; y < StarshroudHollows.World.World.WORLD_HEIGHT; y++)
                {
                    if (y < surfaceHeight) continue;
                    else if (y == surfaceHeight)
                        world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Grass));
                    else if (y < surfaceHeight + DIRT_LAYER_THICKNESS)
                        world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Dirt));
                    else
                        world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Stone));
                }
            }
        }

        // This list will store the spawn points of the major tunnels for guaranteed chest placement
        private System.Collections.Generic.List<Point> caveStartPoints = new System.Collections.Generic.List<Point>();

        private void GenerateCaves()
        {
            int maxSafeDepth = StarshroudHollows.World.World.WORLD_HEIGHT - 150;
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

        private void GenerateNaturalWalls()
        {
            int wallsPlaced = 0;
            
            // Add natural walls behind underground tiles
            for (int x = 0; x < StarshroudHollows.World.World.WORLD_WIDTH; x++)
            {
                for (int y = CAVE_START_DEPTH; y < StarshroudHollows.World.World.WORLD_HEIGHT - 150; y++)
                {
                    var tile = world.GetTile(x, y);
                    
                    // Only add walls to solid tiles underground
                    if (tile != null && tile.IsActive && !tile.IsPartOfTree)
                    {
                        // Determine wall type based on depth and tile type
                        TileType wallType = TileType.DirtWall;
                        
                        // Dirt walls in upper layers (230-600)
                        if (y < 600)
                        {
                            wallType = TileType.DirtWall;
                        }
                        // Stone walls in deeper layers (600+)
                        else
                        {
                            wallType = TileType.StoneWall;
                        }
                        
                        // Don't place walls behind chests, torches, or special tiles
                        if (tile.Type != TileType.WoodChest && tile.Type != TileType.SilverChest && 
                            tile.Type != TileType.MagicChest && tile.Type != TileType.Torch &&
                            tile.Type != TileType.WoodCraftingBench && tile.Type != TileType.CopperCraftingBench)
                        {
                            tile.WallType = wallType;
                            wallsPlaced++;
                        }
                    }
                }
            }
            
            Logger.Log($"[WORLDGEN] Placed {wallsPlaced} natural background walls");
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
                int startX = random.Next(50, StarshroudHollows.World.World.WORLD_WIDTH - 50);
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
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new StarshroudHollows.World.Tile(tileType));
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
                                world.SetTile(tileX, tileY, new StarshroudHollows.World.Tile(TileType.Air));
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

                if (currentX < 50 || currentX >= StarshroudHollows.World.World.WORLD_WIDTH - 50 ||
                    currentY < CAVE_START_DEPTH || currentY >= StarshroudHollows.World.World.WORLD_HEIGHT - 150)
                    break;
            }
        }

        private void PlaceCaveTorches()
        {
            int torchesPlaced = 0;
            for (int x = 50; x < StarshroudHollows.World.World.WORLD_WIDTH - 50; x += 8)
            {
                for (int y = CAVE_START_DEPTH; y < StarshroudHollows.World.World.WORLD_HEIGHT - 150; y += 8)
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
                                        world.SetTile(torchX, torchY, new StarshroudHollows.World.Tile(TileType.Torch));
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
                int centerX = random.Next(0, StarshroudHollows.World.World.WORLD_WIDTH);
                int centerY = random.Next(CAVE_START_DEPTH, StarshroudHollows.World.World.WORLD_HEIGHT - 150);
                int pocketSize = random.Next(6, 15);
                for (int j = 0; j < pocketSize; j++)
                {
                    int dirtX = centerX + random.Next(-4, 5);
                    int dirtY = centerY + random.Next(-4, 5);
                    var tile = world.GetTile(dirtX, dirtY);
                    if (tile != null && tile.Type == TileType.Stone)
                        world.SetTile(dirtX, dirtY, new StarshroudHollows.World.Tile(TileType.Dirt));
                }
            }
        }

        private void GenerateOres()
        {
            // Coal (Reduced by 10%)
            PlaceOreType(TileType.Coal, 42120, 210, 1900, 5, 9);

            // Copper Distribution (Reduced by 30%)
            PlaceOreType(TileType.Copper, 3500, 210, 400, 4, 7);
            PlaceOreType(TileType.Copper, 14000, 400, 700, 6, 9);
            PlaceOreType(TileType.Copper, 4900, 700, 900, 4, 7);
            PlaceOreType(TileType.Copper, 700, 900, 1000, 3, 5);

            // Iron Distribution (Reduced by 30%)
            PlaceOreType(TileType.Iron, 12600, 500, 900, 5, 8);
            PlaceOreType(TileType.Iron, 10500, 900, 1500, 5, 7);
            PlaceOreType(TileType.Iron, 4200, 1500, 1900, 4, 6);

            // Gold Distribution (Reduced by 30%)
            PlaceOreType(TileType.Gold, 3500, 1000, 1500, 5, 7);
            PlaceOreType(TileType.Gold, 8400, 1500, 1900, 6, 8);
            PlaceOreType(TileType.Gold, 2800, 1900, 2100, 4, 6);

            // Silver Distribution (Reduced by 30%)
            PlaceOreType(TileType.Silver, 2380, 1500, 1900, 5, 7);
            PlaceOreType(TileType.Silver, 10500, 1900, 2100, 6, 9);
            PlaceOreType(TileType.Silver, 3500, 2100, StarshroudHollows.World.World.WORLD_HEIGHT - 200, 4, 6);

            // Platinum Distribution (Reduced by 30%)
            PlaceOreType(TileType.Platinum, 3150, 1900, 2100, 5, 7);
            PlaceOreType(TileType.Platinum, 10500, 2100, StarshroudHollows.World.World.WORLD_HEIGHT - 200, 6, 9);
        }

        private void PlaceOreType(TileType oreType, int veinCount, int minDepth, int maxDepth, int minVeinSize, int maxVeinSize)
        {
            for (int i = 0; i < veinCount; i++)
            {
                int startX = random.Next(0, StarshroudHollows.World.World.WORLD_WIDTH);
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
                                if (oreX >= 0 && oreX < StarshroudHollows.World.World.WORLD_WIDTH &&
                                    oreY >= 0 && oreY < StarshroudHollows.World.World.WORLD_HEIGHT)
                                {
                                    var tile = world.GetTile(oreX, oreY);
                                    if (tile != null && tile.Type == TileType.Stone)
                                        world.SetTile(oreX, oreY, new StarshroudHollows.World.Tile(oreType));
                                }
                            }
                        }
                    }
                    direction += (random.NextDouble() - 0.5) * 0.8;
                    if (random.NextDouble() < 0.3) direction += 0.1;
                    int moveStep = random.Next(1, 3);
                    currentX += (int)(Math.Cos(direction) * moveStep);
                    currentY += (int)(Math.Sin(direction) * moveStep);
                    if (currentX < 0 || currentX >= StarshroudHollows.World.World.WORLD_WIDTH ||
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

            // Updated Wood Chest attempts to 1200 - FIXED: Deeper underground only
            for (int attempt = 0; attempt < 1200; attempt++)
            {
                int x = random.Next(50, StarshroudHollows.World.World.WORLD_WIDTH - 50);
                int y = random.Next(350, 600); // Changed from 250 to 350 to prevent surface spawning
                Point? floorPos = FindCaveFloor(x, y, 600); // Changed maxY from 250 to 600
                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new StarshroudHollows.World.Tile(TileType.WoodChest));
                    chestSystem.PlaceChest(floorPos.Value, ChestTier.Wood, true);
                    Chest chest = chestSystem.GetChest(floorPos.Value);
                    if (chest != null) chestSystem.GenerateChestLoot(chest, random);
                    woodChestsPlaced++;
                }
            }

            // Updated Silver Chest attempts to 500
            for (int attempt = 0; attempt < 500; attempt++)
            {
                int x = random.Next(50, StarshroudHollows.World.World.WORLD_WIDTH - 50);
                int y = random.Next(1250, 1500);
                Point? floorPos = FindCaveFloor(x, y, 1250);
                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new StarshroudHollows.World.Tile(TileType.SilverChest));
                    chestSystem.PlaceChest(floorPos.Value, ChestTier.Silver, true);
                    Chest chest = chestSystem.GetChest(floorPos.Value);
                    if (chest != null) chestSystem.GenerateChestLoot(chest, random);
                    silverChestsPlaced++;
                }
            }

            // Magic Chest attempts remain at 50 (rare)
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = random.Next(50, StarshroudHollows.World.World.WORLD_WIDTH - 50);
                int y = random.Next(2000, Math.Min(2300, StarshroudHollows.World.World.WORLD_HEIGHT - 150));
                Point? floorPos = FindCaveFloor(x, y, 2000);
                if (floorPos.HasValue && IsValidChestSpot(floorPos.Value))
                {
                    world.SetTile(floorPos.Value.X, floorPos.Value.Y, new StarshroudHollows.World.Tile(TileType.MagicChest));
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
            for (int checkY = startY; checkY <= maxY && checkY < StarshroudHollows.World.World.WORLD_HEIGHT - 1; checkY++)
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
                int x = random.Next(10, StarshroudHollows.World.World.WORLD_WIDTH - 10);
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
            int lavaPoolsPlaced = 0;

            // ✅ INCREASED: Generate CONTAINED lava pools at deep depths (1500-2500) - 200 attempts instead of 100
            for (int attempt = 0; attempt < 200; attempt++)
            {
                int x = random.Next(100, StarshroudHollows.World.World.WORLD_WIDTH - 100);
                int y = random.Next(1500, Math.Min(2500, StarshroudHollows.World.World.WORLD_HEIGHT - 200));

                // Create contained lava pool
                if (CreateContainedLavaPool(x, y))
                {
                    lavaPoolsPlaced++;
                }
            }

            Logger.Log($"[WORLDGEN] Generated {lavaPoolsPlaced} CONTAINED lava pools");
        }

        // CONTAINED lava pool - carved with stone container AT FLOOR LEVEL
        private bool CreateContainedLavaPool(int centerX, int centerY)
        {
            // CRITICAL FIX: Find the cave FLOOR first, not the ceiling
            int floorY = -1;
            bool foundFloor = false;

            // Search downward from centerY to find cave floor (air above, solid below)
            for (int checkY = centerY; checkY < Math.Min(centerY + 50, StarshroudHollows.World.World.WORLD_HEIGHT - 1); checkY++)
            {
                var tile = world.GetTile(centerX, checkY);
                var tileBelow = world.GetTile(centerX, checkY + 1);

                if ((tile == null || !tile.IsActive) && tileBelow != null && tileBelow.IsActive)
                {
                    floorY = checkY; // This is the floor (air space above solid)
                    foundFloor = true;
                    break;
                }
            }

            if (!foundFloor) return false; // No cave floor found

            // Pick pool size based on random
            int poolWidth = random.Next(6, 14);
            int poolDepth = random.Next(3, 8);
            bool lavaPlaced = false;

            // FIXED: Build pool from floor UPWARD (negative dy values)
            // Build stone container around pool
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                for (int dy = -poolDepth - 1; dy <= 0; dy++) // FIXED: Build upward from floor
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy; // FIXED: Use floorY instead of centerY

                    // Build walls on all sides and top (ceiling of pool)
                    if (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1 || dy == -poolDepth - 1 || dy == 0)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                    }
                }
            }

            // Clear interior (the pool cavity)
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = -poolDepth; dy < 0; dy++) // FIXED: Clear upward from floor
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy; // FIXED: Use floorY

                    world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                }
            }

            // Fill with lava FROM BOTTOM UP
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = -poolDepth; dy < 0; dy++) // FIXED: Fill upward from floor
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy; // FIXED: Use floorY

                    var lavaTile = new StarshroudHollows.World.Tile(TileType.Lava);
                    lavaTile.LiquidVolume = 1.0f; // Full volume
                    world.SetTile(poolX, poolY, lavaTile);
                    lavaPlaced = true;
                }
            }

            return lavaPlaced;
        }

        // REPLACED: Now builds a RECTANGULAR pool with a flat bottom
        private bool CreateSurfaceSwampPool(int centerX, int surfaceY)
        {
            int poolWidth = random.Next(6, 12);
            int poolDepth = random.Next(2, 4); // This is now the constant depth
            bool waterPlaced = false;

            // 1. Build dirt walls (Sides, Bottom, and Surface Lip)
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                // REMOVED edgeDepth logic

                for (int dy = 0; dy <= poolDepth + 1; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    if (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1 || dy == poolDepth + 1)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Dirt));
                    }
                    // Build the surface "lip"
                    else if (dy == 0)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Dirt));
                    }
                }
            }

            // 2. Clear interior
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                // REMOVED edgeDepth logic

                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;
                    world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                }
            }

            // 3. Fill with water
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                // REMOVED edgeDepth logic

                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    var waterTile = new StarshroudHollows.World.Tile(TileType.Water);
                    waterTile.WaterColor = new Color(80, 100, 60);
                    waterTile.LiquidVolume = 1.0f;
                    world.SetTile(poolX, poolY, waterTile);
                    waterPlaced = true;
                }
            }

            return waterPlaced;
        }

        private void GenerateWater()
        {
            int surfacePoolsPlaced = 0;
            int smallUndergroundPoolsPlaced = 0;
            int mediumUndergroundPoolsPlaced = 0;
            int largeUndergroundPoolsPlaced = 0;

            // 1. Generate surface water pools (small ponds) - CONTAINED
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = random.Next(100, StarshroudHollows.World.World.WORLD_WIDTH - 100);
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

                if (isFlat && CreateContainedSurfaceWaterPool(x, surfaceY))
                {
                    surfacePoolsPlaced++;
                }
            }

            // ✅ INCREASED: 2. Generate small underground pools (depth 300-1000) - 250 attempts instead of 150
            for (int attempt = 0; attempt < 250; attempt++)
            {
                int x = random.Next(100, StarshroudHollows.World.World.WORLD_WIDTH - 100);
                int y = random.Next(300, 1000);

                if (CreateContainedSmallUndergroundPool(x, y))
                {
                    smallUndergroundPoolsPlaced++;
                }
            }

            // ✅ INCREASED: 3. Generate medium underground pools (depth 800-1800) - 120 attempts instead of 80
            for (int attempt = 0; attempt < 120; attempt++)
            {
                int x = random.Next(100, StarshroudHollows.World.World.WORLD_WIDTH - 100);
                int y = random.Next(800, 1800);

                if (CreateContainedMediumUndergroundPool(x, y))
                {
                    mediumUndergroundPoolsPlaced++;
                }
            }

            // ✅ INCREASED: 4. Generate large underground pools/lakes (depth 1200-2200) - 50 attempts instead of 30
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = random.Next(150, StarshroudHollows.World.World.WORLD_WIDTH - 150);
                int y = random.Next(1200, 2200);

                if (CreateContainedLargeUndergroundPool(x, y))
                {
                    largeUndergroundPoolsPlaced++;
                }
            }

            Logger.Log($"[WORLDGEN] Generated {surfacePoolsPlaced} surface pools, {smallUndergroundPoolsPlaced} small underground pools, {mediumUndergroundPoolsPlaced} medium pools, {largeUndergroundPoolsPlaced} large pools (ALL CONTAINED)");
        }

        // REPLACED: Now builds a RECTANGULAR pool with a flat bottom
        private bool CreateContainedSurfaceWaterPool(int centerX, int surfaceY)
        {
            int poolWidth = random.Next(8, 16);
            int poolDepth = random.Next(3, 6); // This is now the constant depth
            bool waterPlaced = false;

            // 1. Build container walls (Sides, Bottom, and Surface Lip)
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                // REMOVED edgeDepth logic

                // Loop starts from dy = 0 (surface)
                for (int dy = 0; dy <= poolDepth + 1; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    // Build walls on sides (dx == ...) and bottom (dy == poolDepth + 1)
                    if (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1 || dy == poolDepth + 1)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                    }
                    // Build the surface "lip"
                    else if (dy == 0)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                    }
                }
            }

            // 2. Clear interior
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                // REMOVED edgeDepth logic

                for (int dy = 0; dy <= poolDepth; dy++) // Start at dy=0
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;
                    world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                }
            }

            // 3. Fill with water
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                // REMOVED edgeDepth logic

                for (int dy = 0; dy <= poolDepth; dy++) // Start at dy=0
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    var waterTile = new StarshroudHollows.World.Tile(TileType.Water);
                    waterTile.LiquidVolume = 1.0f; // Full volume
                    world.SetTile(poolX, poolY, waterTile);
                    waterPlaced = true;
                }
            }

            return waterPlaced;
        }

        // ✅ FIXED: Pool walls now extend from floor level (dy = 0) all the way down
        private bool CreateContainedSmallUndergroundPool(int centerX, int centerY)
        {
            int floorY = -1;
            bool foundCave = false;

            // Find a cave floor
            for (int checkY = centerY; checkY < Math.Min(centerY + 50, StarshroudHollows.World.World.WORLD_HEIGHT - 1); checkY++)
            {
                var tile = world.GetTile(centerX, checkY);
                var tileBelow = world.GetTile(centerX, checkY + 1);

                if ((tile == null || !tile.IsActive) && tileBelow != null && tileBelow.IsActive)
                {
                    floorY = checkY; // This is the air block right above the floor
                    foundCave = true;
                    break;
                }
            }

            if (!foundCave) return false;

            int poolWidth = random.Next(4, 8);
            int poolDepth = random.Next(2, 3);
            bool waterPlaced = false;

            // 1. Build container (Sides and Bottom) - FIXED: walls now extend to opening level
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                for (int dy = 0; dy <= poolDepth + 1; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    
                    // Build side walls (left and right edges) and bottom
                    // Skip the top interior (dy == 0 and inside the pool) to keep it open
                    bool isSideWall = (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1);
                    bool isBottom = (dy == poolDepth + 1);
                    
                    if (isSideWall || isBottom)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                    }
                }
            }

            // 2. Clear interior (including the air block we found)
            // Starts at dy = 0
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                }
            }

            // 3. Fill with water
            // Starts at dy = 0
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;

                    var waterTile = new StarshroudHollows.World.Tile(TileType.Water);
                    waterTile.LiquidVolume = 1.0f;
                    world.SetTile(poolX, poolY, waterTile);
                    waterPlaced = true;
                }
            }

            return waterPlaced;
        }

        // ✅ FIXED: Pool walls now extend from floor level (dy = 0) all the way down
        private bool CreateContainedMediumUndergroundPool(int centerX, int centerY)
        {
            int floorY = -1;
            bool foundCave = false;

            // Find a cave floor
            for (int checkY = centerY; checkY < Math.Min(centerY + 50, StarshroudHollows.World.World.WORLD_HEIGHT - 1); checkY++)
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

            // 1. Build container (Sides and Bottom) - FIXED: walls now extend to opening level
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                for (int dy = 0; dy <= poolDepth + 1; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    
                    // Build side walls (left and right edges) and bottom
                    bool isSideWall = (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1);
                    bool isBottom = (dy == poolDepth + 1);
                    
                    if (isSideWall || isBottom)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                    }
                }
            }

            // 2. Clear interior
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                }
            }

            // 3. Fill with water
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;

                    var waterTile = new StarshroudHollows.World.Tile(TileType.Water);
                    waterTile.LiquidVolume = 1.0f;
                    world.SetTile(poolX, poolY, waterTile);
                    waterPlaced = true;
                }
            }

            return waterPlaced;
        }

        // ✅ FIXED: Pool walls now extend from floor level (dy = 0) all the way down
        private bool CreateContainedLargeUndergroundPool(int centerX, int centerY)
        {
            int floorY = -1;
            bool foundCave = false;

            // Find a cave floor
            for (int checkY = centerY; checkY < Math.Min(centerY + 60, StarshroudHollows.World.World.WORLD_HEIGHT - 1); checkY++)
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

            // 1. Build container (Sides and Bottom) - FIXED: walls now extend to opening level
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                float edgeFactor = 1.0f - (Math.Abs(dx) / (float)(poolWidth / 2));
                int localDepth = (int)(poolDepth * (0.5f + edgeFactor * 0.5f));

                for (int dy = 0; dy <= localDepth + 1; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    
                    // Build side walls (left and right edges) and bottom
                    bool isSideWall = (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1);
                    bool isBottom = (dy == localDepth + 1);
                    
                    if (isSideWall || isBottom)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                    }
                }
            }

            // 2. Clear interior
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                float edgeFactor = 1.0f - (Math.Abs(dx) / (float)(poolWidth / 2));
                int localDepth = (int)(poolDepth * (0.5f + edgeFactor * 0.5f));

                for (int dy = 0; dy <= localDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;
                    world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                }
            }

            // 3. Fill with water
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                float edgeFactor = 1.0f - (Math.Abs(dx) / (float)(poolWidth / 2));
                int localDepth = (int)(poolDepth * (0.5f + edgeFactor * 0.5f));

                for (int dy = 0; dy <= localDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;

                    var waterTile = new StarshroudHollows.World.Tile(TileType.Water);
                    waterTile.LiquidVolume = 1.0f;
                    world.SetTile(poolX, poolY, waterTile);
                    waterPlaced = true;
                }
            }

            return waterPlaced;
        }

        private void ConvertLavaWaterToObsidian()
        {
            int obsidianCreated = 0;

            for (int x = 1; x < StarshroudHollows.World.World.WORLD_WIDTH - 1; x++)
            {
                for (int y = 1; y < StarshroudHollows.World.World.WORLD_HEIGHT - 1; y++)
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
                            world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Obsidian));
                            obsidianCreated++;
                        }
                    }
                }
            }

            Logger.Log($"[WORLDGEN] Created {obsidianCreated} obsidian blocks from lava+water interactions");
        }

        private void RemoveWaterAroundTrees()
        {
            int waterRemoved = 0;
            
            // Go through all trees and remove water in/around them
            foreach (var tree in world.trees)
            {
                // Remove water in a radius around the tree base
                int clearRadius = 6;
                for (int dx = -clearRadius; dx <= clearRadius; dx++)
                {
                    for (int dy = -20; dy <= 2; dy++) // From canopy to below base
                    {
                        int checkX = tree.BaseX + dx;
                        int checkY = tree.BaseY + dy;
                        
                        var tile = world.GetTile(checkX, checkY);
                        if (tile != null && tile.Type == TileType.Water)
                        {
                            world.SetTile(checkX, checkY, new StarshroudHollows.World.Tile(TileType.Air));
                            waterRemoved++;
                        }
                    }
                }
            }
            
            Logger.Log($"[WORLDGEN] Removed {waterRemoved} water tiles around trees");
        }

        private void PlaceTree(int baseX, int baseY)
        {
            // CRITICAL: Don't place trees in areas with modified tiles (player buildings)
            int treeRadius = 5; // Check 5 tiles around tree base
            for (int dx = -treeRadius; dx <= treeRadius; dx++)
            {
                for (int dy = -15; dy <= 2; dy++) // Check from trunk to canopy height
                {
                    Point checkPos = new Point(baseX + dx, baseY + dy);
                    if (modifiedTilePositions.Contains(checkPos))
                    {
                        // This area has been modified by the player - don't place tree
                        return;
                    }
                }
            }
            
            // CRITICAL FIX: Detect what biome we're in and set the correct tree type
            TileType treeType = TileType.ForestTree; // Default to forest
            var groundTile = world.GetTile(baseX, baseY);
            
            if (groundTile != null && groundTile.IsActive)
            {
                switch (groundTile.Type)
                {
                    case TileType.SnowGrass:
                        treeType = TileType.SnowTree;
                        break;
                    case TileType.SwampGrass:
                        treeType = TileType.SwampTree;
                        break;
                    case TileType.JungleGrass:
                        treeType = TileType.JungleTree;
                        break;
                    case TileType.VolcanicGrass:
                        treeType = TileType.VolcanicTree;
                        break;
                    default:
                        treeType = TileType.ForestTree; // Default for grass/dirt
                        break;
                }
            }
            
            int trunkHeight = random.Next(8, 15);
            var tree = new StarshroudHollows.World.Tree(baseX, baseY, trunkHeight, treeType); // Use detected tree type!
            for (int y = 0; y < trunkHeight; y++)
            {
                int treeY = baseY - 1 - y;
                // Trees use full sprites now, but we still place tiles for collision
                var newTile = new StarshroudHollows.World.Tile(TileType.Wood, true);
                newTile.IsPartOfTree = true; // Mark as tree so lighting system doesn't add background
                world.SetTile(baseX, treeY, newTile);
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
                            // Trees use full sprites now, but we still place tiles for collision
                            var newTile = new StarshroudHollows.World.Tile(TileType.Leaves, true);
                            newTile.IsPartOfTree = true; // Mark as tree so lighting system doesn't add background
                            world.SetTile(leafX, leafY, newTile);
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }
            world.AddTree(tree);
        }

        private void GenerateGrass()
        {
            for (int x = 0; x < StarshroudHollows.World.World.WORLD_WIDTH; x++)
            {
                int surfaceY = world.GetSurfaceHeight(x);
                var surfaceTile = world.GetTile(x, surfaceY);
                if (surfaceTile != null && surfaceTile.Type == TileType.Dirt)
                {
                    var aboveTile = world.GetTile(x, surfaceY - 1);
                    if (aboveTile == null || !aboveTile.IsActive)
                        world.SetTile(x, surfaceY, new StarshroudHollows.World.Tile(TileType.Grass));
                }
            }
        }

        private void GenerateSnowBiome()
        {
            const int WORLD_CENTER = StarshroudHollows.World.World.WORLD_WIDTH / 2; // 3250
            const int MIN_DISTANCE_FROM_CENTER = 250;
            const int MAX_DISTANCE_FROM_CENTER = 450;

            // Randomly choose left or right side
            bool spawnLeft = random.Next(0, 2) == 0;

            // Calculate distance from center (250-450 blocks)
            int distanceFromCenter = random.Next(MIN_DISTANCE_FROM_CENTER, MAX_DISTANCE_FROM_CENTER + 1);

            // Calculate width of snow biome (250-450 blocks)
            int biomeWidth = random.Next(250, 451);

            if (spawnLeft)
            {
                // Left side: center - distance - width to center - distance
                snowBiomeEndX = WORLD_CENTER - distanceFromCenter;
                snowBiomeStartX = snowBiomeEndX - biomeWidth;
            }
            else
            {
                // Right side: center + distance to center + distance + width
                snowBiomeStartX = WORLD_CENTER + distanceFromCenter;
                snowBiomeEndX = snowBiomeStartX + biomeWidth;
            }

            // Ensure boundaries stay within world
            snowBiomeStartX = Math.Max(0, snowBiomeStartX);
            snowBiomeEndX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, snowBiomeEndX);

            Logger.Log($"[WORLDGEN] Generating snow biome from X={snowBiomeStartX} to X={snowBiomeEndX} (width: {snowBiomeEndX - snowBiomeStartX}, side: {(spawnLeft ? "LEFT" : "RIGHT")})");
            Logger.Log($"[WORLDGEN] Snow biome is {Math.Abs((snowBiomeStartX + snowBiomeEndX) / 2 - WORLD_CENTER)} blocks from spawn (world center)");

            int snowTilesConverted = 0;
            int iciclesPlaced = 0;
            int treesConverted = 0;

            // FIRST: Convert existing trees to snow trees - this changes which sprite is used
            var treesList = new System.Collections.Generic.List<StarshroudHollows.World.Tree>(world.trees);
            foreach (var tree in treesList)
            {
                // Check if tree is in snow biome
                if (tree.BaseX >= snowBiomeStartX && tree.BaseX <= snowBiomeEndX)
                {
                    // Change tree type to snow tree - this will use the snowtree.png sprite
                    tree.TreeType = TileType.SnowTree;
                    treesConverted++;
                }
            }

            // Apply snow transformation
            for (int x = snowBiomeStartX; x <= snowBiomeEndX; x++)
            {
                for (int y = 0; y < SNOW_BIOME_DEPTH; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile != null && tile.IsActive)
                    {
                        // Convert tiles to snow variants
                        switch (tile.Type)
                        {
                            case TileType.Grass:
                                world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.SnowGrass));
                                snowTilesConverted++;
                                break;
                            case TileType.Dirt:
                                world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Snow));
                                snowTilesConverted++;
                                break;
                            // DON'T convert leaves/wood - trees now use full sprites and the tree tiles are marked with IsPartOfTree
                            // case TileType.Leaves:
                            //     world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.SnowyLeaves));
                            //     snowTilesConverted++;
                            //     break;
                            case TileType.Water:
                                // Freeze top layer of water to ice
                                var tileAbove = world.GetTile(x, y - 1);
                                // If air above, this is the top layer - freeze it
                                if (tileAbove == null || !tileAbove.IsActive)
                                {
                                    world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Ice));
                                    snowTilesConverted++;
                                }
                                break;
                        }

                        // Place icicles on cave ceilings (10% chance)
                        if (tile.Type == TileType.Stone && random.Next(0, 100) < 10)
                        {
                            // Check if there's air below
                            var tileBelow = world.GetTile(x, y + 1);
                            if (tileBelow != null && !tileBelow.IsActive)
                            {
                                // Check if there's 2-3 blocks of space below for icicle
                                int icicleLength = random.Next(2, 4);
                                bool hasSpace = true;
                                for (int i = 1; i <= icicleLength; i++)
                                {
                                    var checkTile = world.GetTile(x, y + i);
                                    if (checkTile == null || checkTile.IsActive)
                                    {
                                        hasSpace = false;
                                        break;
                                    }
                                }

                                if (hasSpace)
                                {
                                    // Place icicle
                                    world.SetTile(x, y + 1, new StarshroudHollows.World.Tile(TileType.Icicle));
                                    iciclesPlaced++;
                                }
                            }
                        }
                    }
                }
            }

            Logger.Log($"[WORLDGEN] Snow biome complete: {snowTilesConverted} tiles converted, {iciclesPlaced} icicles placed, {treesConverted} trees converted to snow trees");
        }

        private void GenerateSwampBiome()
        {
            const int WORLD_CENTER = StarshroudHollows.World.World.WORLD_WIDTH / 2;
            const int MIN_DISTANCE_FROM_CENTER = 300;
            const int MAX_DISTANCE_FROM_CENTER = 500;

            // Choose opposite side from snow biome
            bool spawnLeft = (snowBiomeStartX + snowBiomeEndX) / 2 > WORLD_CENTER;

            // Calculate distance from center (300-500 blocks) - farther than snow
            int distanceFromCenter = random.Next(MIN_DISTANCE_FROM_CENTER, MAX_DISTANCE_FROM_CENTER + 1);

            // Calculate width of swamp biome (300-500 blocks)
            int biomeWidth = random.Next(300, 501);

            if (spawnLeft)
            {
                // Left side
                swampBiomeEndX = WORLD_CENTER - distanceFromCenter;
                swampBiomeStartX = swampBiomeEndX - biomeWidth;
            }
            else
            {
                // Right side
                swampBiomeStartX = WORLD_CENTER + distanceFromCenter;
                swampBiomeEndX = swampBiomeStartX + biomeWidth;
            }

            // Ensure boundaries stay within world
            swampBiomeStartX = Math.Max(0, swampBiomeStartX);
            swampBiomeEndX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, swampBiomeEndX);

            Logger.Log($"[WORLDGEN] Generating swamp biome from X={swampBiomeStartX} to X={swampBiomeEndX} (width: {swampBiomeEndX - swampBiomeStartX}, side: {(spawnLeft ? "LEFT" : "RIGHT")}");
            Logger.Log($"[WORLDGEN] Swamp biome is {Math.Abs((swampBiomeStartX + swampBiomeEndX) / 2 - WORLD_CENTER)} blocks from spawn (world center)");

            int swampTilesConverted = 0;
            int swampTreesPlaced = 0;
            int extraWaterPoolsPlaced = 0;

            // Apply swamp transformation
            for (int x = swampBiomeStartX; x <= swampBiomeEndX; x++)
            {
                for (int y = 0; y < SWAMP_BIOME_DEPTH; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile != null && tile.IsActive)
                    {
                        // Convert tiles to swamp variants
                        switch (tile.Type)
                        {
                            case TileType.Grass:
                                world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.SwampGrass));
                                swampTilesConverted++;
                                break;
                        }
                    }
                }
            }

            // Place swamp trees (replace existing trees or place new ones) - every 6-12 blocks for more density
            for (int x = swampBiomeStartX; x <= swampBiomeEndX; x += random.Next(6, 13))
            {
                int surfaceY = world.GetSurfaceHeight(x);
                var groundTile = world.GetTile(x, surfaceY);
                
                if (groundTile != null && groundTile.Type == TileType.SwampGrass)
                {
                    // Remove any existing tree at this location
                    for (int checkY = surfaceY - 20; checkY < surfaceY; checkY++)
                    {
                        var checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsPartOfTree)
                        {
                            world.RemoveTree(x, checkY);
                            break; // Only need to remove once
                        }
                    }

                    // Place swamp tree
                    bool hasSpace = true;
                    for (int checkY = surfaceY - 1; checkY > surfaceY - 15; checkY--)
                    {
                        var checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsActive) { hasSpace = false; break; }
                    }

                    if (hasSpace)
                    {
                        PlaceSwampTree(x, surfaceY);
                        swampTreesPlaced++;
                    }
                }
            }

            // Add extra water pools in swamp biome (carved into ground, not floating)
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int x = random.Next(swampBiomeStartX, swampBiomeEndX + 1);
                int surfaceY = world.GetSurfaceHeight(x);
                
                // Create surface water pool carved into ground (like surface water pools)
                if (CreateSurfaceSwampPool(x, surfaceY))
                {
                    extraWaterPoolsPlaced++;
                }
            }

            // Color all water in swamp biome to murky green
            int waterColored = 0;
            for (int x = swampBiomeStartX; x <= swampBiomeEndX; x++)
            {
                for (int y = 0; y < SWAMP_BIOME_DEPTH; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile != null && tile.Type == TileType.Water)
                    {
                        // Set swamp water color (murky green)
                        tile.WaterColor = new Color(80, 100, 60); // Dark murky green
                        waterColored++;
                    }
                }
            }

            Logger.Log($"[WORLDGEN] Swamp biome complete: {swampTilesConverted} tiles converted, {swampTreesPlaced} swamp trees placed, {extraWaterPoolsPlaced} extra water pools, {waterColored} water tiles colored");
        }

        private void GenerateJungleBiome()
        {
            const int WORLD_CENTER = StarshroudHollows.World.World.WORLD_WIDTH / 2;
            const int MIN_DISTANCE_FROM_CENTER = 550;
            const int MAX_DISTANCE_FROM_CENTER = 750;

            // Choose opposite side from swamp biome
            bool spawnLeft = (swampBiomeStartX + swampBiomeEndX) / 2 > WORLD_CENTER;

            // Calculate distance from center (550-750 blocks) - farther than swamp
            int distanceFromCenter = random.Next(MIN_DISTANCE_FROM_CENTER, MAX_DISTANCE_FROM_CENTER + 1);

            // Calculate width of jungle biome (400-600 blocks)
            int biomeWidth = random.Next(400, 601);

            if (spawnLeft)
            {
                // Left side
                jungleBiomeEndX = WORLD_CENTER - distanceFromCenter;
                jungleBiomeStartX = jungleBiomeEndX - biomeWidth;
            }
            else
            {
                // Right side
                jungleBiomeStartX = WORLD_CENTER + distanceFromCenter;
                jungleBiomeEndX = jungleBiomeStartX + biomeWidth;
            }

            // Ensure boundaries stay within world
            jungleBiomeStartX = Math.Max(0, jungleBiomeStartX);
            jungleBiomeEndX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, jungleBiomeEndX);

            Logger.Log($"[WORLDGEN] Generating jungle biome from X={jungleBiomeStartX} to X={jungleBiomeEndX} (width: {jungleBiomeEndX - jungleBiomeStartX}, side: {(spawnLeft ? "LEFT" : "RIGHT")}");
            Logger.Log($"[WORLDGEN] Jungle biome is {Math.Abs((jungleBiomeStartX + jungleBiomeEndX) / 2 - WORLD_CENTER)} blocks from spawn (world center)");

            int jungleTilesConverted = 0;
            int jungleTreesPlaced = 0;

            // Apply jungle transformation - convert grass to jungle grass
            for (int x = jungleBiomeStartX; x <= jungleBiomeEndX; x++)
            {
                for (int y = 0; y < JUNGLE_BIOME_DEPTH; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile != null && tile.IsActive)
                    {
                        // Convert grass tiles
                        if (tile.Type == TileType.Grass)
                        {
                            world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.JungleGrass));
                            jungleTilesConverted++;
                        }
                    }
                }
            }

            // Place LOTS of jungle trees (very dense forest) - every 3-6 blocks
            for (int x = jungleBiomeStartX; x <= jungleBiomeEndX; x += random.Next(3, 7))
            {
                int surfaceY = world.GetSurfaceHeight(x);
                var groundTile = world.GetTile(x, surfaceY);
                
                if (groundTile != null && groundTile.Type == TileType.JungleGrass)
                {
                    // Remove any existing tree at this location
                    for (int checkY = surfaceY - 20; checkY < surfaceY; checkY++)
                    {
                        var checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsPartOfTree)
                        {
                            world.RemoveTree(x, checkY);
                            break; // Only need to remove once
                        }
                    }

                    // Place jungle tree
                    bool hasSpace = true;
                    for (int checkY = surfaceY - 1; checkY > surfaceY - 18; checkY--)
                    {
                        var checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsActive) { hasSpace = false; break; }
                    }

                    if (hasSpace)
                    {
                        PlaceJungleTree(x, surfaceY);
                        jungleTreesPlaced++;
                    }
                }
            }

            Logger.Log($"[WORLDGEN] Jungle biome complete: {jungleTilesConverted} tiles converted, {jungleTreesPlaced} jungle trees placed");
        }

        private void GenerateVolcanicBiome()
        {
            const int WORLD_CENTER = StarshroudHollows.World.World.WORLD_WIDTH / 2;
            const int MIN_DISTANCE_FROM_CENTER = 850;
            const int MAX_DISTANCE_FROM_CENTER = 1050;

            // Choose opposite side from jungle biome
            bool spawnLeft = (jungleBiomeStartX + jungleBiomeEndX) / 2 > WORLD_CENTER;

            // Calculate distance from center (850-1050 blocks) - farther than jungle
            int distanceFromCenter = random.Next(MIN_DISTANCE_FROM_CENTER, MAX_DISTANCE_FROM_CENTER + 1);

            // Calculate width of volcanic biome (500-700 blocks)
            int biomeWidth = random.Next(500, 701);

            if (spawnLeft)
            {
                // Left side
                volcanicBiomeEndX = WORLD_CENTER - distanceFromCenter;
                volcanicBiomeStartX = volcanicBiomeEndX - biomeWidth;
            }
            else
            {
                // Right side
                volcanicBiomeStartX = WORLD_CENTER + distanceFromCenter;
                volcanicBiomeEndX = volcanicBiomeStartX + biomeWidth;
            }

            // Ensure boundaries stay within world
            volcanicBiomeStartX = Math.Max(0, volcanicBiomeStartX);
            volcanicBiomeEndX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, volcanicBiomeEndX);

            Logger.Log($"[WORLDGEN] Generating volcanic biome from X={volcanicBiomeStartX} to X={volcanicBiomeEndX} (width: {volcanicBiomeEndX - volcanicBiomeStartX}, side: {(spawnLeft ? "LEFT" : "RIGHT")}");
            Logger.Log($"[WORLDGEN] Volcanic biome is {Math.Abs((volcanicBiomeStartX + volcanicBiomeEndX) / 2 - WORLD_CENTER)} blocks from spawn (world center)");

            int volcanicTilesConverted = 0;
            int waterToLavaConverted = 0;
            int volcanicTreesPlaced = 0;
            int lavaPoolsPlaced = 0;

            // Apply volcanic transformation
            for (int x = volcanicBiomeStartX; x <= volcanicBiomeEndX; x++)
            {
                for (int y = 0; y < VOLCANIC_BIOME_DEPTH; y++)
                {
                    var tile = world.GetTile(x, y);
                    if (tile != null && tile.IsActive)
                    {
                        // Convert grass to volcanic grass (ash-covered)
                        if (tile.Type == TileType.Grass || tile.Type == TileType.JungleGrass)
                        {
                            world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.VolcanicGrass));
                            volcanicTilesConverted++;
                        }
                        // Convert ALL water to lava in volcanic biome
                        else if (tile.Type == TileType.Water)
                        {
                            world.SetTile(x, y, new StarshroudHollows.World.Tile(TileType.Lava));
                            waterToLavaConverted++;
                        }
                    }
                }
            }

            // Place sparse volcanic trees (way less than other biomes) - every 25-40 blocks
            for (int x = volcanicBiomeStartX; x <= volcanicBiomeEndX; x += random.Next(25, 41))
            {
                int surfaceY = world.GetSurfaceHeight(x);
                var groundTile = world.GetTile(x, surfaceY);
                
                if (groundTile != null && groundTile.Type == TileType.VolcanicGrass)
                {
                    // Remove any existing tree at this location
                    for (int checkY = surfaceY - 20; checkY < surfaceY; checkY++)
                    {
                        var checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsPartOfTree)
                        {
                            world.RemoveTree(x, checkY);
                            break; // Only need to remove once
                        }
                    }

                    // Place volcanic tree (small chance)
                    if (random.Next(0, 100) < 40) // Only 40% chance to place tree
                    {
                        bool hasSpace = true;
                        for (int checkY = surfaceY - 1; checkY > surfaceY - 12; checkY--)
                        {
                            var checkTile = world.GetTile(x, checkY);
                            if (checkTile != null && checkTile.IsActive) { hasSpace = false; break; }
                        }

                        if (hasSpace)
                        {
                            PlaceVolcanicTree(x, surfaceY);
                            volcanicTreesPlaced++;
                        }
                    }
                }
            }

            // Add LOTS of surface lava pools (150 pools)
            for (int attempt = 0; attempt < 150; attempt++)
            {
                int x = random.Next(volcanicBiomeStartX, volcanicBiomeEndX + 1);
                int surfaceY = world.GetSurfaceHeight(x);
                
                // Create lava pool on surface
                if (CreateSurfaceLavaPool(x, surfaceY))
                {
                    lavaPoolsPlaced++;
                }
            }

            // Add underground lava pockets
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = random.Next(volcanicBiomeStartX, volcanicBiomeEndX + 1);
                int y = random.Next(SURFACE_LEVEL, SURFACE_LEVEL + 300);
                
                if (CreateSmallLavaPool(x, y))
                {
                    lavaPoolsPlaced++;
                }
            }

            Logger.Log($"[WORLDGEN] Volcanic biome complete: {volcanicTilesConverted} tiles converted, {waterToLavaConverted} water→lava, {volcanicTreesPlaced} volcanic trees placed, {lavaPoolsPlaced} lava pools");
        }

        private void PlaceVolcanicTree(int baseX, int baseY)
        {
            // Check for player-modified tiles
            int treeRadius = 4;
            for (int dx = -treeRadius; dx <= treeRadius; dx++)
            {
                for (int dy = -12; dy <= 2; dy++)
                {
                    Point checkPos = new Point(baseX + dx, baseY + dy);
                    if (modifiedTilePositions.Contains(checkPos))
                    {
                        return;
                    }
                }
            }

            int trunkHeight = random.Next(6, 11); // Short, burnt-looking trees
            var tree = new StarshroudHollows.World.Tree(baseX, baseY, trunkHeight, TileType.VolcanicTree);
            
            // Place volcanic tree trunk
            for (int y = 0; y < trunkHeight; y++)
            {
                int treeY = baseY - 1 - y;
                var newTile = new StarshroudHollows.World.Tile(TileType.VolcanicTree, true);
                newTile.IsPartOfTree = true;
                world.SetTile(baseX, treeY, newTile);
                tree.AddTile(baseX, treeY);
            }
            
            // Volcanic trees have small, sparse canopies
            int canopyY = baseY - trunkHeight;
            int canopyRadius = random.Next(2, 3); // Small canopy
            for (int dx = -canopyRadius; dx <= canopyRadius; dx++)
            {
                for (int dy = -canopyRadius; dy <= canopyRadius; dy++)
                {
                    // Sparse, burnt leaves
                    if (dx * dx + dy * dy <= canopyRadius * canopyRadius && random.Next(0, 100) < 60)
                    {
                        int leafX = baseX + dx;
                        int leafY = canopyY + dy;
                        var existingTile = world.GetTile(leafX, leafY);
                        if (existingTile == null || !existingTile.IsActive || existingTile.Type != TileType.VolcanicTree)
                        {
                            var newTile = new StarshroudHollows.World.Tile(TileType.Leaves, true);
                            newTile.IsPartOfTree = true;
                            world.SetTile(leafX, leafY, newTile);
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }
            world.AddTree(tree);
        }

        // REPLACED: This version builds container on sides and bottom, leaving the top open
        private bool CreateSurfaceLavaPool(int centerX, int surfaceY)
        {
            int poolWidth = random.Next(3, 7);
            int poolDepth = random.Next(1, 3); // Shallow pools
            bool lavaPlaced = false;

            // 1. Build stone container walls (Sides and Bottom ONLY)
            // This loop starts at dy = 1 (below the surface)
            for (int dx = -poolWidth / 2 - 1; dx <= poolWidth / 2 + 1; dx++)
            {
                for (int dy = 1; dy <= poolDepth + 1; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    // Build walls on sides (dx == ...) and bottom (dy == poolDepth + 1)
                    if (dx == -poolWidth / 2 - 1 || dx == poolWidth / 2 + 1 || dy == poolDepth + 1)
                    {
                        // Only build walls if it's air (don't overwrite existing ground)
                        var tile = world.GetTile(poolX, poolY);
                        if (tile == null || !tile.IsActive)
                        {
                            world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Stone));
                        }
                    }
                }
            }

            // 2. Clear interior (including the surface level)
            // This loop starts at dy = 0 (at the surface)
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    // Clear any grass/dirt that might be here
                    var tile = world.GetTile(poolX, poolY);
                    if (tile == null || tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Air));
                    }
                }
            }

            // 3. Fill with lava (including the surface level)
            // This loop starts at dy = 0 (at the surface)
            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = 0; dy <= poolDepth; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = surfaceY + dy;

                    var lavaTile = new StarshroudHollows.World.Tile(TileType.Lava);
                    lavaTile.LiquidVolume = 1.0f; // Full volume
                    world.SetTile(poolX, poolY, lavaTile);
                    lavaPlaced = true;
                }
            }

            return lavaPlaced;
        }

        private bool CreateSmallLavaPool(int centerX, int centerY)
        {
            // Find cave floor
            int floorY = -1;
            bool foundCave = false;

            for (int checkY = centerY; checkY < Math.Min(centerY + 40, StarshroudHollows.World.World.WORLD_HEIGHT - 1); checkY++)
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

            int poolWidth = random.Next(3, 6);
            int poolDepth = random.Next(2, 3);
            bool lavaPlaced = false;

            for (int dx = -poolWidth / 2; dx <= poolWidth / 2; dx++)
            {
                for (int dy = -poolDepth; dy < 0; dy++)
                {
                    int poolX = centerX + dx;
                    int poolY = floorY + dy;

                    var tile = world.GetTile(poolX, poolY);
                    if (tile == null || !tile.IsActive)
                    {
                        world.SetTile(poolX, poolY, new StarshroudHollows.World.Tile(TileType.Lava));
                        lavaPlaced = true;
                    }
                }
            }

            return lavaPlaced;
        }

        private void PlaceJungleTree(int baseX, int baseY)
        {
            // Check for player-modified tiles
            int treeRadius = 6;
            for (int dx = -treeRadius; dx <= treeRadius; dx++)
            {
                for (int dy = -18; dy <= 2; dy++)
                {
                    Point checkPos = new Point(baseX + dx, baseY + dy);
                    if (modifiedTilePositions.Contains(checkPos))
                    {
                        return;
                    }
                }
            }

            int trunkHeight = random.Next(12, 18); // Very tall trees (12-18 blocks)
            var tree = new StarshroudHollows.World.Tree(baseX, baseY, trunkHeight, TileType.JungleTree);
            
            // Place jungle tree trunk
            for (int y = 0; y < trunkHeight; y++)
            {
                int treeY = baseY - 1 - y;
                var newTile = new StarshroudHollows.World.Tile(TileType.JungleTree, true);
                newTile.IsPartOfTree = true;
                world.SetTile(baseX, treeY, newTile);
                tree.AddTile(baseX, treeY);
            }
            
            // Jungle trees have very large, bushy canopies
            int canopyY = baseY - trunkHeight;
            int canopyRadius = random.Next(4, 6); // Bigger than normal trees
            for (int dx = -canopyRadius; dx <= canopyRadius; dx++)
            {
                for (int dy = -canopyRadius; dy <= canopyRadius; dy++)
                {
                    // Create a bushy, irregular canopy
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= canopyRadius + random.Next(-1, 2))
                    {
                        int leafX = baseX + dx;
                        int leafY = canopyY + dy;
                        var existingTile = world.GetTile(leafX, leafY);
                        if (existingTile == null || !existingTile.IsActive || existingTile.Type != TileType.JungleTree)
                        {
                            var newTile = new StarshroudHollows.World.Tile(TileType.Leaves, true);
                            newTile.IsPartOfTree = true;
                            world.SetTile(leafX, leafY, newTile);
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }
            world.AddTree(tree);
        }

        private void PlaceSwampTree(int baseX, int baseY)
        {
            // Check for player-modified tiles
            int treeRadius = 5;
            for (int dx = -treeRadius; dx <= treeRadius; dx++)
            {
                for (int dy = -15; dy <= 2; dy++)
                {
                    Point checkPos = new Point(baseX + dx, baseY + dy);
                    if (modifiedTilePositions.Contains(checkPos))
                    {
                        return;
                    }
                }
            }

            int trunkHeight = random.Next(10, 16); // Taller than normal trees
            var tree = new StarshroudHollows.World.Tree(baseX, baseY, trunkHeight, TileType.SwampTree);
            
            // Place swamp tree trunk
            for (int y = 0; y < trunkHeight; y++)
            {
                int treeY = baseY - 1 - y;
                var newTile = new StarshroudHollows.World.Tile(TileType.SwampTree, true);
                newTile.IsPartOfTree = true;
                world.SetTile(baseX, treeY, newTile);
                tree.AddTile(baseX, treeY);
            }
            
            // Swamp trees have drooping leaves
            int canopyY = baseY - trunkHeight;
            int canopyRadius = random.Next(3, 5);
            for (int dx = -canopyRadius; dx <= canopyRadius; dx++)
            {
                for (int dy = -canopyRadius; dy <= canopyRadius + 2; dy++) // Droops down more
                {
                    if (dx * dx + dy * dy <= canopyRadius * canopyRadius + 3)
                    {
                        int leafX = baseX + dx;
                        int leafY = canopyY + dy;
                        var existingTile = world.GetTile(leafX, leafY);
                        if (existingTile == null || !existingTile.IsActive || existingTile.Type != TileType.SwampTree)
                        {
                            var newTile = new StarshroudHollows.World.Tile(TileType.Leaves, true);
                            newTile.IsPartOfTree = true;
                            world.SetTile(leafX, leafY, newTile);
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }
            world.AddTree(tree);
        }

        // CRITICAL FIX: Remove liquids that are floating in the air (no solid block below)
        private void RemoveFloatingLiquids()
        {
            int floatingLiquidsRemoved = 0;
            
            // Scan the entire world for floating liquids
            for (int x = 0; x < StarshroudHollows.World.World.WORLD_WIDTH; x++)
            {
                for (int y = 0; y < StarshroudHollows.World.World.WORLD_HEIGHT - 1; y++)
                {
                    var tile = world.GetTile(x, y);
                    
                    // Check if this is a liquid tile
                    if (tile != null && (tile.Type == TileType.Water || tile.Type == TileType.Lava) && tile.LiquidVolume > 0)
                    {
                        // Check if there's any solid ground within 10 blocks below
                        bool hasGroundBelow = false;
                        for (int checkY = y + 1; checkY <= Math.Min(y + 10, StarshroudHollows.World.World.WORLD_HEIGHT - 1); checkY++)
                        {
                            var checkTile = world.GetTile(x, checkY);
                            if (checkTile != null && checkTile.IsActive && 
                                checkTile.Type != TileType.Water && checkTile.Type != TileType.Lava)
                            {
                                hasGroundBelow = true;
                                break;
                            }
                        }
                        
                        // If no ground below within 10 blocks, remove the liquid
                        if (!hasGroundBelow)
                        {
                            tile.Type = TileType.Air;
                            tile.LiquidVolume = 0f;
                            floatingLiquidsRemoved++;
                        }
                    }
                }
            }
            
            Logger.Log($"[WORLDGEN] Removed {floatingLiquidsRemoved} floating liquid tiles");
        }

        // NEW: Generate small spider nest biomes underground
        private void GenerateSpiderBiomes()
        {
            int spiderBiomesPlaced = 0;
            int spiderWebsPlaced = 0;
            
            // Generate 5-10 small spider nests in forest biome underground (850+ blocks deep)
            int numBiomes = random.Next(5, 11);
            
            for (int i = 0; i < numBiomes; i++)
            {
                // Find location in forest biome (center region), deep underground
                int centerX = StarshroudHollows.World.World.WORLD_WIDTH / 2;
                int x = random.Next(centerX - 600, centerX + 600); // Within 600 blocks of center
                int y = random.Next(850, 1200); // 850-1200 blocks deep
                
                // Check if this area is relatively open (cave)
                var tile = world.GetTile(x, y);
                if (tile == null || !tile.IsActive)
                {
                    // Found a cave - create spider nest here
                    int nestRadius = random.Next(8, 15); // 8-15 block radius
                    int webCount = 0;
                    
                    for (int dx = -nestRadius; dx <= nestRadius; dx++)
                    {
                        for (int dy = -nestRadius; dy <= nestRadius; dy++)
                        {
                            // Place webs in a roughly circular pattern
                            if (dx * dx + dy * dy <= nestRadius * nestRadius)
                            {
                                int webX = x + dx;
                                int webY = y + dy;
                                
                                var webTile = world.GetTile(webX, webY);
                                // Place web if empty air, randomly (60% chance)
                                if ((webTile == null || !webTile.IsActive) && random.Next(0, 100) < 60)
                                {
                                    world.SetTile(webX, webY, new StarshroudHollows.World.Tile(TileType.SpiderWeb));
                                    webCount++;
                                }
                            }
                        }
                    }
                    
                    if (webCount > 0)
                    {
                        spiderBiomesPlaced++;
                        spiderWebsPlaced += webCount;
                    }
                }
            }
            
            Logger.Log($"[WORLDGEN] Generated {spiderBiomesPlaced} spider nests with {spiderWebsPlaced} spider webs");
        }

        // NEW: Generate small worm colony biomes underground
        private void GenerateWormBiomes()
        {
            int wormBiomesPlaced = 0;
            int burrowsCarved = 0;
            
            // Generate 3-7 worm colonies in forest biome underground (850+ blocks deep)
            int numBiomes = random.Next(3, 8);
            
            for (int i = 0; i < numBiomes; i++)
            {
                // Find location in forest biome (center region), deep underground
                int centerX = StarshroudHollows.World.World.WORLD_WIDTH / 2;
                int x = random.Next(centerX - 600, centerX + 600); // Within 600 blocks of center
                int y = random.Next(850, 1200); // 850-1200 blocks deep
                
                // Carve out worm burrows (worms tunnel through blocks)
                int colonyRadius = random.Next(12, 20); // 12-20 block radius
                int burrowCount = 0;
                
                // Create several worm tunnels radiating from center
                int numTunnels = random.Next(4, 8);
                for (int t = 0; t < numTunnels; t++)
                {
                    double angle = (Math.PI * 2 * t) / numTunnels + (random.NextDouble() - 0.5) * 0.5;
                    int tunnelLength = random.Next(15, 30);
                    
                    int currentX = x;
                    int currentY = y;
                    
                    for (int step = 0; step < tunnelLength; step++)
                    {
                        // Carve tunnel (6 blocks wide for worm)
                        int tunnelWidth = 6;
                        for (int dx = -tunnelWidth / 2; dx <= tunnelWidth / 2; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int tunnelX = currentX + dx;
                                int tunnelY = currentY + dy;
                                
                                var tunnelTile = world.GetTile(tunnelX, tunnelY);
                                if (tunnelTile != null && tunnelTile.IsActive && tunnelTile.Type == TileType.Stone)
                                {
                                    world.SetTile(tunnelX, tunnelY, new StarshroudHollows.World.Tile(TileType.Air));
                                    burrowCount++;
                                }
                            }
                        }
                        
                        // Move tunnel forward
                        angle += (random.NextDouble() - 0.5) * 0.3;
                        currentX += (int)(Math.Cos(angle) * 2);
                        currentY += (int)(Math.Sin(angle) * 2);
                        
                        // Stop if out of bounds
                        if (currentX < 50 || currentX >= StarshroudHollows.World.World.WORLD_WIDTH - 50 ||
                            currentY < 850 || currentY >= 1200)
                        {
                            break;
                        }
                    }
                }
                
                if (burrowCount > 0)
                {
                    wormBiomesPlaced++;
                    burrowsCarved += burrowCount;
                }
            }
            
            Logger.Log($"[WORLDGEN] Generated {wormBiomesPlaced} worm colonies with {burrowsCarved} burrow blocks carved");
        }

        public Vector2 GetSpawnPosition(int playerPixelHeight)
        {
            int spawnX = StarshroudHollows.World.World.WORLD_WIDTH / 2;
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

            StarshroudHollows.World.Tile groundTile = world.GetTile(spawnX, surfaceY);
            if (groundTile == null || !groundTile.IsActive)
            {
                for (int searchRadius = 1; searchRadius <= 50; searchRadius++)
                {
                    for (int dx = -searchRadius; dx <= searchRadius; dx++)
                    {
                        int checkX = spawnX + dx;
                        int checkY = world.GetSurfaceHeight(checkX);
                        StarshroudHollows.World.Tile checkGround = world.GetTile(checkX, checkY);
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

            int surfacePixelY = surfaceY * StarshroudHollows.World.World.TILE_SIZE;
            int spawnPixelY = surfacePixelY - playerPixelHeight - 2;
            int spawnPixelX = spawnX * StarshroudHollows.World.World.TILE_SIZE;
            return new Vector2(spawnPixelX, spawnPixelY);
        }
    }
}
