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

        // HashSet to track all tiles that need a flow update in the next frame.
        private HashSet<Point> unstableLiquidTiles = new HashSet<Point>();
        
        // NEW: Track stagnant slivers for cleanup (limited size to prevent FPS issues)
        private Dictionary<Point, int> stagnantSlivers = new Dictionary<Point, int>();
        private const int MAX_STAGNANT_FRAMES = 30; // INCREASED - only remove truly stuck slivers
        private const int MAX_TRACKED_SLIVERS = 200; // Limit tracking to prevent FPS drops

        // Time-slicing constant: Max flow operations per frame to prevent lag/crash.
        private const int MAX_MOVES_PER_FRAME = 20; // REDUCED from 50 to prevent stutters

        // UPDATED CONSTANTS for volumetric flow
        private const float MAX_FLOW_RATE = 0.10f; // INCREASED - faster flow
        private const float MIN_VOLUME_FOR_FLOW = 0.05f; // LOWERED - allows smaller amounts to flow
        private const float MIN_VOLUME_FOR_EXISTENCE = 0.03f; // Aggressive cleanup
        private const float HORIZONTAL_DECAY = 0.0005f; // REDUCED - less friction, faster spread

        public LiquidSystem(StarshroudHollows.World.World world)
        {
            this.world = world;
            this.random = new Random();
        }

        // --- PUBLIC API ---

        // Called every frame by Game1.Update()
        public void UpdateFlow()
        {
            if (unstableLiquidTiles.Count == 0) return;

            // 1. Convert the hash set to a list and shuffle it to prevent directional bias
            List<Point> tilesToProcess = unstableLiquidTiles.ToList();
            tilesToProcess = tilesToProcess.OrderBy(x => random.Next()).ToList();

            // 2. Clear the set and prepare a new set for *new* unstable spots generated this frame
            unstableLiquidTiles.Clear();

            int movesPerformed = 0;
            foreach (Point p in tilesToProcess)
            {
                if (movesPerformed >= MAX_MOVES_PER_FRAME)
                {
                    // If limit is reached, put the remaining tiles back into the unstable set for the next frame
                    for (int i = tilesToProcess.IndexOf(p); i < tilesToProcess.Count; i++)
                    {
                        unstableLiquidTiles.Add(tilesToProcess[i]);
                    }
                    break;
                }

                // CRITICAL FIX: Clean up tiny volumes BEFORE processing
                Tile currentTile = world.GetTile(p.X, p.Y);
                if (currentTile != null && currentTile.LiquidVolume > 0f && currentTile.LiquidVolume < MIN_VOLUME_FOR_EXISTENCE)
                {
                    currentTile.LiquidVolume = 0f;
                    currentTile.Type = TileType.Air;
                    stagnantSlivers.Remove(p);
                    continue; // Skip to next tile
                }

                // Check for item movement only when liquid is stable at this spot
                if (IsLiquid(p.X, p.Y))
                {
                    // Carry dropped items along with the flow (even if slow)
                    CarryItems(p.X, p.Y);

                    if (SpreadLiquid(p.X, p.Y, out bool moved))
                    {
                        movesPerformed++;

                        // If liquid moved, queue the neighbors (flow sources) and the destination.
                        QueueNeighborForFlow(p.X, p.Y);
                        QueueNeighborForFlow(p.X + 1, p.Y);
                        QueueNeighborForFlow(p.X - 1, p.Y);
                        QueueNeighborForFlow(p.X, p.Y + 1);   // Tile below is critical for cascade
                        QueueNeighborForFlow(p.X, p.Y - 1);   // Tile above (for next layer to fall)
                        
                        // Reset stagnant counter if it moved
                        stagnantSlivers.Remove(p);
                    }
                    // If it didn't move, but still has volume, requeue for another check next frame
                    else if (world.GetTile(p.X, p.Y).LiquidVolume > 0f)
                    {
                        unstableLiquidTiles.Add(p);
                        
                        // NEW: Track stagnant small slivers (with size limit to prevent FPS drops)
                        if (currentTile.LiquidVolume < 0.15f && stagnantSlivers.Count < MAX_TRACKED_SLIVERS)
                        {
                            if (stagnantSlivers.ContainsKey(p))
                            {
                                stagnantSlivers[p]++;
                                
                                // Remove if stuck too long
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
                        else if (currentTile.LiquidVolume >= 0.15f)
                        {
                            // If volume is substantial, reset stagnant counter
                            stagnantSlivers.Remove(p);
                        }
                    }
                    else
                    {
                        // Volume is 0, remove from stagnant tracking
                        stagnantSlivers.Remove(p);
                    }
                }
            }
        }

        // Called by World.SetTile when a block is mined or liquid is placed
        public void TriggerLocalFlow(int changedX, int changedY)
        {
            // Add the changed tile and its neighbors to the unstable set to start flow on the next frame.
            QueueNeighborForFlow(changedX + 1, changedY);
            QueueNeighborForFlow(changedX - 1, changedY);
            QueueNeighborForFlow(changedX, changedY + 1);
            QueueNeighborForFlow(changedX, changedY - 1);
            QueueNeighborForFlow(changedX, changedY);
        }

        // Called by WorldGenerator to stabilize liquids immediately after generation
        public void StabilizeLiquids(float progressStart, float progressEnd, Action<float, string> updateCallback)
        {
            Logger.Log("[LIQUID] Starting comprehensive flow stabilization...");
            int stabilizationIterations = 0;

            // INCREASED: More iterations for better stability (was 40, now 80)
            for (int iteration = 0; iteration < 80; iteration++)
            {
                stabilizationIterations = iteration + 1;
                bool liquidMoved = false;

                int worldW = StarshroudHollows.World.World.WORLD_WIDTH;
                int worldH = StarshroudHollows.World.World.WORLD_HEIGHT;

                // Create a list of all liquid tiles across the world to check in this pass
                List<Point> allLiquidTiles = new List<Point>();
                for (int x = 0; x < worldW; x += 1)
                {
                    for (int y = 0; y < worldH; y += 1)
                    {
                        if (IsLiquid(x, y)) allLiquidTiles.Add(new Point(x, y));
                    }
                }

                // Shuffle the list to prevent slow, directional flow bias during stabilization
                allLiquidTiles = allLiquidTiles.OrderBy(x => random.Next()).ToList();

                foreach (Point p in allLiquidTiles)
                {
                    if (SpreadLiquid(p.X, p.Y, out bool moved))
                    {
                        liquidMoved = true;
                    }
                }

                // Update progress for the UI
                float currentProgress = progressStart + (progressEnd - progressStart) * (float)stabilizationIterations / 80f;
                updateCallback?.Invoke(currentProgress, $"Stabilizing liquids (Pass {stabilizationIterations}/80)...");

                // If no liquid moved in a full pass, we are stable.
                if (!liquidMoved)
                {
                    Logger.Log($"[LIQUID] Stabilization achieved in {stabilizationIterations} passes.");
                    return;
                }
            }
            Logger.Log($"[LIQUID] Stabilization completed after max 80 passes.");
        }

        // NEW: Activate all loaded liquids (call this after loading a save)
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


        // --- INTERNAL LOGIC ---

        private void CarryItems(int x, int y)
        {
            // Placeholder for DroppedItem logic
        }

        private bool IsLiquid(int x, int y)
        {
            Tile tile = world.GetTile(x, y);
            // Check if the tile type is water/lava AND has volume
            return tile != null && (tile.Type == TileType.Water || tile.Type == TileType.Lava) && tile.LiquidVolume > MIN_VOLUME_FOR_EXISTENCE;
        }

        private bool IsSolid(int x, int y)
        {
            // CRITICAL FIX: The liquid system must IGNORE player collision for flow purposes, and use Tile.Type 
            // to check for non-liquid barriers, bypassing the ambiguous IsActive property.

            Tile tile = world.GetTile(x, y);

            // 1. Check if the tile is occupied by the player (flow should pass through the player)
            if (world.GetPlayer() != null)
            {
                Point playerTile = new Point((int)(world.GetPlayer().Position.X / StarshroudHollows.World.World.TILE_SIZE), (int)(world.GetPlayer().Position.Y / StarshroudHollows.World.World.TILE_SIZE));

                // Check the 2x3 grid typically occupied by the player
                if (x >= playerTile.X && x <= playerTile.X + 1 && y >= playerTile.Y && y <= playerTile.Y + 2)
                {
                    return false; // Player spot is non-solid for flow
                }
            }

            // 2. Check for intrinsic solid block type (Dirt, Stone, Wood, etc.)
            if (tile == null) return false;

            switch (tile.Type)
            {
                case TileType.Air:
                case TileType.Water:
                case TileType.Lava:
                case TileType.Torch:
                case TileType.Sapling:
                    return false; // Allows flow to pass through
                default:
                    return true; // Assume all other types (Dirt, Stone, Chests, Benches) are solid barriers
            }
        }


        private void QueueNeighborForFlow(int x, int y)
        {
            if (x >= 0 && x < StarshroudHollows.World.World.WORLD_WIDTH && y >= 0 && y < StarshroudHollows.World.World.WORLD_HEIGHT)
            {
                // Only add it if it has volume or if it's the destination of flow (air)
                Tile tile = world.GetTile(x, y);
                if (tile == null) return;

                if (tile.LiquidVolume > 0f || tile.Type == TileType.Air)
                {
                    unstableLiquidTiles.Add(new Point(x, y));
                }
            }
        }

        // Core physics logic: Downward consumption, then sideways spreading.
        private bool SpreadLiquid(int x, int y, out bool moved)
        {
            moved = false;
            Tile sourceTile = world.GetTile(x, y);
            if (sourceTile == null || !IsLiquid(x, y)) return false;

            TileType liquidType = sourceTile.Type;
            float sourceVolume = sourceTile.LiquidVolume;

            if (sourceVolume < MIN_VOLUME_FOR_FLOW) return false;

            // --- 1. Downward Flow (Highest Priority) ---
            Tile below = world.GetTile(x, y + 1);
            if (below != null && !IsSolid(x, y + 1))
            {
                // CRITICAL FIX: Only allow flow into AIR or LIQUID tiles, never into solid blocks
                if (below.Type == TileType.Air || below.Type == TileType.Water || below.Type == TileType.Lava)
                {
                    float targetVolume = below.LiquidVolume;
                    float flowAmount = Math.Min(sourceVolume, MAX_FLOW_RATE);

                    // If the block below is empty or less than full, the source liquid moves down.
                    if (targetVolume < 1.0f)
                    {
                        // Calculate how much volume can move down before the block below is full
                        float spaceBelow = 1.0f - targetVolume;
                        float transferAmount = Math.Min(flowAmount, spaceBelow);

                        if (transferAmount > MIN_VOLUME_FOR_EXISTENCE)
                        {
                            below.LiquidVolume += transferAmount;
                            sourceTile.LiquidVolume -= transferAmount;

                            // Set the new liquid type (important for converting air/water to lava)
                            if (below.LiquidVolume > 0) below.Type = liquidType;

                            // Clean up source if empty
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

            // --- 2. Sideways Flow (Only if below is occupied AND difference is large enough) ---
            if (IsSolid(x, y + 1) || (world.GetTile(x, y + 1) != null && world.GetTile(x, y + 1).LiquidVolume > 0.9f))
            {
                Tile left = world.GetTile(x - 1, y);
                Tile right = world.GetTile(x + 1, y);

                float flowAmount = Math.Min(sourceVolume, MAX_FLOW_RATE);

                float currentVolume = sourceVolume;

                // Attempt to flow left or right into a tile with less than MAX_VOLUME
                Point targetPos = new Point(0, 0);
                float bestFlow = 0f;

                // 2a. Check Left
                if (left != null && !IsSolid(x - 1, y))
                {
                    // CRITICAL FIX: Only allow flow into AIR or LIQUID tiles
                    if (left.Type == TileType.Air || left.Type == TileType.Water || left.Type == TileType.Lava)
                    {
                        if (left.LiquidVolume < 1.0f)
                        {
                            // IMPROVED: More aggressive equalization - transfer 80% of difference
                            float volumeDifference = currentVolume - left.LiquidVolume;
                            float flowLeft = volumeDifference * 0.8f;

                            // LOWERED threshold for better spreading
                            if (flowLeft > 0.01f && flowLeft > bestFlow)
                            {
                                bestFlow = flowLeft;
                                targetPos = new Point(x - 1, y);
                            }
                        }
                    }
                }

                // 2b. Check Right
                if (right != null && !IsSolid(x + 1, y))
                {
                    // CRITICAL FIX: Only allow flow into AIR or LIQUID tiles
                    if (right.Type == TileType.Air || right.Type == TileType.Water || right.Type == TileType.Lava)
                    {
                        if (right.LiquidVolume < 1.0f)
                        {
                            // IMPROVED: More aggressive equalization - transfer 80% of difference
                            float volumeDifference = currentVolume - right.LiquidVolume;
                            float flowRight = volumeDifference * 0.8f;

                            // LOWERED threshold for better spreading
                            if (flowRight > 0.01f && flowRight > bestFlow)
                            {
                                bestFlow = flowRight;
                                targetPos = new Point(x + 1, y);
                            }
                        }
                    }
                }

                if (bestFlow > 0.001f)
                {
                    float transferAmount = Math.Min(flowAmount, bestFlow);

                    // We only proceed if targetPos was updated (not still 0,0) AND the target is not null
                    if (targetPos.X != 0 || targetPos.Y != 0)
                    {
                        Tile targetTile = world.GetTile(targetPos.X, targetPos.Y);

                        // Final check to prevent oscillation and ensure flow only happens if we have more volume
                        // IMPROVED: Added small threshold to prevent micro-oscillations
                        if (targetTile.LiquidVolume < currentVolume - 0.01f)
                        {
                            targetTile.LiquidVolume += transferAmount;

                            // Apply horizontal decay/friction
                            sourceTile.LiquidVolume -= transferAmount + HORIZONTAL_DECAY;

                            if (targetTile.LiquidVolume > 0) targetTile.Type = liquidType;
                            moved = true;
                        }
                    }

                    // Clean up source if empty
                    if (sourceTile.LiquidVolume < MIN_VOLUME_FOR_EXISTENCE)
                    {
                        sourceTile.LiquidVolume = 0f;
                        sourceTile.Type = TileType.Air;
                    }

                    return moved;
                }
            }

            return false;
        }
    }
}
