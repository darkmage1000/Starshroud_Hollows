using Microsoft.Xna.Framework;
using StarshroudHollows.Enums;
using StarshroudHollows.World;
using System;

namespace StarshroudHollows.Systems
{
    /// <summary>
    /// Generates and manages boss arena dimensions
    /// </summary>
    public class BossArena
    {
        public const int ARENA_WIDTH = 80;  // 80 tiles wide
        public const int ARENA_HEIGHT = 60; // 60 tiles tall
        
        private World.World arenaWorld;
        private BossType bossType;
        private Random random;
        
        public Vector2 PlayerSpawnPoint { get; private set; }
        public Vector2 BossSpawnPoint { get; private set; }
        public Vector2 ExitPortalPoint { get; private set; }

        public BossArena(BossType boss, int seed)
        {
            bossType = boss;
            random = new Random(seed);
            GenerateArena();
        }

        private void GenerateArena()
        {
            Logger.Log($"[ARENA] Generating arena for {bossType}...");
            
            // Set spawn points based on arena type
            switch (bossType)
            {
                case BossType.CaveTroll:
                    GenerateCaveTrollArena();
                    break;
                // Future bosses can have custom arenas
            }
            
            Logger.Log("[ARENA] Arena generation complete!");
        }

        private void GenerateCaveTrollArena()
        {
            // Player spawns on left side
            PlayerSpawnPoint = new Vector2(
                10 * World.World.TILE_SIZE,
                (ARENA_HEIGHT / 2) * World.World.TILE_SIZE
            );
            
            // Boss spawns on right side
            BossSpawnPoint = new Vector2(
                (ARENA_WIDTH - 15) * World.World.TILE_SIZE,
                (ARENA_HEIGHT / 2) * World.World.TILE_SIZE
            );
            
            // Exit portal spawns in center after boss dies
            ExitPortalPoint = new Vector2(
                (ARENA_WIDTH / 2) * World.World.TILE_SIZE,
                (ARENA_HEIGHT / 2 - 3) * World.World.TILE_SIZE
            );
        }

        /// <summary>
        /// Builds the arena structure in a temporary world section
        /// </summary>
        public void BuildArenaStructure(World.World world, Point startOffset)
        {
            int offsetX = startOffset.X;
            int offsetY = startOffset.Y;
            
            Logger.Log($"[ARENA] Building arena structure at offset ({offsetX}, {offsetY})");
            
            // Clear the entire arena area first
            for (int x = 0; x < ARENA_WIDTH; x++)
            {
                for (int y = 0; y < ARENA_HEIGHT; y++)
                {
                    world.SetTile(offsetX + x, offsetY + y, new Tile(TileType.Air));
                }
            }
            
            // Build floor
            for (int x = 0; x < ARENA_WIDTH; x++)
            {
                // Grass on top
                world.SetTile(offsetX + x, offsetY + ARENA_HEIGHT - 5, new Tile(TileType.Grass));
                
                // Dirt below grass (4 layers)
                for (int y = 0; y < 4; y++)
                {
                    world.SetTile(offsetX + x, offsetY + ARENA_HEIGHT - 4 + y, new Tile(TileType.Dirt));
                }
            }
            
            // Build walls (stone)
            for (int y = 0; y < ARENA_HEIGHT - 5; y++)
            {
                // Left wall (3 blocks thick)
                for (int thickness = 0; thickness < 3; thickness++)
                {
                    world.SetTile(offsetX + thickness, offsetY + y, new Tile(TileType.Stone));
                }
                
                // Right wall (3 blocks thick)
                for (int thickness = 0; thickness < 3; thickness++)
                {
                    world.SetTile(offsetX + ARENA_WIDTH - 1 - thickness, offsetY + y, new Tile(TileType.Stone));
                }
            }
            
            // Build ceiling (stone)
            for (int x = 0; x < ARENA_WIDTH; x++)
            {
                // Top ceiling (3 blocks thick)
                for (int thickness = 0; thickness < 3; thickness++)
                {
                    world.SetTile(offsetX + x, offsetY + thickness, new Tile(TileType.Stone));
                }
            }
            
            // Add torches for lighting (every 8 blocks on walls)
            for (int y = 10; y < ARENA_HEIGHT - 10; y += 8)
            {
                // Left wall torches
                world.SetTile(offsetX + 4, offsetY + y, new Tile(TileType.Torch));
                
                // Right wall torches
                world.SetTile(offsetX + ARENA_WIDTH - 5, offsetY + y, new Tile(TileType.Torch));
            }
            
            // Add some decorative pillars for the Cave Troll arena
            if (bossType == BossType.CaveTroll)
            {
                AddStonePillar(world, offsetX + 15, offsetY + ARENA_HEIGHT - 5, 8);
                AddStonePillar(world, offsetX + ARENA_WIDTH - 16, offsetY + ARENA_HEIGHT - 5, 8);
            }
            
            Logger.Log("[ARENA] Arena structure built successfully!");
        }

        private void AddStonePillar(World.World world, int baseX, int baseY, int height)
        {
            for (int y = 0; y < height; y++)
            {
                // 2x2 pillar
                world.SetTile(baseX, baseY - y, new Tile(TileType.Stone));
                world.SetTile(baseX + 1, baseY - y, new Tile(TileType.Stone));
            }
            
            // Add torch on top
            world.SetTile(baseX, baseY - height - 1, new Tile(TileType.Torch));
        }

        public Vector2 GetPlayerSpawnWorldPosition(Point arenaOffset)
        {
            return new Vector2(
                arenaOffset.X * World.World.TILE_SIZE + PlayerSpawnPoint.X,
                arenaOffset.Y * World.World.TILE_SIZE + PlayerSpawnPoint.Y
            );
        }

        public Vector2 GetBossSpawnWorldPosition(Point arenaOffset)
        {
            return new Vector2(
                arenaOffset.X * World.World.TILE_SIZE + BossSpawnPoint.X,
                arenaOffset.Y * World.World.TILE_SIZE + BossSpawnPoint.Y
            );
        }

        public Vector2 GetExitPortalWorldPosition(Point arenaOffset)
        {
            return new Vector2(
                arenaOffset.X * World.World.TILE_SIZE + ExitPortalPoint.X,
                arenaOffset.Y * World.World.TILE_SIZE + ExitPortalPoint.Y
            );
        }
    }
}