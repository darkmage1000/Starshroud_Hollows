using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Enums;
using StarshroudHollows.World;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.Systems.Housing
{
    /// <summary>
    /// Manages housing validation and NPC assignments
    /// </summary>
    public class HousingSystem
    {
        private StarshroudHollows.World.World world;
        private List<House> validHouses;
        private List<House> pendingHouses; // Houses waiting for NPC assignment
        private const float VALIDATION_INTERVAL = 10.0f; // Check every 10 seconds (reduced from 5)
        private float validationTimer = 0f;

        // Optimization: Track known door positions to avoid re-scanning
        private HashSet<Point> scannedDoors;

        // NPC management
        private List<NPC> activeNPCs;
        private List<NPCPortal> activePortals;
        private bool hasSpawnedGuide = false;
        public bool PlayerSurvivedFirstNight { get; set; } = false;

        public HousingSystem(StarshroudHollows.World.World world)
        {
            this.world = world;
            validHouses = new List<House>();
            pendingHouses = new List<House>();
            activeNPCs = new List<NPC>();
            activePortals = new List<NPCPortal>();
            scannedDoors = new HashSet<Point>();
        }

        public void Update(float deltaTime)
        {
            validationTimer += deltaTime;

            if (validationTimer >= VALIDATION_INTERVAL)
            {
                validationTimer = 0f;
                ScanForNewHouses();
            }

            // Update pending houses
            for (int i = pendingHouses.Count - 1; i >= 0; i--)
            {
                pendingHouses[i].Update(deltaTime);

                // After 60 seconds, house is ready for NPC
                if (pendingHouses[i].TimeValidated >= 60f && !pendingHouses[i].HasNPC)
                {
                    Logger.Log($"[HOUSING] House at ({pendingHouses[i].DoorPosition.X}, {pendingHouses[i].DoorPosition.Y}) ready for NPC!");

                    // Try to assign an NPC
                    AssignNPCToHouse(pendingHouses[i]);

                    // Move to valid houses
                    validHouses.Add(pendingHouses[i]);
                    pendingHouses.RemoveAt(i);
                }
            }
            
            // CRITICAL FIX: Check valid houses for NPC assignment too!
            // This handles cases where houses became valid BEFORE first night was survived
            if (PlayerSurvivedFirstNight && !hasSpawnedGuide)
            {
                // Only log once per second to avoid spam
                if (validationTimer < 0.1f)  // Only log at the start of each interval
                {
                    Logger.Log($"[HOUSING] First night survived! Checking {validHouses.Count} valid houses for NPC assignment...");
                }
                
                // Try to assign NPC to any valid house without one
                foreach (var house in validHouses)
                {
                    if (!house.HasNPC)
                    {
                        Logger.Log($"[HOUSING] Found house without NPC at ({house.DoorPosition.X}, {house.DoorPosition.Y}), assigning Guide...");
                        AssignNPCToHouse(house);
                        break; // Only assign one NPC at a time
                    }
                }
            }

            // Update all NPCs
            foreach (var npc in activeNPCs)
            {
                npc.Update(deltaTime, world);
            }

            // Update all portals
            for (int i = activePortals.Count - 1; i >= 0; i--)
            {
                activePortals[i].Update(deltaTime);

                // Spawn NPC when portal is ready
                if (activePortals[i].HasSpawnedNPC && !activePortals[i].TargetHouse.HasNPC)
                {
                    SpawnNPCFromPortal(activePortals[i]);
                }

                // Remove inactive portals
                if (!activePortals[i].IsActive)
                {
                    Logger.Log("[HOUSING] NPC portal closed");
                    activePortals.RemoveAt(i);
                }
            }
        }

        private void AssignNPCToHouse(House house)
        {
            // Starling Guide - spawns after first night
            if (!hasSpawnedGuide && PlayerSurvivedFirstNight)
            {
                // Spawn portal in the center of the house
                Vector2 portalPos = new Vector2(
                    (house.Bounds.X + house.Bounds.Width / 2) * StarshroudHollows.World.World.TILE_SIZE - 24,
                    (house.Bounds.Bottom - 3) * StarshroudHollows.World.World.TILE_SIZE - 64
                );

                NPCPortal portal = new NPCPortal(portalPos, house);
                activePortals.Add(portal);

                hasSpawnedGuide = true; // Mark as spawned (portal created)

                Logger.Log("[HOUSING] NPC arrival portal opened! The Starling Guide is arriving...");
            }
        }

        private void SpawnNPCFromPortal(NPCPortal portal)
        {
            House house = portal.TargetHouse;

            // Spawn the NPC at portal location
            StarlingGuide guide = new StarlingGuide(portal.Position);
            guide.AssignedHouse = house;
            activeNPCs.Add(guide);

            house.HasNPC = true;
            house.NPCType = "Guide";

            Logger.Log("[NPC] The Starling Guide has arrived through the portal!");
        }

        private void ScanForNewHouses()
        {
            // FIXED: Scan around PLAYER position instead of just spawn area
            // This ensures houses are detected wherever the player builds

            if (world.GetPlayer() != null)
            {
                var player = world.GetPlayer();
                Vector2 playerPos = player.Position;
                int playerTileX = (int)(playerPos.X / StarshroudHollows.World.World.TILE_SIZE);
                int playerTileY = (int)(playerPos.Y / StarshroudHollows.World.World.TILE_SIZE);

                // Scan large area around player (200 tiles = ~6400 pixels)
                ScanAreaForDoors(playerTileX - 200, playerTileX + 200, playerTileY - 100, playerTileY + 100);
            }
            else
            {
                // Fallback: Scan around spawn if no player
                int spawnX = StarshroudHollows.World.World.WORLD_WIDTH / 2;
                int spawnY = 200;
                ScanAreaForDoors(spawnX - 100, spawnX + 100, spawnY - 50, spawnY + 50);
            }
        }

        private void ScanAreaForDoors(int startX, int endX, int startY, int endY)
        {
            // Clamp to world bounds
            startX = Math.Max(0, startX);
            endX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, endX);
            startY = Math.Max(0, startY);
            endY = Math.Min(StarshroudHollows.World.World.WORLD_HEIGHT - 1, endY);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    Tile tile = world.GetTile(x, y);
                    if (tile != null && tile.Type == TileType.Door)
                    {
                        // FIX: Only detect the BOTTOM tile of the door (2 tiles tall)
                        // Check if there's a door tile above this one
                        Tile tileAbove = world.GetTile(x, y - 1);
                        if (tileAbove != null && tileAbove.Type == TileType.Door)
                        {
                            // This is the bottom tile, skip it
                            continue;
                        }
                        
                        // This is the top tile of the door, use it
                        Point doorPos = new Point(x, y);

                        // Skip if already scanned
                        if (scannedDoors.Contains(doorPos))
                            continue;

                        // Mark as scanned
                        scannedDoors.Add(doorPos);

                        // Check if we already know about this door
                        if (IsHouseAlreadyTracked(doorPos))
                            continue;

                        // Validate the house
                        House house = ValidateHouse(doorPos);
                        if (house != null)
                        {
                            pendingHouses.Add(house);
                            Logger.Log($"[HOUSING] New valid house found at door ({x}, {y})! Size: {house.Width}x{house.Height}");
                        }
                    }
                }
            }
        }

        // Public method to trigger a scan around a specific location (e.g., where player places door)
        public void ScanAroundPosition(int tileX, int tileY, int radius = 50)
        {
            ScanAreaForDoors(tileX - radius, tileX + radius, tileY - radius, tileY + radius);
        }
        
        // NEW: Manual validation trigger for debugging - press a key to force validation
        public void ForceValidateNearbyHouses(Vector2 playerPosition)
        {
            int playerTileX = (int)(playerPosition.X / StarshroudHollows.World.World.TILE_SIZE);
            int playerTileY = (int)(playerPosition.Y / StarshroudHollows.World.World.TILE_SIZE);
            
            Logger.Log("[HOUSING] ========== MANUAL HOUSE VALIDATION ==========");
            Logger.Log($"[HOUSING] Player position: ({playerTileX}, {playerTileY})");
            
            // Find nearest door within 30 tiles
            Point? nearestDoor = null;
            float nearestDist = float.MaxValue;
            
            for (int dx = -30; dx <= 30; dx++)
            {
                for (int dy = -30; dy <= 30; dy++)
                {
                    int checkX = playerTileX + dx;
                    int checkY = playerTileY + dy;
                    
                    Tile tile = world.GetTile(checkX, checkY);
                    if (tile != null && tile.Type == TileType.Door)
                    {
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (dist < nearestDist)
                        {
                            nearestDist = dist;
                            nearestDoor = new Point(checkX, checkY);
                        }
                    }
                }
            }
            
            if (nearestDoor.HasValue)
            {
                Logger.Log($"[HOUSING] Found door at ({nearestDoor.Value.X}, {nearestDoor.Value.Y})");
                
                // Force validation even if already tracked
                House house = ValidateHouse(nearestDoor.Value);
                if (house != null)
                {
                    Logger.Log("[HOUSING] ✅ House is VALID!");
                    
                    // CRITICAL FIX: Actually ADD the house to the system!
                    if (!IsHouseAlreadyTracked(nearestDoor.Value))
                    {
                        pendingHouses.Add(house);
                        scannedDoors.Add(nearestDoor.Value);
                        Logger.Log($"[HOUSING] ✅ House ADDED to pending list! It will be ready for NPCs in 60 seconds.");
                    }
                    else
                    {
                        Logger.Log($"[HOUSING] ℹ️ House already tracked in the system.");
                    }
                }
                else
                {
                    Logger.Log("[HOUSING] ❌ House is INVALID - check logs above for reason");
                }
            }
            else
            {
                Logger.Log("[HOUSING] ❌ No door found within 30 tiles of player");
            }
            
            Logger.Log("[HOUSING] =============================================");
        }

        private bool IsHouseAlreadyTracked(Point doorPos)
        {
            foreach (var house in validHouses)
                if (house.DoorPosition == doorPos) return true;

            foreach (var house in pendingHouses)
                if (house.DoorPosition == doorPos) return true;

            return false;
        }

        private House ValidateHouse(Point doorPos)
        {
            Logger.Log($"[HOUSING] ===========================================");
            Logger.Log($"[HOUSING] Validating house at door ({doorPos.X}, {doorPos.Y})...");
            Logger.Log($"[HOUSING] ===========================================");

            // Requirements:
            // 1. Enclosed rectangle (walls all around)
            // 2. Full background walls
            // 3. At least one bed
            // 4. At least one torch for lighting
            // 5. Minimum size: 6 wide x 6 tall, Maximum: 20 wide x 15 tall

            // Find the bounds of the room starting from the door
            Rectangle? bounds = FindRoomBounds(doorPos);
            if (!bounds.HasValue)
            {
                Logger.Log($"[HOUSING] ❌ Failed: Could not find room bounds (room not enclosed?)");
                Logger.Log($"[HOUSING] ===========================================");
                return null;
            }

            Rectangle room = bounds.Value;
            Logger.Log($"[HOUSING] ✓ Room bounds found: {room.Width}x{room.Height} at ({room.X}, {room.Y})");

            // Check size constraints
            if (room.Width < 6 || room.Width > 20 || room.Height < 6 || room.Height > 15)
            {
                Logger.Log($"[HOUSING] ❌ Failed: Size out of range");
                Logger.Log($"[HOUSING]    Need: 6-20 wide, 6-15 tall");
                Logger.Log($"[HOUSING]    Got: {room.Width} wide, {room.Height} tall");
                Logger.Log($"[HOUSING] ===========================================");
                return null;
            }
            Logger.Log($"[HOUSING] ✓ Size check passed: {room.Width}x{room.Height}");

            // Validate all requirements
            if (!HasFullWalls(room))
            {
                Logger.Log($"[HOUSING] ❌ Failed: Incomplete walls or missing/extra doors");
                Logger.Log($"[HOUSING] ===========================================");
                return null;
            }
            Logger.Log($"[HOUSING] ✓ Full walls check passed");

            if (!HasFullBackgroundWalls(room))
            {
                Logger.Log($"[HOUSING] ❌ Failed: Missing background walls (use hammer to place walls everywhere!)");
                Logger.Log($"[HOUSING] ===========================================");
                return null;
            }
            Logger.Log($"[HOUSING] ✓ Background walls check passed");

            if (!HasBed(room))
            {
                Logger.Log($"[HOUSING] ❌ Failed: No bed found in interior");
                Logger.Log($"[HOUSING] ===========================================");
                return null;
            }
            Logger.Log($"[HOUSING] ✓ Bed check passed");

            if (!HasLighting(room))
            {
                Logger.Log($"[HOUSING] ❌ Failed: No torch/lighting found in interior");
                Logger.Log($"[HOUSING] ===========================================");
                return null;
            }
            Logger.Log($"[HOUSING] ✓ Lighting check passed");

            // All checks passed!
            Logger.Log($"[HOUSING] ✅✅✅ House is VALID! All requirements met. ✅✅✅");
            Logger.Log($"[HOUSING] ===========================================");
            return new House(doorPos, room);
        }

        // ==================================================================
        // ===== FIXED METHOD BELOW =========================================
        // ==================================================================
        private Rectangle? FindRoomBounds(Point doorPos)
        {
            // Flood fill to find connected air space
            HashSet<Point> visitedTiles = new HashSet<Point>();
            Queue<Point> toVisit = new Queue<Point>();

            // --- NEW: Find a valid starting point for the flood fill ---
            // The old method could accidentally start on a solid floor tile.
            // This new logic checks for an empty tile to the left or right of the door.
            Point startPoint = Point.Zero;
            Tile tileLeft = world.GetTile(doorPos.X - 1, doorPos.Y);
            Tile tileRight = world.GetTile(doorPos.X + 1, doorPos.Y);

            if (tileLeft != null && !tileLeft.IsActive)
            {
                startPoint = new Point(doorPos.X - 1, doorPos.Y);
            }
            else if (tileRight != null && !tileRight.IsActive)
            {
                startPoint = new Point(doorPos.X + 1, doorPos.Y);
            }
            else
            {
                Logger.Log($"[HOUSING] Failed: Could not find empty space next to door at ({doorPos.X}, {doorPos.Y}).");
                return null;
            }

            toVisit.Enqueue(startPoint);

            // --- NEW: Correctly initialize bounds ---
            int minX = startPoint.X;
            int maxX = startPoint.X;
            int minY = startPoint.Y;
            int maxY = startPoint.Y;

            while (toVisit.Count > 0 && visitedTiles.Count < 400) // Max 400 tiles (20x20 room)
            {
                Point current = toVisit.Dequeue();

                if (visitedTiles.Contains(current))
                    continue;

                // Check world bounds
                if (current.X < 0 || current.X >= StarshroudHollows.World.World.WORLD_WIDTH ||
                    current.Y < 0 || current.Y >= StarshroudHollows.World.World.WORLD_HEIGHT)
                    continue;

                Tile tile = world.GetTile(current.X, current.Y);

                // --- FIX: Treat ANY active tile (wall, door, etc.) as a boundary ---
                if (tile != null && tile.IsActive)
                    continue;

                visitedTiles.Add(current);

                // Update bounds based on the actual empty space found
                if (current.X < minX) minX = current.X;
                if (current.X > maxX) maxX = current.X;
                if (current.Y < minY) minY = current.Y;
                if (current.Y > maxY) maxY = current.Y;

                // Add neighbors to the queue
                toVisit.Enqueue(new Point(current.X - 1, current.Y));
                toVisit.Enqueue(new Point(current.X + 1, current.Y));
                toVisit.Enqueue(new Point(current.X, current.Y - 1));
                toVisit.Enqueue(new Point(current.X, current.Y + 1));
            }

            // If room is too open (hit max tiles), it's not enclosed
            if (visitedTiles.Count >= 400)
            {
                Logger.Log($"[HOUSING] Failed: Room is not enclosed. Flood fill explored too large an area.");
                return null;
            }

            if (visitedTiles.Count == 0)
            {
                Logger.Log($"[HOUSING] Failed: Flood fill found no empty space.");
                return null;
            }

            // Create a rectangle that represents the walls around the discovered empty space.
            // minX/maxX define the interior space. The walls are one tile outside of that.
            return new Rectangle(minX - 1, minY - 1, (maxX - minX) + 3, (maxY - minY) + 3);
        }

        private bool HasFullWalls(Rectangle room)
        {
            // Count total doors in the entire perimeter
            int doorCount = 0;
            
            // Check top wall
            for (int x = room.Left; x < room.Right; x++)
            {
                Tile topTile = world.GetTile(x, room.Top);
                
                if (topTile == null)
                    return false;
                    
                // Must be solid or door
                if (!topTile.IsActive && topTile.Type != TileType.Door)
                    return false;
                    
                // Count doors (only count top piece of 2-tall door)
                if (topTile.Type == TileType.Door)
                {
                    Tile tileAbove = world.GetTile(x, room.Top - 1);
                    if (tileAbove == null || tileAbove.Type != TileType.Door)
                    {
                        doorCount++;
                    }
                }
            }
            
            // Check bottom wall
            for (int x = room.Left; x < room.Right; x++)
            {
                Tile bottomTile = world.GetTile(x, room.Bottom - 1);
                
                if (bottomTile == null)
                    return false;
                    
                // Must be solid or door
                if (!bottomTile.IsActive && bottomTile.Type != TileType.Door)
                    return false;
                    
                // Count doors (only count top piece of 2-tall door)
                if (bottomTile.Type == TileType.Door)
                {
                    Tile tileAbove = world.GetTile(x, room.Bottom - 2);
                    if (tileAbove == null || tileAbove.Type != TileType.Door)
                    {
                        doorCount++;
                    }
                }
            }

            // Check left and right walls
            for (int y = room.Top; y < room.Bottom; y++)
            {
                // --- Check Left Wall ---
                Tile leftTile = world.GetTile(room.Left, y);
                if (leftTile == null)
                    return false;
                    
                // Must be solid or door
                if (!leftTile.IsActive && leftTile.Type != TileType.Door)
                    return false;

                if (leftTile.Type == TileType.Door)
                {
                    // Only count the top piece of the door
                    Tile tileAbove = world.GetTile(room.Left, y - 1);
                    if (tileAbove == null || tileAbove.Type != TileType.Door)
                    {
                        doorCount++;
                    }
                }

                // --- Check Right Wall ---
                Tile rightTile = world.GetTile(room.Right - 1, y);
                if (rightTile == null)
                    return false;
                    
                // Must be solid or door
                if (!rightTile.IsActive && rightTile.Type != TileType.Door)
                    return false;

                if (rightTile.Type == TileType.Door)
                {
                    // Only count the top piece of the door
                    Tile tileAbove = world.GetTile(room.Right - 1, y - 1);
                    if (tileAbove == null || tileAbove.Type != TileType.Door)
                    {
                        doorCount++;
                    }
                }
            }

            // A valid house must have exactly one door anywhere on the perimeter
            return doorCount == 1;
        }

        private bool HasFullBackgroundWalls(Rectangle room)
        {
            // Check every interior tile has a background wall
            for (int x = room.Left + 1; x < room.Right - 1; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom - 1; y++)
                {
                    Tile tile = world.GetTile(x, y);
                    if (tile == null || !tile.HasWall)
                        return false;
                }
            }
            return true;
        }

        private bool HasBed(Rectangle room)
        {
            // FIXED: Only check INTERIOR tiles (exclude the walls)
            for (int x = room.Left + 1; x < room.Right - 1; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom - 1; y++)
                {
                    Tile tile = world.GetTile(x, y);
                    if (tile != null && tile.Type == TileType.Bed)
                        return true;
                }
            }
            return false;
        }

        private bool HasLighting(Rectangle room)
        {
            // FIXED: Only check INTERIOR tiles (exclude the walls)
            for (int x = room.Left + 1; x < room.Right - 1; x++)
            {
                for (int y = room.Top + 1; y < room.Bottom - 1; y++)
                {
                    Tile tile = world.GetTile(x, y);
                    if (tile != null && tile.Type == TileType.Torch)
                        return true;
                }
            }
            return false;
        }

        public List<House> GetValidHouses() => validHouses;
        public List<House> GetPendingHouses() => pendingHouses;
        public List<NPC> GetActiveNPCs() => activeNPCs;
        
        // NEW: Get house save data
        public List<Systems.HouseData> GetHouseSaveData()
        {
            var houseData = new List<Systems.HouseData>();
            
            // Save valid houses
            foreach (var house in validHouses)
            {
                houseData.Add(new Systems.HouseData
                {
                    DoorX = house.DoorPosition.X,
                    DoorY = house.DoorPosition.Y,
                    BoundsX = house.Bounds.X,
                    BoundsY = house.Bounds.Y,
                    BoundsWidth = house.Bounds.Width,
                    BoundsHeight = house.Bounds.Height,
                    TimeValidated = house.TimeValidated,
                    HasNPC = house.HasNPC,
                    NPCType = house.NPCType,
                    IsPending = false
                });
            }
            
            // Save pending houses
            foreach (var house in pendingHouses)
            {
                houseData.Add(new Systems.HouseData
                {
                    DoorX = house.DoorPosition.X,
                    DoorY = house.DoorPosition.Y,
                    BoundsX = house.Bounds.X,
                    BoundsY = house.Bounds.Y,
                    BoundsWidth = house.Bounds.Width,
                    BoundsHeight = house.Bounds.Height,
                    TimeValidated = house.TimeValidated,
                    HasNPC = house.HasNPC,
                    NPCType = house.NPCType,
                    IsPending = true
                });
            }
            
            Logger.Log($"[HOUSING] Saving {houseData.Count} houses ({validHouses.Count} valid, {pendingHouses.Count} pending)");
            return houseData;
        }
        
        // NEW: Load houses from save data
        public void LoadHouses(List<Systems.HouseData> houseDataList)
        {
            if (houseDataList == null || houseDataList.Count == 0)
            {
                Logger.Log("[HOUSING] No houses to load");
                return;
            }
            
            foreach (var data in houseDataList)
            {
                Point doorPos = new Point(data.DoorX, data.DoorY);
                Rectangle bounds = new Rectangle(data.BoundsX, data.BoundsY, data.BoundsWidth, data.BoundsHeight);
                
                House house = new House(doorPos, bounds);
                house.HasNPC = data.HasNPC;
                house.NPCType = data.NPCType;
                
                // Set time validated using reflection or direct field access
                // Since TimeValidated is read-only, we need to manually set it
                var timeField = typeof(House).GetField("<TimeValidated>k__BackingField", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                timeField?.SetValue(house, data.TimeValidated);
                
                if (data.IsPending)
                {
                    pendingHouses.Add(house);
                }
                else
                {
                    validHouses.Add(house);
                }
                
                // Mark door as scanned
                scannedDoors.Add(doorPos);
            }
            
            Logger.Log($"[HOUSING] Loaded {houseDataList.Count} houses ({validHouses.Count} valid, {pendingHouses.Count} pending)");
        }
        
        // NEW: Get NPC save data
        public List<Systems.NPCData> GetNPCSaveData()
        {
            var npcData = new List<Systems.NPCData>();
            
            foreach (var npc in activeNPCs)
            {
                npcData.Add(new Systems.NPCData
                {
                    NPCType = npc.Type,
                    PositionX = npc.Position.X,
                    PositionY = npc.Position.Y,
                    HouseDoorX = npc.AssignedHouse?.DoorPosition.X ?? 0,
                    HouseDoorY = npc.AssignedHouse?.DoorPosition.Y ?? 0,
                    CurrentDialogueIndex = npc.GetDialogueIndex()
                });
            }
            
            Logger.Log($"[HOUSING] Saving {npcData.Count} NPCs");
            return npcData;
        }
        
        // NEW: Load NPCs from save data
        public void LoadNPCs(List<Systems.NPCData> npcDataList)
        {
            if (npcDataList == null || npcDataList.Count == 0)
            {
                Logger.Log("[HOUSING] No NPCs to load");
                return;
            }
            
            foreach (var data in npcDataList)
            {
                // Find the house this NPC was assigned to
                House assignedHouse = null;
                Point doorPos = new Point(data.HouseDoorX, data.HouseDoorY);
                
                foreach (var house in validHouses)
                {
                    if (house.DoorPosition == doorPos)
                    {
                        assignedHouse = house;
                        break;
                    }
                }
                
                if (assignedHouse == null)
                {
                    Logger.Log($"[HOUSING] Could not find house for NPC at door ({data.HouseDoorX}, {data.HouseDoorY})");
                    continue;
                }
                
                // Spawn the appropriate NPC type
                NPC npc = null;
                Vector2 position = new Vector2(data.PositionX, data.PositionY);
                
                if (data.NPCType == "Guide")
                {
                    npc = new StarlingGuide(position);
                    hasSpawnedGuide = true;
                }
                // Add more NPC types here in the future
                
                if (npc != null)
                {
                    npc.AssignedHouse = assignedHouse;
                    npc.SetDialogueIndex(data.CurrentDialogueIndex);
                    activeNPCs.Add(npc);
                    assignedHouse.HasNPC = true;
                    assignedHouse.NPCType = data.NPCType;
                    Logger.Log($"[HOUSING] Loaded {data.NPCType} NPC at ({data.PositionX}, {data.PositionY})");
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            // Draw portals first (behind NPCs)
            foreach (var portal in activePortals)
            {
                portal.Draw(spriteBatch, pixelTexture);
            }

            // Draw NPCs
            foreach (var npc in activeNPCs)
            {
                npc.Draw(spriteBatch, pixelTexture);
            }
        }

        public NPC GetNearestNPC(Vector2 playerPosition, float maxDistance = 64f)
        {
            NPC nearest = null;
            float nearestDist = maxDistance;

            foreach (var npc in activeNPCs)
            {
                float dist = Vector2.Distance(playerPosition, npc.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = npc;
                }
            }

            return nearest;
        }
    }

    /// <summary>
    /// Represents a valid house structure
    /// </summary>
    public class House
    {
        public Point DoorPosition { get; private set; }
        public Rectangle Bounds { get; private set; }
        public int Width => Bounds.Width;
        public int Height => Bounds.Height;
        public float TimeValidated { get; private set; }
        public bool HasNPC { get; set; }
        public string NPCType { get; set; }

        public House(Point doorPosition, Rectangle bounds)
        {
            DoorPosition = doorPosition;
            Bounds = bounds;
            TimeValidated = 0f;
            HasNPC = false;
            NPCType = null;
        }

        public void Update(float deltaTime)
        {
            TimeValidated += deltaTime;
        }
    }
}