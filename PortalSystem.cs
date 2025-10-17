using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Entities;
using StarshroudHollows.Enums;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.Systems
{
    /// <summary>
    /// Manages all portals, altars, and boss arenas
    /// </summary>
    public class PortalSystem
    {
        private World.World world;
        private List<SummonAltar> altars;
        private List<Portal> activePortals;
        
        // Arena state
        private BossArena currentArena;
        private bool isInArena = false;
        private Point arenaOffset; // Where in the world the arena is built
        private Vector2 returnPosition; // Where player came from
        private BossType currentBossType;
        
        // Arena location in world (far from spawn)
        private const int ARENA_WORLD_X = 100;
        private const int ARENA_WORLD_Y = 100;

        public bool IsInArena => isInArena;
        public BossType CurrentBoss => currentBossType;

        public PortalSystem(World.World world)
        {
            this.world = world;
            altars = new List<SummonAltar>();
            activePortals = new List<Portal>();
        }

        public void PlaceAltar(Point position, BossType bossType)
        {
            SummonAltar altar = new SummonAltar(position, bossType);
            altars.Add(altar);
            Logger.Log($"[PORTAL] Placed {bossType} altar at ({position.X}, {position.Y})");
        }

        public void RemoveAltar(Point position)
        {
            altars.RemoveAll(a => a.Position == position);
            Logger.Log($"[PORTAL] Removed altar at ({position.X}, {position.Y})");
        }

        public bool TryActivateAltar(Point position, ItemType summonItem, Inventory inventory)
        {
            SummonAltar altar = altars.Find(a => a.Position == position);
            if (altar == null) return false;

            if (altar.TryActivate(summonItem))
            {
                // Consume the summon item
                inventory.RemoveItem(summonItem, 1);
                
                // Spawn entrance portal above the altar
                Vector2 portalPos = new Vector2(
                    position.X * World.World.TILE_SIZE,
                    (position.Y - 3) * World.World.TILE_SIZE
                );
                
                Portal entrancePortal = new Portal(portalPos, PortalType.Entrance, altar.AssociatedBoss);
                activePortals.Add(entrancePortal);
                
                Logger.Log($"[PORTAL] Spawned entrance portal for {altar.AssociatedBoss}");
                return true;
            }
            
            return false;
        }

        public void Update(float deltaTime, Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            // Update all altars
            foreach (var altar in altars)
            {
                altar.Update(deltaTime);
            }

            // Update all portals
            foreach (var portal in activePortals)
            {
                portal.Update(deltaTime);
            }

            // Check for player collision with portals
            CheckPortalCollisions(playerPosition, playerWidth, playerHeight);
        }

        private void CheckPortalCollisions(Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            foreach (var portal in activePortals)
            {
                if (portal.CheckPlayerCollision(playerPosition, playerWidth, playerHeight))
                {
                    if (portal.Type == PortalType.Entrance && !isInArena)
                    {
                        // Enter arena
                        EnterArena(playerPosition, portal.DestinationBoss);
                    }
                    else if (portal.Type == PortalType.Exit && isInArena)
                    {
                        // Exit arena
                        ExitArena();
                    }
                    break;
                }
            }
        }

        private void EnterArena(Vector2 currentPlayerPos, BossType boss)
        {
            Logger.Log($"[PORTAL] Entering {boss} arena...");
            
            // Save return position
            returnPosition = currentPlayerPos;
            currentBossType = boss;
            
            // Generate arena
            arenaOffset = new Point(ARENA_WORLD_X, ARENA_WORLD_Y);
            currentArena = new BossArena(boss, Environment.TickCount);
            currentArena.BuildArenaStructure(world, arenaOffset);
            
            // Remove entrance portal
            activePortals.RemoveAll(p => p.Type == PortalType.Entrance);
            
            isInArena = true;
            
            Logger.Log("[PORTAL] Arena entered! Teleporting player...");
        }

        private void ExitArena()
        {
            Logger.Log("[PORTAL] Exiting arena...");
            
            // Clean up arena (optional - could leave it)
            // For now we'll just mark as not in arena
            
            // Remove exit portal
            activePortals.RemoveAll(p => p.Type == PortalType.Exit);
            
            isInArena = false;
            currentArena = null;
            
            Logger.Log("[PORTAL] Returned to overworld!");
        }

        /// <summary>
        /// Called when a boss is defeated - spawns exit portal
        /// </summary>
        public void OnBossDefeated()
        {
            if (!isInArena || currentArena == null) return;
            
            Logger.Log("[PORTAL] Boss defeated! Spawning exit portal...");
            
            Vector2 exitPos = currentArena.GetExitPortalWorldPosition(arenaOffset);
            Portal exitPortal = new Portal(exitPos, PortalType.Exit, currentBossType);
            activePortals.Add(exitPortal);
        }

        /// <summary>
        /// Gets the player spawn position when entering arena
        /// </summary>
        public Vector2 GetArenaPlayerSpawn()
        {
            if (currentArena == null) return Vector2.Zero;
            return currentArena.GetPlayerSpawnWorldPosition(arenaOffset);
        }

        /// <summary>
        /// Gets the boss spawn position in arena
        /// </summary>
        public Vector2 GetArenaBossSpawn()
        {
            if (currentArena == null) return Vector2.Zero;
            return currentArena.GetBossSpawnWorldPosition(arenaOffset);
        }

        /// <summary>
        /// Gets the return position for exiting arena
        /// </summary>
        public Vector2 GetReturnPosition()
        {
            return returnPosition;
        }

        /// <summary>
        /// Checks if an altar exists at the given position
        /// </summary>
        public SummonAltar GetAltarAt(Point position)
        {
            return altars.Find(a => a.Position == position);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            // Draw all active portals
            foreach (var portal in activePortals)
            {
                portal.Draw(spriteBatch, pixelTexture);
            }
        }

        /// <summary>
        /// Clear all portals (for game reset/cleanup)
        /// </summary>
        public void ClearAllPortals()
        {
            activePortals.Clear();
            isInArena = false;
            currentArena = null;
        }
    }
}