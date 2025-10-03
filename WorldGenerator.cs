using System;
using Claude4_5Terraria.Enums;
using Microsoft.Xna.Framework;

namespace Claude4_5Terraria.World
{
    public class WorldGenerator
    {
        private Random random;
        private World world;
        private int seed;

        private const int SURFACE_LEVEL = 100;
        private const int DIRT_LAYER_THICKNESS = 10;
        private const int CAVE_START_DEPTH = 150;

        public WorldGenerator(World world, int seed = 0)
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
                GenerateTerrain();
                GenerateCaves();
                GenerateDirtPockets();
                GenerateOres();
                GenerateTrees();
                GenerateGrass();

                TimeSpan duration = DateTime.Now - startTime;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void GenerateTerrain()
        {
            int centerX = World.WORLD_WIDTH / 2;
            int flatZoneRadius = 25;
            int transitionZone = 25;

            for (int x = 0; x < World.WORLD_WIDTH; x++)
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

                for (int y = 0; y < World.WORLD_HEIGHT; y++)
                {
                    if (y < surfaceHeight)
                    {
                        continue;
                    }
                    else if (y < surfaceHeight + DIRT_LAYER_THICKNESS)
                    {
                        world.SetTile(x, y, new Tile(TileType.Dirt));
                    }
                    else
                    {
                        world.SetTile(x, y, new Tile(TileType.Stone));
                    }
                }
            }
        }

