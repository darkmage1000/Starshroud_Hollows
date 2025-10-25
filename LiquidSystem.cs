using StarshroudHollows.Enums;
using StarshroudHollows.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarshroudHollows.Systems
{
    public class LiquidSystem
    {
        private StarshroudHollows.World.World world;
        private Random random;
        
        // Player position tracking for regional updates
        private Vector2 lastPlayerPosition = Vector2.Zero;
        private float timeSinceLastRegionalUpdate = 0f;
        private const float REGIONAL_UPDATE_INTERVAL = 0.3f; // Update region every 0.3 seconds (was 0.5)
        private const int REGIONAL_UPDATE_RADIUS = 100; // Tiles around player (was 80 - larger radius)

        // HashSet to track all tiles that need a flow update in the next frame.
        private HashSet<Point> unstableLiquidTiles = new HashSet<Point>();
        
        // Track stagnant slivers for cleanup
        private Dictionary<Point, int> stagnantSlivers = new Dictionary<Point, int>();
        private const int MAX_STAGNANT_FRAMES = 60; // More patient before cleanup
        private const int MAX_TRACKED_SLIVERS = 300; // Track more slivers

        // Time-slicing: Max flow operations per frame
        private const int MAX_MOVES_PER_FRAME = 40; // INCREASED for better flow

        // UPDATED CONSTANTS for much better pool filling
        private const float MAX_FLOW_RATE = 0.30f; // VERY FAST flow rate
        private const float MIN_VOLUME_FOR_FLOW = 0.01f; // VERY LOW threshold
        private const float MIN_VOLUME_FOR_EXISTENCE = 0.015f; // Lower cleanup threshold
        private const float HORIZONTAL_DECAY = 0.00005f; // MINIMAL friction

        public LiquidSystem(StarshroudHollows.World.World world)
        {
            this.world = world;
            this.random = new Random();
        }

        // Called every frame by Game1.Update()
        public void UpdateFlow()
        {
            UpdateFlow(Vector2.Zero, 0f);
        }
        
        // Overload with player position and delta time for regional updates
        public void UpdateFlow(Vector2 playerPosition, float deltaTime)
        {
            // Regional update: Periodically activate liquids near the player
            if (playerPosition != Vector2.Zero && deltaTime > 0)
            {
                timeSinceLastRegionalUpdate += deltaTime;
                
                // Every REGIONAL_UPDATE_INTERVAL seconds, scan for liquids near player
                if (timeSinceLastRegionalUpdate >= REGIONAL_UPDATE_INTERVAL)
                {
                    timeSinceLastRegionalUpdate = 0f;
                    ActivateLiquidsNearPlayer(playerPosition);
                }
            }
            
            if (unstableLiquidTiles.Count == 0) return;

            // Convert to list and shuffle - but limit the batch size for performance
            List<Point> tilesToProcess = unstableLiquidTiles.Take(200).ToList(); // Only process 200 tiles per frame
            tilesToProcess = tilesToProcess.OrderBy(x => random.Next()).ToList();

            // Remove processed tiles from the set
            foreach (var tile in tilesToProcess)
            {
                unstableLiquidTiles.Remove(tile);
            }

            int movesPerformed = 0;
            foreach (Point p in tilesToProcess)
            {
                if (movesPerformed >= MAX_MOVES_PER_FRAME)
                {
                    // Put remaining back in the queue
                    for (int i = tilesToProcess.IndexOf(p); i < tilesToProcess.Count; i++)
                    {
                        unstableLiquidTiles.Add(tilesToProcess[i]);
                    }
                    break;
                }

                // Clean up tiny volumes BEFORE processing
                Tile currentTile = world.GetTile(p.X, p.Y);
                if (currentTile != null && currentTile.LiquidVolume > 0f && currentTile.LiquidVolume < MIN_VOLUME_FOR_EXISTENCE)
                {
                    currentTile.LiquidVolume = 0f;
                    currentTile.Type = TileType.Air;
                    stagnantSlivers.Remove(p);
                    continue;
                }

                if (IsLiquid(p.X, p.Y))
                {
                    CarryItems(p.X, p.Y);

                    if (SpreadLiquid(p.X, p.Y, out bool moved))
                    {
                        movesPerformed++;

                        // Queue neighbors for flow
                        QueueNeighborForFlow(p.X, p.Y);
                        QueueNeighborForFlow(p.X + 1, p.Y);
                        QueueNeighborForFlow(p.X - 1, p.Y);
                        QueueNeighborForFlow(p.X, p.Y + 1);
                        QueueNeighborForFlow(p.X, p.Y - 1);
                        
                        stagnantSlivers.Remove(p);
                    }
                    else if (world.GetTile(p.X, p.Y).LiquidVolume > 0f)
                    {
                        unstableLiquidTiles.Add(p);
                        
                        // Track stagnant small slivers
                        if (currentTile.LiquidVolume < 0.20f && stagnantSlivers.Count < MAX_TRACKED_SLIVERS)
                        {
                            if (stagnantSlivers.ContainsKey(p))
                            {
                                stagnantSlivers[p]++;
                                
                                if (stagnantSlivers[p] >= MAX_STAGNANT_FRAMES)
                                {
                                    currentTile.LiquidVolume = 0f;
                                    currentTile.Type = TileType.Air;
                                    stagnantSlivers.Remove(p);
                                }
                            }
                            else
                            {
                                stagnantSlivers[p] = 1;
                            }
                        }
                        else if (currentTile.LiquidVolume >= 0.20f)
                        {
                            stagnantSlivers.Remove(p);
                        }
                    }
                    else
                    {
                        stagnantSlivers.Remove(p);
                    }
                }
            }
        }

        public void TriggerLocalFlow(int changedX, int changedY)
        {
            // ENHANCED: When a block is removed, activate ALL adjacent liquids immediately
            // This ensures that liquids "held in place" by removed blocks will start flowing
            
            // First, activate immediate neighbors in a 3x3 grid (this catches liquids directly touching the removed block)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int checkX = changedX + dx;
                    int checkY = changedY + dy;
                    
                    if (IsLiquid(checkX, checkY))
                    {
                        // Force this liquid to update immediately
                        unstableLiquidTiles.Add(new Point(checkX, checkY));
                    }
                }
            }
            
            // Then trigger a wider 7x7 area for cascading effects
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    QueueNeighborForFlow(changedX + dx, changedY + dy);
                }
            }
        }

        public void StabilizeLiquids(float progressStart, float progressEnd, Action<float, string> updateCallback)
        {
            Logger.Log("[LIQUID] Starting comprehensive flow stabilization...");
            int stabilizationIterations = 0;

            // MORE iterations for better stability
            for (int iteration = 0; iteration < 120; iteration++)
            {
                stabilizationIterations = iteration + 1;
                bool liquidMoved = false;

                int worldW = StarshroudHollows.World.World.WORLD_WIDTH;
                int worldH = StarshroudHollows.World.World.WORLD_HEIGHT;

                List<Point> allLiquidTiles = new List<Point>();
                for (int x = 0; x < worldW; x += 1)
                {
                    for (int y = 0; y < worldH; y += 1)
                    {
                        if (IsLiquid(x, y)) allLiquidTiles.Add(new Point(x, y));
                    }
                }

                allLiquidTiles = allLiquidTiles.OrderBy(x => random.Next()).ToList();

                foreach (Point p in allLiquidTiles)
                {
                    if (SpreadLiquid(p.X, p.Y, out bool moved))
                    {
                        liquidMoved = true;
                    }
                }

                float currentProgress = progressStart + (progressEnd - progressStart) * (float)stabilizationIterations / 120f;
                updateCallback?.Invoke(currentProgress, $"Stabilizing liquids (Pass {stabilizationIterations}/120)...");

                if (!liquidMoved)
                {
                    Logger.Log($"[LIQUID] Stabilization achieved in {stabilizationIterations} passes.");
                    return;
                }
            }
            Logger.Log($"[LIQUID] Stabilization completed after max 120 passes.");
        }

        public void ActivateAllLiquids()
        {
            Logger.Log("[LIQUID] Activating all liquid tiles from save...");
            int worldW = StarshroudHollows.World.World.WORLD_WIDTH;
            int worldH = StarshroudHollows.World.World.WORLD_HEIGHT;
            int liquidCount = 0;

            for (int x = 0; x < worldW; x++)
            {
                for (int y = 0; y < worldH; y++)
                {
                    if (IsLiquid(x, y))
                    {
                        unstableLiquidTiles.Add(new Point(x, y));
                        liquidCount++;
                    }
                }
            }

            Logger.Log($"[LIQUID] Activated {liquidCount} liquid tiles for flow simulation");
        }
        
        // Activate liquids in a radius around the player
        private void ActivateLiquidsNearPlayer(Vector2 playerPosition)
        {
            int playerTileX = (int)(playerPosition.X / StarshroudHollows.World.World.TILE_SIZE);
            int playerTileY = (int)(playerPosition.Y / StarshroudHollows.World.World.TILE_SIZE);
            
            int startX = Math.Max(0, playerTileX - REGIONAL_UPDATE_RADIUS);
            int endX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, playerTileX + REGIONAL_UPDATE_RADIUS);
            int startY = Math.Max(0, playerTileY - REGIONAL_UPDATE_RADIUS);
            int endY = Math.Min(StarshroudHollows.World.World.WORLD_HEIGHT - 1, playerTileY + REGIONAL_UPDATE_RADIUS);
            
            // Sample every 2nd tile (was 3rd) to be more aggressive in finding liquids
            for (int x = startX; x <= endX; x += 2)
            {
                for (int y = startY; y <= endY; y += 2)
                {
                    if (IsLiquid(x, y))
                    {
                        unstableLiquidTiles.Add(new Point(x, y));
                        // Also add immediate neighbors for smoother flow
                        QueueNeighborForFlow(x, y);
                    }
                }
            }
        }

        private void CarryItems(int x, int y)
        {
            // Placeholder for DroppedItem logic
        }

        private bool CheckForObsidianFormation(int x, int y)
        {
            Tile sourceTile = world.GetTile(x, y);
            if (sourceTile == null) return false;

            TileType liquidType = sourceTile.Type;
            if (liquidType != TileType.Water && liquidType != TileType.Lava) return false;

            TileType oppositeLiquid = (liquidType == TileType.Water) ? TileType.Lava : TileType.Water;

            Point[] neighbors = new Point[]
            {
                new Point(x - 1, y), new Point(x + 1, y),
                new Point(x, y - 1), new Point(x, y + 1)
            };

            Tile neighborToConsume = null;

            foreach (Point p in neighbors)
            {
                Tile neighborTile = world.GetTile(p.X, p.Y);
                if (neighborTile != null &&
                    neighborTile.Type == oppositeLiquid &&
                    neighborTile.LiquidVolume > MIN_VOLUME_FOR_FLOW)
                {
                    neighborToConsume = neighborTile;
                    break;
                }
            }

            if (neighborToConsume != null)
            {
                sourceTile.Type = TileType.Obsidian;
                sourceTile.LiquidVolume = 0f;

                neighborToConsume.LiquidVolume -= 0.4f;
                if (neighborToConsume.LiquidVolume < MIN_VOLUME_FOR_EXISTENCE)
                {
                    neighborToConsume.LiquidVolume = 0f;
                    neighborToConsume.Type = TileType.Air;
                }

                return true;
            }

            return false;
        }

        private bool IsLiquid(int x, int y)
        {
            Tile tile = world.GetTile(x, y);
            return tile != null && (tile.Type == TileType.Water || tile.Type == TileType.Lava) && tile.LiquidVolume > MIN_VOLUME_FOR_EXISTENCE;
        }

        private bool IsSolid(int x, int y)
        {
            Tile tile = world.GetTile(x, y);

            // Check for player collision
            if (world.GetPlayer() != null)
            {
                Point playerTile = new Point((int)(world.GetPlayer().Position.X / StarshroudHollows.World.World.TILE_SIZE), (int)(world.GetPlayer().Position.Y / StarshroudHollows.World.World.TILE_SIZE));

                if (x >= playerTile.X && x <= playerTile.X + 1 && y >= playerTile.Y && y <= playerTile.Y + 2)
                {
                    return false;
                }
            }

            if (tile == null) return false;

            if (tile.Type == TileType.Door)
            {
                return !tile.IsDoorOpen;
            }

            switch (tile.Type)
            {
                case TileType.Air:
                case TileType.Water:
                case TileType.Lava:
                case TileType.Torch:
                case TileType.Sapling:
                    return false;
                default:
                    return true;
            }
        }

        private void QueueNeighborForFlow(int x, int y)
        {
            if (x >= 0 && x < StarshroudHollows.World.World.WORLD_WIDTH && y >= 0 && y < StarshroudHollows.World.World.WORLD_HEIGHT)
            {
                Tile tile = world.GetTile(x, y);
                if (tile == null) return;

                if (tile.LiquidVolume > 0f || tile.Type == TileType.Air)
                {
                    unstableLiquidTiles.Add(new Point(x, y));
                }
            }
        }

        // COMPLETELY REWRITTEN spread logic for perfect pool filling
        private bool SpreadLiquid(int x, int y, out bool moved)
        {
            moved = false;
            Tile sourceTile = world.GetTile(x, y);
            if (sourceTile == null || !IsLiquid(x, y)) return false;

            TileType liquidType = sourceTile.Type;
            float sourceVolume = sourceTile.LiquidVolume;

            if (sourceVolume < MIN_VOLUME_FOR_FLOW) return false;

            // Check for obsidian formation first
            if (CheckForObsidianFormation(x, y))
            {
                moved = true;
                return true;
            }

            // === PRIORITY 1: DOWNWARD FLOW ===
            Tile below = world.GetTile(x, y + 1);
            if (below != null && !IsSolid(x, y + 1))
            {
                if (below.Type == TileType.Air || below.Type == liquidType)
                {
                    float targetVolume = below.LiquidVolume;
                    float flowAmount = Math.Min(sourceVolume, MAX_FLOW_RATE);

                    if (targetVolume < 1.0f)
                    {
                        float spaceBelow = 1.0f - targetVolume;
                        float transferAmount = Math.Min(flowAmount, spaceBelow);

                        if (transferAmount > MIN_VOLUME_FOR_EXISTENCE)
                        {
                            below.LiquidVolume += transferAmount;
                            sourceTile.LiquidVolume -= transferAmount;

                            if (below.LiquidVolume > 0) below.Type = liquidType;

                            if (sourceTile.LiquidVolume < MIN_VOLUME_FOR_EXISTENCE)
                            {
                                sourceTile.LiquidVolume = 0f;
                                sourceTile.Type = TileType.Air;
                            }

                            moved = true;
                            return true;
                        }
                    }
                }
            }

            // === PRIORITY 2: HORIZONTAL EQUALIZATION ===
            // Only spread horizontally if we're sitting on solid ground or full liquid
            Tile belowTile = world.GetTile(x, y + 1);
            bool canSpreadHorizontally = (belowTile != null && IsSolid(x, y + 1)) || 
                                          (belowTile != null && belowTile.LiquidVolume >= 0.95f);

            if (canSpreadHorizontally)
            {
                Tile left = world.GetTile(x - 1, y);
                Tile right = world.GetTile(x + 1, y);

                // AGGRESSIVE EQUALIZATION - transfer 90% of the difference
                float targetVolume = -1f;
                Point targetPos = Point.Zero;

                // Check left
                if (left != null && !IsSolid(x - 1, y))
                {
                    if (left.Type == TileType.Air || left.Type == liquidType)
                    {
                        if (left.LiquidVolume < sourceVolume - 0.005f) // Small threshold
                        {
                            float difference = sourceVolume - left.LiquidVolume;
                            if (difference > targetVolume)
                            {
                                targetVolume = difference;
                                targetPos = new Point(x - 1, y);
                            }
                        }
                    }
                }

                // Check right
                if (right != null && !IsSolid(x + 1, y))
                {
                    if (right.Type == TileType.Air || right.Type == liquidType)
                    {
                        if (right.LiquidVolume < sourceVolume - 0.005f) // Small threshold
                        {
                            float difference = sourceVolume - right.LiquidVolume;
                            if (difference > targetVolume)
                            {
                                targetVolume = difference;
                                targetPos = new Point(x + 1, y);
                            }
                        }
                    }
                }

                if (targetVolume > 0.01f && targetPos != Point.Zero)
                {
                    Tile targetTile = world.GetTile(targetPos.X, targetPos.Y);
                    
                    // Transfer 90% of the difference for fast equalization
                    float transferAmount = Math.Min(MAX_FLOW_RATE, targetVolume * 0.9f);
                    
                    targetTile.LiquidVolume += transferAmount;
                    sourceTile.LiquidVolume -= (transferAmount + HORIZONTAL_DECAY);

                    if (targetTile.LiquidVolume > 0) targetTile.Type = liquidType;

                    if (sourceTile.LiquidVolume < MIN_VOLUME_FOR_EXISTENCE)
                    {
                        sourceTile.LiquidVolume = 0f;
                        sourceTile.Type = TileType.Air;
                    }

                    moved = true;
                    return true;
                }
            }

            return false;
        }
    }
}
