using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Entities;
using StarshroudHollows.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using StarshroudHollows.Player;

namespace StarshroudHollows.Systems
{
    /// <summary>
    /// Manages boss spawning, combat, and loot drops
    /// </summary>
    public class BossSystem
    {
        private StarshroudHollows.World.World world;
        private CaveTroll activeTroll;
        private Random random;

        public bool HasActiveBoss => activeTroll != null && activeTroll.IsAlive;
        public CaveTroll ActiveTroll => activeTroll;

        public BossSystem(StarshroudHollows.World.World world)
        {
            this.world = world;
            random = new Random();
        }

        /// <summary>
        /// Attempts to summon Cave Troll boss
        /// Returns error message if failed, null if successful
        /// </summary>
         public string TrySummonCaveTroll(Vector2 playerPosition)
        {
            if (HasActiveBoss)
            {
                return "A boss is already active!";
            }

            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            int playerTileX = (int)(playerPosition.X / tileSize);
            int playerTileY = (int)(playerPosition.Y / tileSize);

            // Find the ground level below the player
            int groundY = playerTileY;
            for (int y = playerTileY; y < playerTileY + 20; y++)
            {
                if (world.IsSolidAtPosition(playerTileX, y))
                {
                    groundY = y;
                    break;
                }
            }

            // Get surface height to determine if player is on surface or underground
            int surfaceHeight = world.GetSurfaceHeight(playerTileX);
            bool isOnSurface = groundY <= surfaceHeight + 5; // Within 5 tiles of surface = on surface

            // Only enforce arena requirements if underground
            if (!isOnSurface)
            {
                const int REQUIRED_FLAT_WIDTH = 40;
                const int REQUIRED_HEIGHT = 10;

                int leftBound = playerTileX - REQUIRED_FLAT_WIDTH / 2;
                int rightBound = playerTileX + REQUIRED_FLAT_WIDTH / 2;

                // Check for flat ground
                for (int x = leftBound; x <= rightBound; x++)
                {
                    bool foundGround = false;
                    for (int yCheck = groundY - 2; yCheck <= groundY + 2; yCheck++)
                    {
                        if (world.IsSolidAtPosition(x, yCheck))
                        {
                            foundGround = true;
                            break;
                        }
                    }
                    if (!foundGround)
                    {
                        return "Not enough flat ground! The underground area is too cluttered.";
                    }
                }

                // Check for height clearance
                for (int x = leftBound; x <= rightBound; x++)
                {
                    for (int y = groundY - REQUIRED_HEIGHT; y < groundY; y++)
                    {
                        if (world.IsSolidAtPosition(x, y))
                        {
                            return "Not enough ceiling clearance! Carve out more space above.";
                        }
                    }
                }
            }

            // All checks passed - spawn the boss!
            SpawnCaveTroll(playerPosition, groundY);
            return null; // Success
        }

        private void SpawnCaveTroll(Vector2 playerPosition, int groundTileY)
        {
            int spawnDistance = random.Next(15, 21) * StarshroudHollows.World.World.TILE_SIZE;
            int direction = random.Next(2) == 0 ? -1 : 1;
            float spawnX = playerPosition.X + (spawnDistance * direction);
            float spawnY = groundTileY * StarshroudHollows.World.World.TILE_SIZE - 160;

            activeTroll = new CaveTroll(new Vector2(spawnX, spawnY), world);
            Logger.Log("[BOSS] Cave Troll summoned!");
        }

        // Corrected Player type usage
        public void Update(float deltaTime, Vector2 playerPosition, Player.Player player, Inventory playerInventory, CombatSystem combatSystem, ProjectileSystem projectileSystem, ItemType heldItem)
        {
            if (activeTroll == null) return;

            if (activeTroll.IsAlive && activeTroll.CanBeDamaged())
            {
                if (combatSystem != null && combatSystem.IsAttacking() && !combatSystem.HasAlreadyHit(activeTroll))
                {
                    // Corrected constant access
                    Rectangle swordHitbox = combatSystem.GetAttackHitbox(player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT);
                    if (swordHitbox.Intersects(activeTroll.GetHitbox()))
                    {
                        activeTroll.TakeDamage(combatSystem.GetAttackDamage(heldItem));
                        combatSystem.RegisterHit(activeTroll);
                    }
                }

                if (projectileSystem != null)
                {
                    foreach (var projectile in projectileSystem.GetActiveProjectiles().ToList())
                    {
                        if (projectile is RunicLaser laser && !laser.HasHit(activeTroll))
                        {
                            for (float i = 0; i < laser.Length; i += 16)
                            {
                                if (activeTroll.GetHitbox().Contains(laser.StartPosition + laser.Direction * i))
                                {
                                    activeTroll.TakeDamage(laser.Damage);
                                    laser.RegisterHit(activeTroll);
                                    break;
                                }
                            }
                        }
                        else if (!projectile.GetHitbox().IsEmpty && projectile.GetHitbox().Intersects(activeTroll.GetHitbox()))
                        {
                            activeTroll.TakeDamage(projectile.Damage);
                            projectile.OnHit();
                        }
                    }
                }
            }

            if (activeTroll.IsAlive)
            {
                activeTroll.Update(deltaTime, playerPosition);

                // Corrected constant access
                if (activeTroll.GetHitbox().Intersects(player.GetHitbox()))
                {
                    if (activeTroll.IsAlive)
                    {
                        player.TakeDamage(CaveTroll.SLAM_DAMAGE);
                    }
                }

                foreach (var ripple in activeTroll.ActiveRipples)
                {
                    if (ripple.IsActive && ripple.GetHitbox().Intersects(player.GetHitbox()))
                    {
                        if (player.IsOnGround())
                        {
                            player.TakeDamage(CaveTroll.AOE_DAMAGE);
                        }
                    }
                }
            }
            else if (activeTroll.IsDefeated)
            {
                DropLoot(activeTroll.Position, player, playerInventory);
                activeTroll = null;
            }
        }

        // Corrected Player type usage
        private void DropLoot(Vector2 bossPosition, Player.Player player, Inventory playerInventory)
        {
            int barCount = random.Next(10, 20);

            for (int i = 0; i < barCount; i++)
            {
                playerInventory.AddItem(ItemType.TrollBar, 1);
            }

            Logger.Log($"[BOSS] Dropped {barCount} Troll Bars!");

            if (random.Next(100) < 5)
            {
                playerInventory.AddItem(ItemType.TrollClub, 1);
                Logger.Log("[BOSS] Rare drop! Troll Club obtained!");
            }

            Logger.Log("[BOSS] Cave Troll defeated! Victory!");
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D trollTexture, Texture2D pixelTexture)
        {
            if (activeTroll != null && activeTroll.IsAlive)
            {
                activeTroll.Draw(spriteBatch, trollTexture, pixelTexture);

                foreach (var ripple in activeTroll.ActiveRipples)
                {
                    ripple.Draw(spriteBatch, pixelTexture);
                }
            }
        }
    }
}
