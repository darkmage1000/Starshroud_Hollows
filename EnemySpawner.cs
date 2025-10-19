using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Entities;
using StarshroudHollows.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
// FIX: Fully qualify the Player class to avoid ambiguity with the Player namespace.
using StarshroudHollows.Player;

namespace StarshroudHollows.Systems
{
    public class EnemySpawner
    {
        private List<Enemy> activeOozes;
        private List<Zombie> activeZombies;
        private Random random;
        private StarshroudHollows.World.World world;

        private const int MAX_OOZES = 2;  // Reduced from 3
        private const int MAX_ZOMBIES_ON_SCREEN = 3;  // Reduced from 5
        private const float SPAWN_INTERVAL_MIN = 30.0f;  // Increased from 15.0f - spawn every 30-60 seconds
        private const float SPAWN_INTERVAL_MAX = 60.0f;  // Increased from 30.0f
        private float oozeSpawnTimer;
        private float nextOozeSpawnTime;

        private float zombieSpawnTimer;
        private const float ZOMBIE_SPAWN_INTERVAL = 45.0f; // Increased from 20.0f - Check for zombie spawns every 45 seconds

        private const int SPAWN_DISTANCE = 200;

        public EnemySpawner(StarshroudHollows.World.World world)
        {
            this.world = world;
            activeOozes = new List<Enemy>();
            activeZombies = new List<Zombie>();
            random = new Random();
            ResetOozeSpawnTimer();
            zombieSpawnTimer = 0f;  // FIXED: Start at 0, count UP to ZOMBIE_SPAWN_INTERVAL

            Systems.Logger.Log("[SPAWNER] EnemySpawner system initialized with Ooze and Zombie support.");
        }

        private void ResetOozeSpawnTimer()
        {
            nextOozeSpawnTime = (float)(random.NextDouble() * (SPAWN_INTERVAL_MAX - SPAWN_INTERVAL_MIN) + SPAWN_INTERVAL_MIN);
            oozeSpawnTimer = 0f; // FIXED: Start at 0, count UP to nextOozeSpawnTime
        }

        public void Update(float deltaTime, Vector2 playerPosition, TimeSystem timeSystem, Rectangle cameraView)
        {
            // Remove dead enemies
            activeOozes.RemoveAll(e => !e.IsAlive);
            activeZombies.RemoveAll(z => !z.IsAlive);

            // Despawn zombies that are too far from player (outside screen + buffer)
            const float ZOMBIE_DESPAWN_DISTANCE = 1500f; // ~47 tiles
            for (int i = activeZombies.Count - 1; i >= 0; i--)
            {
                float distanceToPlayer = Vector2.Distance(activeZombies[i].Position, playerPosition);
                if (distanceToPlayer > ZOMBIE_DESPAWN_DISTANCE)
                {
                    Systems.Logger.Log($"[SPAWNER] Despawned zombie at distance {distanceToPlayer:F0}");
                    activeZombies.RemoveAt(i);
                }
            }

            // Despawn surface zombies at dawn
            if (timeSystem.IsDaytime())
            {
                int despawnedCount = activeZombies.RemoveAll(z => z.IsSurfaceZombie);
                if (despawnedCount > 0)
                {
                    Systems.Logger.Log($"[SPAWNER] Despawned {despawnedCount} surface zombies at dawn");
                }
            }

            // Spawn Oozes during daytime
            if (timeSystem.IsDaytime())
            {
                oozeSpawnTimer += deltaTime;
                if (oozeSpawnTimer >= nextOozeSpawnTime && activeOozes.Count < MAX_OOZES)
                {
                    TrySpawnOoze(playerPosition, cameraView);
                    ResetOozeSpawnTimer();
                }
            }

            // Spawn Zombies
            zombieSpawnTimer += deltaTime;
            if (zombieSpawnTimer >= ZOMBIE_SPAWN_INTERVAL)
            {
                zombieSpawnTimer = 0f;

                int currentZombiesOnScreen = CountZombiesOnScreen(cameraView);
                if (currentZombiesOnScreen < MAX_ZOMBIES_ON_SCREEN)
                {
                    // Try surface spawn at night
                    if (!timeSystem.IsDaytime())
                    {
                        TrySpawnSurfaceZombie(playerPosition, cameraView);
                    }

                    // Try underground spawn (anytime)
                    TrySpawnUndergroundZombie(playerPosition, cameraView);
                }
            }

            // Update all active enemies
            foreach (Enemy ooze in activeOozes)
            {
                ooze.Update(deltaTime, playerPosition);
            }

            foreach (Zombie zombie in activeZombies)
            {
                zombie.Update(deltaTime, playerPosition);
            }
        }

