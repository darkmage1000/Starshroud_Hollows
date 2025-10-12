using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Enums;
using System;

namespace Claude4_5Terraria.Entities
{
    public class Enemy
    {
        public Vector2 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }

        private Vector2 velocity;
        private const float MOVE_SPEED = 1.5f; // Slow slinky movement
        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private const int WIDTH = 28;
        private const int HEIGHT = 24;

        // Damage
        public const float CONTACT_DAMAGE = 5f;
        private float damageCooldown = 0f;
        private const float DAMAGE_COOLDOWN_TIME = 1.0f; // 1 second between damage to player

        // FIX: Player Hit Cooldown (for anti-multi-hit)
        private float hitCooldownTimer = 0f;
        private const float HIT_COOLDOWN_TIME = 0.5f; // Ensures immunity lasts longer than a 0.3s swing

        // Animation
        private float animationTimer = 0f;
        private const float ANIMATION_SPEED = 0.15f;
        private int currentFrame = 0;
        private const int MAX_FRAMES = 4; // Slinky animation frames

        // AI
        private World.World world;
        private float changeDirectionTimer = 0f;
        private const float DIRECTION_CHANGE_INTERVAL = 2.0f;

        public Enemy(Vector2 spawnPosition, World.World world)
        {
            Position = spawnPosition;
            this.world = world;
            MaxHealth = 3f;
            Health = MaxHealth;
            IsAlive = true;
            velocity = Vector2.Zero;
        }

        // Simpler TakeDamage: Only handles HP reduction and status, does NOT handle cooldown check.
        public void TakeDamage(float damage)
        {
            Health -= damage;

            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
                Systems.Logger.Log($"[ENEMY] Ooze defeated at {Position}");
            }
            else
            {
                Systems.Logger.Log($"[ENEMY] Ooze took {damage} damage, {Health}/{MaxHealth} HP remaining");
            }
        }

        // NEW/FIX: Public method for external systems (ProjectileSystem) to check cooldown
        public bool CanBeDamaged()
        {
            return hitCooldownTimer <= 0;
        }

        // NEW/FIX: Public method for external systems (ProjectileSystem) to reset the cooldown *after* a hit.
        public void ResetHitCooldown()
        {
            hitCooldownTimer = HIT_COOLDOWN_TIME;
        }

        public void Update(float deltaTime, Vector2 playerPosition)
        {
            if (!IsAlive) return;

            // FIX: Update hit cooldown timer
            if (hitCooldownTimer > 0)
            {
                hitCooldownTimer -= deltaTime;
            }

            // Update damage cooldown (for hitting the player)
            if (damageCooldown > 0)
            {
                damageCooldown -= deltaTime;
            }

            // Update animation
            animationTimer += deltaTime;
            if (animationTimer >= ANIMATION_SPEED)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % MAX_FRAMES;
            }

            // AI: Move toward player
            Vector2 directionToPlayer = playerPosition - Position;

            // Only move horizontally toward player
            if (Math.Abs(directionToPlayer.X) > 5) // Dead zone to prevent jittering
            {
                if (directionToPlayer.X > 0)
                {
                    velocity.X = MOVE_SPEED;
                }
                else
                {
                    velocity.X = -MOVE_SPEED;
                }
            }
            else
            {
                velocity.X = 0;
            }

            // Apply gravity
            velocity.Y += GRAVITY;
            if (velocity.Y > MAX_FALL_SPEED)
            {
                velocity.Y = MAX_FALL_SPEED;
            }

            // Apply physics
            ApplyPhysics();
        }

        private void ApplyPhysics()
        {
            // Horizontal movement
            Vector2 newPosition = new Vector2(Position.X + velocity.X, Position.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
            }
            else
            {
                velocity.X = 0;
            }

            // Vertical movement
            newPosition = new Vector2(Position.X, Position.Y + velocity.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
            }
            else
            {
                if (velocity.Y > 0) // Landing
                {
                    int hitTileY = (int)((Position.Y + velocity.Y + HEIGHT) / World.World.TILE_SIZE);
                    Position = new Vector2(Position.X, hitTileY * World.World.TILE_SIZE - HEIGHT);
                }
                velocity.Y = 0;
            }
        }

        private bool CheckCollision(Vector2 position)
        {
            int tileSize = World.World.TILE_SIZE;

            Vector2[] checkPoints = new Vector2[]
            {
                new Vector2(position.X + 2, position.Y + 2),
                new Vector2(position.X + WIDTH - 2, position.Y + 2),
                new Vector2(position.X + 2, position.Y + HEIGHT - 2),
                new Vector2(position.X + WIDTH - 2, position.Y + HEIGHT - 2),
                new Vector2(position.X + WIDTH / 2, position.Y + HEIGHT - 2)
            };

            foreach (Vector2 point in checkPoints)
            {
                int tileX = (int)(point.X / tileSize);
                int tileY = (int)(point.Y / tileSize);

                if (world.IsSolidAtPosition(tileX, tileY))
                {
                    return true;
                }
            }

            return false;
        }

        public Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, WIDTH, HEIGHT);
        }

        public bool CanDamagePlayer()
        {
            return IsAlive && damageCooldown <= 0;
        }

        public void ResetDamageCooldown()
        {
            damageCooldown = DAMAGE_COOLDOWN_TIME;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D enemySprite)
        {
            if (!IsAlive) return;

            // Slinky animation - squash and stretch effect on sprite
            float scaleX = 1.0f;
            float scaleY = 1.0f;

            if (currentFrame == 0 || currentFrame == 2)
            {
                scaleY = 1.1f; // Stretch tall
                scaleX = 0.9f; // Compress width
            }
            else
            {
                scaleY = 0.9f; // Squash short
                scaleX = 1.1f; // Expand width
            }

            // Draw the enemy sprite with animation
            Rectangle destRect = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                (int)(WIDTH * scaleX),
                (int)(HEIGHT * scaleY)
            );

            spriteBatch.Draw(enemySprite, destRect, Color.White);

            // Draw health bar
            if (Health < MaxHealth)
            {
                // Health bar logic is handled in EnemySpawner.DrawEnemies
            }
        }
    }
}