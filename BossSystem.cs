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
    /// Integrates with PortalSystem for arena-based boss fights
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
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            int spawnDistance = random.Next(15, 21) * tileSize;
            int direction = random.Next(2) == 0 ? -1 : 1;
            float spawnX = playerPosition.X + (spawnDistance * direction);

            // Find the actual ground level at spawn location
            int spawnTileX = (int)(spawnX / tileSize);
            int actualGroundY = groundTileY;

            // Search downward to find solid ground
            for (int y = groundTileY - 10; y < groundTileY + 20; y++)
            {
                if (world.IsSolidAtPosition(spawnTileX, y))
                {
                    actualGroundY = y;
                    break;
                }
            }

            // Spawn troll on top of the ground (subtract troll height to place him on surface)
            float spawnY = actualGroundY * tileSize - 160; // 160 = troll height

            activeTroll = new CaveTroll(new Vector2(spawnX, spawnY), world);
            Logger.Log("[BOSS] Cave Troll summoned! Prepare for battle!");
            Logger.Log($"[BOSS] Health: 100 | Club Swing: 30 DMG | AOE Ripple: 25 DMG | Dash (at 50% HP): 50 DMG");
        }

        public void Update(float deltaTime, Vector2 playerPosition, Player.Player player, Inventory playerInventory, CombatSystem combatSystem, ProjectileSystem projectileSystem, ItemType heldItem)
        {
            if (activeTroll == null) return;

            // Check for player damage from boss attacks
            if (activeTroll.IsAlive)
            {
                // Club swing melee damage (when very close)
                if (activeTroll.GetHitbox().Intersects(player.GetHitbox()))
                {
                    if (activeTroll.IsAttackingAnimation())
                    {
                        player.TakeDamage(CaveTroll.CLUB_SWING_DAMAGE);
                        Logger.Log($"[BOSS] Cave Troll club swing! {CaveTroll.CLUB_SWING_DAMAGE} damage!");
                    }
                }

                // Charge attack damage (50 damage at 50% HP)
                if (activeTroll.IsChargingAttack() && activeTroll.GetHitbox().Intersects(player.GetHitbox()))
                {
                    player.TakeDamage(CaveTroll.DASH_DAMAGE);
                    Logger.Log($"[BOSS] Cave Troll CHARGE HIT! {CaveTroll.DASH_DAMAGE} damage!");
                }

                // Ground ripple AOE damage
                foreach (var ripple in activeTroll.ActiveRipples)
                {
                    if (ripple.IsActive && ripple.GetHitbox().Intersects(player.GetHitbox()))
                    {
                        if (player.IsOnGround())
                        {
                            player.TakeDamage(CaveTroll.AOE_RIPPLE_DAMAGE);
                            Logger.Log($"[BOSS] Ground ripple hit! {CaveTroll.AOE_RIPPLE_DAMAGE} AOE damage!");
                        }
                    }
                }
            }

            // Check for player damaging the boss
            if (activeTroll.IsAlive && activeTroll.CanBeDamaged())
            {
                // Sword damage
                if (combatSystem != null && combatSystem.IsAttacking() && !combatSystem.HasAlreadyHit(activeTroll))
                {
                    Rectangle swordHitbox = combatSystem.GetAttackHitbox(player.Position, Player.Player.PLAYER_WIDTH, Player.Player.PLAYER_HEIGHT);
                    if (swordHitbox.Intersects(activeTroll.GetHitbox()))
                    {
                        float damage = combatSystem.GetAttackDamage(heldItem);
                        activeTroll.TakeDamage(damage);
                        combatSystem.RegisterHit(activeTroll);
                        Logger.Log($"[BOSS] Hit Cave Troll for {damage} damage! HP: {activeTroll.Health}/{activeTroll.MaxHealth}");
                    }
                }

                // Projectile damage
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
                                    Logger.Log($"[BOSS] Laser hit! {laser.Damage} damage! HP: {activeTroll.Health}/{activeTroll.MaxHealth}");
                                    break;
                                }
                            }
                        }
                        else if (!projectile.GetHitbox().IsEmpty && projectile.GetHitbox().Intersects(activeTroll.GetHitbox()))
                        {
                            activeTroll.TakeDamage(projectile.Damage);
                            projectile.OnHit();
                            Logger.Log($"[BOSS] Projectile hit! {projectile.Damage} damage! HP: {activeTroll.Health}/{activeTroll.MaxHealth}");
                        }
                    }
                }
            }

            // Update boss AI
            if (activeTroll.IsAlive)
            {
                activeTroll.Update(deltaTime, playerPosition);
            }
            else if (activeTroll.IsDefeated)
            {
                Logger.Log("[BOSS] CAVE TROLL DEFEATED! Victory!");
                DropLoot(activeTroll.Position, player, playerInventory);
                activeTroll = null;
            }
        }

        private void DropLoot(Vector2 bossPosition, Player.Player player, Inventory playerInventory)
        {
            // Drop 10-20 Troll Bars (guaranteed)
            int barCount = random.Next(10, 21);
            for (int i = 0; i < barCount; i++)
            {
                playerInventory.AddItem(ItemType.TrollBar, 1);
            }
            Logger.Log($"[BOSS] Dropped {barCount} Troll Bars!");

            // 5% chance to drop Troll Club weapon
            if (random.Next(100) < 5)
            {
                playerInventory.AddItem(ItemType.TrollClub, 1);
                Logger.Log("[BOSS] RARE DROP! Troll Club obtained!");
            }

            Logger.Log("[BOSS] Boss fight complete! Check your inventory for rewards.");
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D trollTexture, Texture2D pixelTexture)
        {
            if (activeTroll != null && activeTroll.IsAlive)
            {
                activeTroll.Draw(spriteBatch, trollTexture, pixelTexture);

                // Draw all ground ripples
                foreach (var ripple in activeTroll.ActiveRipples)
                {
                    ripple.Draw(spriteBatch, pixelTexture);
                }
            }
        }
    }
}