        private void TrySpawnOoze(Vector2 playerPosition, Rectangle cameraView)
        {
            bool spawnLeft = random.Next(0, 2) == 0;
            int spawnX = spawnLeft ? cameraView.Left - SPAWN_DISTANCE : cameraView.Right + SPAWN_DISTANCE;

            int spawnTileX = spawnX / StarshroudHollows.World.World.TILE_SIZE;
            int surfaceY = world.GetSurfaceHeight(spawnTileX);

            if (surfaceY < 5)
            {
                surfaceY = (int)(playerPosition.Y / StarshroudHollows.World.World.TILE_SIZE);
            }

            Vector2 spawnPosition = new Vector2(spawnX, (surfaceY - 2) * StarshroudHollows.World.World.TILE_SIZE);
            Enemy newEnemy = new Enemy(spawnPosition, world);
            activeOozes.Add(newEnemy);

            Systems.Logger.Log($"[SPAWNER] Spawned Ooze at {spawnPosition}. Active: {activeOozes.Count}/{MAX_OOZES}");
        }

        private void TrySpawnSurfaceZombie(Vector2 playerPosition, Rectangle cameraView)
        {
            // Spawn off-screen on surface at night
            bool spawnLeft = random.Next(0, 2) == 0;
            int spawnX = spawnLeft ? cameraView.Left - SPAWN_DISTANCE : cameraView.Right + SPAWN_DISTANCE;

            int spawnTileX = spawnX / StarshroudHollows.World.World.TILE_SIZE;
            int surfaceY = world.GetSurfaceHeight(spawnTileX);

            if (surfaceY < 5)
            {
                return; // Invalid spawn location
            }

            Vector2 spawnPosition = new Vector2(spawnX, (surfaceY - 2) * StarshroudHollows.World.World.TILE_SIZE);
            Zombie newZombie = new Zombie(spawnPosition, world, true);
            activeZombies.Add(newZombie);

            Systems.Logger.Log($"[SPAWNER] Spawned surface Zombie at {spawnPosition}. Active zombies: {activeZombies.Count}");
        }

        private void TrySpawnUndergroundZombie(Vector2 playerPosition, Rectangle cameraView)
        {
            // Try to find a dark underground cave within camera view
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            int attempts = 0;
            const int MAX_ATTEMPTS = 10;

            while (attempts < MAX_ATTEMPTS)
            {
                attempts++;

                // Random position within extended camera view
                int spawnX = random.Next(cameraView.Left - SPAWN_DISTANCE, cameraView.Right + SPAWN_DISTANCE);
                int spawnY = random.Next(cameraView.Top, cameraView.Bottom + SPAWN_DISTANCE);

                int tileX = spawnX / tileSize;
                int tileY = spawnY / tileSize;

                // Check if this is underground (below surface)
                int surfaceY = world.GetSurfaceHeight(tileX);
                if (tileY <= surfaceY + 3) continue; // Must be at least 3 blocks underground

                // Check if there's a 2-tile high opening (enough for zombie)
                bool isValidOpening = true;
                bool hasFloor = false;
                bool hasCeiling = false;

                // Check for floor
                if (world.IsSolidAtPosition(tileX, tileY + 3))
                {
                    hasFloor = true;
                }

                // Check for ceiling
                if (world.IsSolidAtPosition(tileX, tileY - 1))
                {
                    hasCeiling = true;
                }

                // Check if the 2-tile space is clear
                for (int checkY = 0; checkY < 2; checkY++)
                {
                    if (world.IsSolidAtPosition(tileX, tileY + checkY))
                    {
                        isValidOpening = false;
                        break;
                    }
                }

                if (isValidOpening && hasFloor && hasCeiling)
                {
                    // Valid cave spawn!
                    Vector2 spawnPosition = new Vector2(tileX * tileSize, tileY * tileSize);
                    Zombie newZombie = new Zombie(spawnPosition, world, false);
                    activeZombies.Add(newZombie);

                    Systems.Logger.Log($"[SPAWNER] Spawned underground Zombie at {spawnPosition}. Active zombies: {activeZombies.Count}");
                    return;
                }
            }
        }