        private void GenerateCaves()
        {
            int largeCaves = 40;
            int mediumCaves = 60;
            int smallCaves = 80;

            for (int i = 0; i < largeCaves; i++)
            {
                int startX = random.Next(0, World.WORLD_WIDTH);
                int startY = random.Next(CAVE_START_DEPTH + 100, World.WORLD_HEIGHT - 200);
                CarveCaveTunnel(startX, startY, random.Next(150, 300), 3, 7);
            }

            for (int i = 0; i < mediumCaves; i++)
            {
                int startX = random.Next(50, World.WORLD_WIDTH - 50);
                int startY = random.Next(CAVE_START_DEPTH, World.WORLD_HEIGHT - 150);
                CarveCaveTunnel(startX, startY, random.Next(80, 150), 2, 5);
            }

            for (int i = 0; i < smallCaves; i++)
            {
                int startX = random.Next(0, World.WORLD_WIDTH);
                int startY = random.Next(CAVE_START_DEPTH, World.WORLD_HEIGHT - 100);
                CarveCaveTunnel(startX, startY, random.Next(30, 60), 2, 4);
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

                            Tile tile = world.GetTile(tileX, tileY);
                            if (tile != null && tile.IsActive)
                            {
                                world.SetTile(tileX, tileY, new Tile(TileType.Air));
                            }
                        }
                    }
                }

                direction += (random.NextDouble() - 0.5) * 0.7;
                int moveSpeed = random.Next(1, 3);
                currentX += (int)(Math.Cos(direction) * moveSpeed);
                currentY += (int)(Math.Sin(direction) * moveSpeed);

                if (random.NextDouble() < 0.2)
                {
                    currentY += 1;
                }

                if (currentX < 10 || currentX >= World.WORLD_WIDTH - 10 ||
                    currentY < CAVE_START_DEPTH || currentY >= World.WORLD_HEIGHT - 10)
                {
                    break;
                }

                if (step > 40 && random.NextDouble() < 0.02)
                {
                    break;
                }
            }
        }

        private void GenerateDirtPockets()
        {
            int pocketCount = 400;

            for (int i = 0; i < pocketCount; i++)
            {
                int centerX = random.Next(0, World.WORLD_WIDTH);
                int centerY = random.Next(CAVE_START_DEPTH, World.WORLD_HEIGHT - 50);
                int pocketSize = random.Next(6, 15);

                for (int j = 0; j < pocketSize; j++)
                {
                    int offsetX = random.Next(-4, 5);
                    int offsetY = random.Next(-4, 5);
                    int dirtX = centerX + offsetX;
                    int dirtY = centerY + offsetY;

                    Tile tile = world.GetTile(dirtX, dirtY);
                    if (tile != null && tile.Type == TileType.Stone)
                    {
                        world.SetTile(dirtX, dirtY, new Tile(TileType.Dirt));
                    }
                }
            }
        }

        private void GenerateOres()
        {
            // Copper - Common, shallow to mid
            PlaceOreType(TileType.Copper, 1000, 120, 500, 3, 8);

            // Silver - Uncommon, mid to deep
            PlaceOreType(TileType.Silver, 600, 350, 750, 3, 6);

            // Platinum - Rare, deep only
            PlaceOreType(TileType.Platinum, 300, 600, 950, 2, 5);

            // Coal - Common, shallow to deep (NEW)
            PlaceOreType(TileType.Coal, 2000, 105, 995, 4, 10);
        }

        private void PlaceOreType(TileType oreType, int veinCount, int minDepth, int maxDepth, int minVeinSize, int maxVeinSize)
        {
            for (int i = 0; i < veinCount; i++)
            {
                int veinX = random.Next(0, World.WORLD_WIDTH);
                int veinY = random.Next(minDepth, maxDepth);
                int veinSize = random.Next(minVeinSize, maxVeinSize);

                for (int j = 0; j < veinSize; j++)
                {
                    int offsetX = random.Next(-3, 4);
                    int offsetY = random.Next(-3, 4);
                    int oreX = veinX + offsetX;
                    int oreY = veinY + offsetY;

                    Tile tile = world.GetTile(oreX, oreY);
                    if (tile != null && tile.Type == TileType.Stone)
                    {
                        world.SetTile(oreX, oreY, new Tile(oreType));
                    }
                }
            }
        }

        private void GenerateTrees()
        {
            int treeAttempts = 150;
            int treesPlaced = 0;

            for (int i = 0; i < treeAttempts; i++)
            {
                int x = random.Next(10, World.WORLD_WIDTH - 10);
                int surfaceY = world.GetSurfaceHeight(x);

                Tile groundTile = world.GetTile(x, surfaceY);
                if (groundTile != null && groundTile.Type == TileType.Dirt)
                {
                    bool hasSpace = true;
                    for (int checkY = surfaceY - 1; checkY > surfaceY - 15; checkY--)
                    {
                        Tile checkTile = world.GetTile(x, checkY);
                        if (checkTile != null && checkTile.IsActive)
                        {
                            hasSpace = false;
                            break;
                        }
                    }

                    if (hasSpace)
                    {
                        bool tooClose = false;
                        for (int checkX = x - 4; checkX <= x + 4; checkX++)
                        {
                            Tile checkTile = world.GetTile(checkX, surfaceY - 1);
                            if (checkTile != null && checkTile.Type == TileType.Wood)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                        {
                            PlaceTree(x, surfaceY);
                            treesPlaced++;
                        }
                    }
                }
            }

            Console.WriteLine($"Generated {treesPlaced} trees");
        }

        private void PlaceTree(int baseX, int baseY)
        {
            int trunkHeight = random.Next(8, 15);
            Tree tree = new Tree(baseX, baseY, trunkHeight);

            for (int y = 0; y < trunkHeight; y++)
            {
                int treeY = baseY - 1 - y;
                world.SetTile(baseX, treeY, new Tile(TileType.Wood, true));
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

                        Tile existingTile = world.GetTile(leafX, leafY);
                        if (existingTile == null || !existingTile.IsActive || existingTile.Type != TileType.Wood)
                        {
                            world.SetTile(leafX, leafY, new Tile(TileType.Leaves, true));
                            tree.AddTile(leafX, leafY);
                        }
                    }
                }
            }

            world.AddTree(tree);
        }

        private void GenerateGrass()
        {
            for (int x = 0; x < World.WORLD_WIDTH; x++)
            {
                int surfaceY = world.GetSurfaceHeight(x);
                Tile surfaceTile = world.GetTile(x, surfaceY);

                if (surfaceTile != null && surfaceTile.Type == TileType.Dirt)
                {
                    Tile aboveTile = world.GetTile(x, surfaceY - 1);
                    if (aboveTile == null || !aboveTile.IsActive)
                    {
                        world.SetTile(x, surfaceY, new Tile(TileType.Grass));
                    }
                }
            }

            Console.WriteLine("Generated grass layer");
        }
    }
}