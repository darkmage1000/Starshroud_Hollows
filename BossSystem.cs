using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Player;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Claude4_5Terraria.Systems
{
    /// <summary>
    /// Manages boss spawning, combat, and loot drops
    /// </summary>
    public class BossSystem
    {
        private World.World world;
        private CaveTroll activeTroll;
        private Random random;

        public bool HasActiveBoss => activeTroll != null && activeTroll.IsAlive;
        public CaveTroll ActiveTroll => activeTroll;

        public BossSystem(World.World world)
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

            int tileSize = World.World.TILE_SIZE;
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

            // --- NEW LOGIC ---
            // Define underground as being below a certain depth (e.g., 100 tiles)
            bool isUnderground = groundY >= 100;

            // If the player is underground, enforce arena requirements.
            // If they are on the surface, these checks are skipped.
            if (isUnderground)
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
            int spawnDistance = random.Next(15, 21) * World.World.TILE_SIZE;
            int direction = random.Next(2) == 0 ? -1 : 1;
            float spawnX = playerPosition.X + (spawnDistance * direction);
            float spawnY = groundTileY * World.World.TILE_SIZE - 160;

            activeTroll = new CaveTroll(new Vector2(spawnX, spawnY), world);
            Logger.Log("[BOSS] Cave Troll summoned!");
        }

        public void Update(float deltaTime, Vector2 playerPosition, Player.Player player, Inventory playerInventory, CombatSystem combatSystem, ProjectileSystem projectileSystem, ItemType heldItem)
        {
            if (activeTroll == null) return;

            if (activeTroll.IsAlive && activeTroll.CanBeDamaged())
            {
                if (combatSystem != null && combatSystem.IsAttacking() && !combatSystem.HasAlreadyHit(activeTroll))
                {
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