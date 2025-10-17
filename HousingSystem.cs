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
            // Optimization: Only scan a small area around existing houses/doors
            // This prevents full world scans every 10 seconds
            
            // Scan around player spawn area on first check
            if (scannedDoors.Count == 0)
            {
                int spawnX = StarshroudHollows.World.World.WORLD_WIDTH / 2;
                int spawnY = 200;
                ScanAreaForDoors(spawnX - 100, spawnX + 100, spawnY - 50, spawnY + 50);
            }
            
            // Periodically expand search if no houses found
            // This is much cheaper than scanning entire world
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
            // Requirements:
            // 1. Enclosed rectangle (walls all around)
            // 2. Full background walls
            // 3. At least one bed
            // 4. At least one torch for lighting
            // 5. Minimum size: 6 wide x 6 tall, Maximum: 20 wide x 15 tall
            
            // Find the bounds of the room starting from the door
            Rectangle? bounds = FindRoomBounds(doorPos);
            if (!bounds.HasValue)
                return null;
            
            Rectangle room = bounds.Value;
            
            // Check size constraints
            if (room.Width < 6 || room.Width > 20 || room.Height < 6 || room.Height > 15)
            {
                return null;
            }
            
            // Validate all requirements
            if (!HasFullWalls(room))
                return null;
            
            if (!HasFullBackgroundWalls(room))
                return null;
            
            if (!HasBed(room))
                return null;
            
            if (!HasLighting(room))
                return null;
            
            // All checks passed!
            return new House(doorPos, room);
        }

        private Rectangle? FindRoomBounds(Point doorPos)
        {
            // Flood fill to find connected air space
            HashSet<Point> visitedTiles = new HashSet<Point>();
            Queue<Point> toVisit = new Queue<Point>();
            
            toVisit.Enqueue(new Point(doorPos.X, doorPos.Y + 1)); // Start inside the door
            
            int minX = doorPos.X;
            int maxX = doorPos.X;
            int minY = doorPos.Y;
            int maxY = doorPos.Y;
            
            while (toVisit.Count > 0 && visitedTiles.Count < 400) // Max 400 tiles (20x20 room)
            {
                Point current = toVisit.Dequeue();
                
                if (visitedTiles.Contains(current))
                    continue;
                
                // Check bounds
                if (current.X < 0 || current.X >= StarshroudHollows.World.World.WORLD_WIDTH ||
                    current.Y < 0 || current.Y >= StarshroudHollows.World.World.WORLD_HEIGHT)
                    continue;
                
                Tile tile = world.GetTile(current.X, current.Y);
                
                // If solid tile (not door), it's a wall - don't continue
                if (tile != null && tile.IsActive && tile.Type != TileType.Door)
                    continue;
                
                visitedTiles.Add(current);
                
                // Update bounds
                if (current.X < minX) minX = current.X;
                if (current.X > maxX) maxX = current.X;
                if (current.Y < minY) minY = current.Y;
                if (current.Y > maxY) maxY = current.Y;
                
                // Add neighbors
                toVisit.Enqueue(new Point(current.X - 1, current.Y));
                toVisit.Enqueue(new Point(current.X + 1, current.Y));
                toVisit.Enqueue(new Point(current.X, current.Y - 1));
                toVisit.Enqueue(new Point(current.X, current.Y + 1));
            }
            
            // If room is too open (hit max tiles), it's not enclosed
            if (visitedTiles.Count >= 400)
                return null;
            
            // Create rectangle with 1 tile padding for walls
            return new Rectangle(minX - 1, minY - 1, (maxX - minX) + 3, (maxY - minY) + 3);
        }

        private bool HasFullWalls(Rectangle room)
        {
            // Check top and bottom walls
            for (int x = room.Left; x < room.Right; x++)
            {
                Tile topTile = world.GetTile(x, room.Top);
                Tile bottomTile = world.GetTile(x, room.Bottom - 1);
                
                if (topTile == null || !topTile.IsActive || topTile.Type == TileType.Door)
                    return false;
                if (bottomTile == null || !bottomTile.IsActive || bottomTile.Type == TileType.Door)
                    return false;
            }
            
            // Check left and right walls (allow one door)
            int doorCount = 0;
            for (int y = room.Top; y < room.Bottom; y++)
            {
                Tile leftTile = world.GetTile(room.Left, y);
                Tile rightTile = world.GetTile(room.Right - 1, y);
                
                if (leftTile == null || (!leftTile.IsActive && leftTile.Type != TileType.Door))
                    return false;
                if (rightTile == null || (!rightTile.IsActive && rightTile.Type != TileType.Door))
                    return false;
                
                if (leftTile.Type == TileType.Door) doorCount++;
                if (rightTile.Type == TileType.Door) doorCount++;
            }
            
            // Must have exactly 1 door
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
            for (int x = room.Left; x < room.Right; x++)
            {
                for (int y = room.Top; y < room.Bottom; y++)
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
            // Check if there's at least one torch
            for (int x = room.Left; x < room.Right; x++)
            {
                for (int y = room.Top; y < room.Bottom; y++)
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
