using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Entities;
using Claude4_5Terraria.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Claude4_5Terraria.Systems
{
    // NEW: Enum definition for new projectile types 
    public enum ProjectileType
    {
        MagicBolt
    }

    public abstract class Projectile
    {
        public Vector2 Position { get; set; }
        public bool IsAlive { get; set; }
        public float Damage { get; protected set; }

        // Abstract methods must be implemented by derived classes
        public abstract void Update(float deltaTime);
        public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture);
        public abstract Rectangle GetHitbox();
    }

    // NEW: Magic Bolt Projectile
    public class MagicBolt : Projectile
    {
        private const float SPEED = 600f; // Very fast projectile, e.g., 600 pixels/sec
        private Vector2 velocity;
        private const int WIDTH = 8;
        private const int HEIGHT = 8;
        private float lifeTimer = 0f;
        private const float MAX_LIFE = 2f; // Despawns after 2 seconds

        // MODIFIED: Constructor now takes Vector2 direction
        public MagicBolt(Vector2 position, Vector2 direction, float damage)
        {
            Position = position;
            IsAlive = true;
            Damage = damage;
            // Velocity is calculated from direction * speed
            velocity = direction * SPEED;
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;

            // Move the projectile
            Position += velocity * deltaTime;

            // Check max life duration
            lifeTimer += deltaTime;
            if (lifeTimer >= MAX_LIFE)
            {
                IsAlive = false;
            }
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (IsAlive)
            {
                // Draw as a purple square
                Rectangle destRect = GetHitbox();
                spriteBatch.Draw(pixelTexture, destRect, Color.MediumPurple);
            }
        }
    }

    public class ProjectileSystem
    {
        private List<Projectile> activeProjectiles;
        private World.World world;

        public ProjectileSystem(World.World world)
        {
            this.world = world;
            activeProjectiles = new List<Projectile>();
        }

        // MODIFIED: Launch now takes Vector2 direction instead of bool facingRight
        public void Launch(ProjectileType type, Vector2 position, Vector2 direction, float damage)
        {
            if (type == ProjectileType.MagicBolt)
            {
                activeProjectiles.Add(new MagicBolt(position, direction, damage));
            }
            Systems.Logger.Log($"[PROJECTILE] Launched {type} at {position} with damage {damage}");
        }

        public void Update(float deltaTime, List<Enemy> activeEnemies)
        {
            // Update positions and check enemy collisions
            foreach (var projectile in activeProjectiles.Where(p => p.IsAlive).ToList())
            {
                projectile.Update(deltaTime);

                // Check collision against enemies
                if (activeEnemies != null)
                {
                    foreach (var enemy in activeEnemies.Where(e => e.IsAlive))
                    {
                        if (projectile.GetHitbox().Intersects(enemy.GetHitbox()))
                        {
                            // CRITICAL CHECK: Only apply damage if the enemy is not on cooldown.
                            if (!enemy.CanBeDamaged())
                            {
                                continue;
                            }

                            // Apply damage and mark projectile for removal
                            enemy.TakeDamage(projectile.Damage);

                            // Immediately reset the enemy's hit cooldown
                            enemy.ResetHitCooldown();

                            projectile.IsAlive = false;
                            break;
                        }
                    }
                }
            }

            // Remove inactive projectiles
            activeProjectiles.RemoveAll(p => !p.IsAlive);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            foreach (var projectile in activeProjectiles.Where(p => p.IsAlive))
            {
                projectile.Draw(spriteBatch, pixelTexture);
            }
        }
    }
}