        private int CountZombiesOnScreen(Rectangle cameraView)
        {
            int count = 0;
            foreach (Zombie zombie in activeZombies)
            {
                if (cameraView.Contains(zombie.Position.ToPoint()))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns ALL active enemies (both Oozes and Zombies) as IDamageable list
        /// This ensures summons, projectiles, and combat systems can target everything
        /// </summary>
        public List<Interfaces.IDamageable> GetActiveEnemies()
        {
            var allEnemies = new List<Interfaces.IDamageable>();
            allEnemies.AddRange(activeOozes.Cast<Interfaces.IDamageable>());
            allEnemies.AddRange(activeZombies.Cast<Interfaces.IDamageable>());
            return allEnemies;
        }

        public List<Zombie> GetActiveZombies()
        {
            return activeZombies;
        }

        public void CheckCombatCollisions(CombatSystem combatSystem, Vector2 playerPosition, int playerWidth, int playerHeight, ItemType heldWeapon, Inventory inventory)
        {
            if (!combatSystem.IsAttacking()) return;

            Rectangle attackHitbox = combatSystem.GetAttackHitbox(playerPosition, playerWidth, playerHeight);

            // Check Ooze collisions
            foreach (Enemy enemy in activeOozes)
            {
                if (!enemy.IsAlive || !enemy.CanBeDamaged()) continue;

                if (attackHitbox.Intersects(enemy.GetHitbox()))
                {
                    float damage = combatSystem.GetAttackDamage(heldWeapon);
                    enemy.TakeDamage(damage);

                    if (!enemy.IsAlive)
                    {
                        inventory.AddItem(ItemType.Slime, 1);
                        Systems.Logger.Log("[SPAWNER] Ooze defeated! Dropped slime.");
                    }
                }
            }

            // Check Zombie collisions
            foreach (Zombie zombie in activeZombies)
            {
                if (!zombie.IsAlive || !zombie.CanBeDamaged()) continue;

                if (attackHitbox.Intersects(zombie.GetHitbox()))
                {
                    float damage = combatSystem.GetAttackDamage(heldWeapon);
                    zombie.TakeDamage(damage);

                    if (!zombie.IsAlive)
                    {
                        // 50% chance to drop Piece of Flesh
                        if (random.Next(0, 2) == 0)
                        {
                            inventory.AddItem(ItemType.PieceOfFlesh, 1);
                            Systems.Logger.Log("[SPAWNER] Zombie defeated! Dropped Piece of Flesh.");
                        }
                        else
                        {
                            Systems.Logger.Log("[SPAWNER] Zombie defeated! No drop.");
                        }
                    }
                }
            }
        }

        public void CheckPlayerCollisions(Vector2 playerPosition, int playerWidth, int playerHeight, StarshroudHollows.Player.Player player)
        {
            Rectangle playerHitbox = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, playerWidth, playerHeight);

            // Check Ooze collisions
            foreach (Enemy enemy in activeOozes)
            {
                if (!enemy.IsAlive || !enemy.CanDamagePlayer()) continue;

                if (playerHitbox.Intersects(enemy.GetHitbox()))
                {
                    player.TakeDamage(Enemy.CONTACT_DAMAGE);
                    enemy.ResetDamageCooldown();
                    Systems.Logger.Log($"[SPAWNER] Player hit by Ooze! Took {Enemy.CONTACT_DAMAGE} damage");
                }
            }

            // Check Zombie collisions
            foreach (Zombie zombie in activeZombies)
            {
                if (!zombie.IsAlive || !zombie.CanDamagePlayer()) continue;

                if (playerHitbox.Intersects(zombie.GetHitbox()))
                {
                    player.TakeDamage(Zombie.CONTACT_DAMAGE);
                    zombie.ResetDamageCooldown();
                    Systems.Logger.Log($"[SPAWNER] Player hit by Zombie! Took {Zombie.CONTACT_DAMAGE} damage");
                }
            }
        }

        public void DrawEnemies(SpriteBatch spriteBatch, Texture2D oozeSprite, Texture2D zombieSprite, Texture2D pixelTexture)
        {
            // Draw Oozes
            foreach (Enemy enemy in activeOozes)
            {
                enemy.Draw(spriteBatch, oozeSprite);

                if (enemy.IsAlive && enemy.Health < enemy.MaxHealth)
                {
                    DrawHealthBar(spriteBatch, pixelTexture, enemy.GetHitbox(), enemy.Health, enemy.MaxHealth);
                }
            }

            // Draw Zombies
            foreach (Zombie zombie in activeZombies)
            {
                zombie.Draw(spriteBatch, zombieSprite, pixelTexture);

                if (zombie.IsAlive && zombie.Health < zombie.MaxHealth)
                {
                    DrawHealthBar(spriteBatch, pixelTexture, zombie.GetHitbox(), zombie.Health, zombie.MaxHealth);
                }
            }
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle hitbox, float health, float maxHealth)
        {
            int healthBarWidth = Math.Max(28, hitbox.Width);
            int healthBarHeight = 4;

            Rectangle healthBarBg = new Rectangle(hitbox.X, hitbox.Y - 8, healthBarWidth, healthBarHeight);
            Rectangle healthBarFill = new Rectangle(hitbox.X, hitbox.Y - 8, (int)(healthBarWidth * (health / maxHealth)), healthBarHeight);

            spriteBatch.Draw(pixelTexture, healthBarBg, Color.Black);
            spriteBatch.Draw(pixelTexture, healthBarFill, Color.Red);
        }
    }
}