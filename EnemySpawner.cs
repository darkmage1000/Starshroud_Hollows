using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Claude4_5Terraria.Systems
{
    public class EnemySpawner
    {
        private List<Enemy> activeEnemies;
        private Random random;
        private World.World world;

        private const int MAX_ENEMIES = 3;
        private const float SPAWN_INTERVAL_MIN = 5.0f; // 5 seconds minimum between spawns
        private const float SPAWN_INTERVAL_MAX = 10.0f; // 10 seconds maximum
        private float spawnTimer;
        private float nextSpawnTime;

        // FINAL: Spawn 200 pixels off screen
        private const int SPAWN_DISTANCE = 200;

        public EnemySpawner(World.World world)
        {
            this.world = world;
            activeEnemies = new List<Enemy>();
            random = new Random();
            ResetSpawnTimer();

            Systems.Logger.Log("[SPAWNER] EnemySpawner system initialized.");

            // Re-prime the timer for the first spawn immediately (as originally intended)
            spawnTimer = nextSpawnTime;
        }

        private void ResetSpawnTimer()
        {
            nextSpawnTime = (float)(random.NextDouble() * (SPAWN_INTERVAL_MAX - SPAWN_INTERVAL_MIN) + SPAWN_INTERVAL_MIN);
            spawnTimer = 0f;
        }

        public void Update(float deltaTime, Vector2 playerPosition, TimeSystem timeSystem, Rectangle cameraView)
        {
            // Only spawn during daytime
            if (!timeSystem.IsDaytime())
            {
                return;
            }

            // FIX: Restore enemy removal logic
            activeEnemies.RemoveAll(e => !e.IsAlive);

            // Update spawn timer
            spawnTimer += deltaTime;

            // Try to spawn if conditions are met
            if (spawnTimer >= nextSpawnTime && activeEnemies.Count < MAX_ENEMIES)
            {
                TrySpawnEnemy(playerPosition, cameraView);
                ResetSpawnTimer();
            }

            // Update all active enemies
            foreach (Enemy enemy in activeEnemies)
            {
                enemy.Update(deltaTime, playerPosition);
            }
        }

        private void TrySpawnEnemy(Vector2 playerPosition, Rectangle cameraView)
        {
            // Restoring original spawn logic: off-screen and onto the surface

            // Randomly choose to spawn left or right of screen
            bool spawnLeft = random.Next(0, 2) == 0;

            int spawnX;
            if (spawnLeft)
            {
                spawnX = cameraView.Left - SPAWN_DISTANCE;
            }
            else
            {
                spawnX = cameraView.Right + SPAWN_DISTANCE;
            }

            // Find ground level at spawn position
            int spawnTileX = spawnX / World.World.TILE_SIZE;
            int surfaceY = world.GetSurfaceHeight(spawnTileX);

            // Fallback for surface height and spawning 2 tiles above the surface to fall
            if (surfaceY < 5)
            {
                // Fallback to the player's height if off-screen lookup fails
                surfaceY = (int)(playerPosition.Y / World.World.TILE_SIZE);
            }

            // Spawn above ground (2 tiles above surfaceY)
            Vector2 spawnPosition = new Vector2(spawnX, (surfaceY - 2) * World.World.TILE_SIZE);

            Enemy newEnemy = new Enemy(spawnPosition, world);
            activeEnemies.Add(newEnemy);

            Systems.Logger.Log($"[SPAWNER] SPAWNED: Ooze at {spawnPosition}. Active enemies: {activeEnemies.Count}/{MAX_ENEMIES}");
        }

        public List<Enemy> GetActiveEnemies()
        {
            return activeEnemies;
        }

        public void CheckCombatCollisions(CombatSystem combatSystem, Vector2 playerPosition, int playerWidth, int playerHeight, ItemType heldWeapon, Inventory inventory)
        {
            if (!combatSystem.IsAttacking()) return;

            Rectangle attackHitbox = combatSystem.GetAttackHitbox(playerPosition, playerWidth, playerHeight);

            foreach (Enemy enemy in activeEnemies)
            {
                if (!enemy.IsAlive) continue;

                Rectangle enemyHitbox = enemy.GetHitbox();

                if (attackHitbox.Intersects(enemyHitbox))
                {
                    // FIX: CRITICAL CHECK HERE. Only apply damage if the enemy is NOT on hit cooldown.
                    if (!enemy.CanBeDamaged())
                    {
                        continue;
                    }

                    float damage = combatSystem.GetAttackDamage(heldWeapon);
                    enemy.TakeDamage(damage);

                    // Drop slime on death
                    if (!enemy.IsAlive)
                    {
                        inventory.AddItem(ItemType.Slime, 1);
                        Systems.Logger.Log("[SPAWNER] Ooze defeated! Dropped slime.");
                    }
                }
            }
        }

        public void CheckPlayerCollisions(Vector2 playerPosition, int playerWidth, int playerHeight, Claude4_5Terraria.Player.Player player)
        {
            Rectangle playerHitbox = new Rectangle(
                (int)playerPosition.X,
                (int)playerPosition.Y,
                playerWidth,
                playerHeight
            );

            foreach (Enemy enemy in activeEnemies)
            {
                if (!enemy.IsAlive) continue;
                if (!enemy.CanDamagePlayer()) continue;

                Rectangle enemyHitbox = enemy.GetHitbox();

                if (playerHitbox.Intersects(enemyHitbox))
                {
                    player.TakeDamage(Enemy.CONTACT_DAMAGE);
                    enemy.ResetDamageCooldown();
                    Systems.Logger.Log($"[SPAWNER] Player hit by Ooze! Took {Enemy.CONTACT_DAMAGE} damage");
                }
            }
        }

        public void DrawEnemies(SpriteBatch spriteBatch, Texture2D enemySprite, Texture2D pixelTexture)
        {
            // FIX: Restore drawing of actual enemy sprite and health bar logic

            foreach (Enemy enemy in activeEnemies)
            {
                enemy.Draw(spriteBatch, enemySprite);

                // Draw health bar using pixel texture
                if (enemy.IsAlive && enemy.Health < enemy.MaxHealth)
                {
                    Rectangle enemyHitbox = enemy.GetHitbox();
                    int healthBarWidth = 28;
                    int healthBarHeight = 4;

                    Rectangle healthBarBg = new Rectangle(
                        enemyHitbox.X,
                        enemyHitbox.Y - 8,
                        healthBarWidth,
                        healthBarHeight
                    );

                    Rectangle healthBarFill = new Rectangle(
                        enemyHitbox.X,
                        enemyHitbox.Y - 8,
                        (int)(healthBarWidth * (enemy.Health / enemy.MaxHealth)),
                        healthBarHeight
                    );

                    spriteBatch.Draw(pixelTexture, healthBarBg, Color.Black);
                    spriteBatch.Draw(pixelTexture, healthBarFill, Color.Red);
                }
            }
        }
    }
}