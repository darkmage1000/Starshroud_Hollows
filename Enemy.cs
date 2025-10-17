using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Enums;
using System;
using StarshroudHollows.Interfaces;

namespace StarshroudHollows.Entities
{
    public class Enemy : IDamageable
    {
        public Vector2 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }

        private Vector2 velocity;
        private const float MOVE_SPEED = 1.5f;
        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private const float JUMP_FORCE = -8f; // Jump strength
        private bool isOnGround = false;
        private float jumpCooldown = 0f;
        private const float JUMP_COOLDOWN_TIME = 0.5f; // Prevent spamming jumps
        // CHANGED: Updated dimensions for the new sprite
        private const int WIDTH = 42;
        private const int HEIGHT = 48;

        public const float CONTACT_DAMAGE = 5f;
        private float damageCooldown = 0f;
        private const float DAMAGE_COOLDOWN_TIME = 1.0f;

        private float hitCooldownTimer = 0f;
        private const float HIT_COOLDOWN_TIME = 0.5f;

        // NOTE: Animation fields are no longer needed for the new static sprite
        // private float animationTimer = 0f;
        // private int currentFrame = 0;

        private StarshroudHollows.World.World world;

        public Enemy(Vector2 spawnPosition, StarshroudHollows.World.World world)
        {
            Position = spawnPosition;
            this.world = world;
            MaxHealth = 3f;
            Health = MaxHealth;
            IsAlive = true;
            velocity = Vector2.Zero;
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
            }
        }

        public bool CanBeDamaged()
        {
            return hitCooldownTimer <= 0;
        }

        public void ResetHitCooldown()
        {
            hitCooldownTimer = HIT_COOLDOWN_TIME;
        }

        public void Update(float deltaTime, Vector2 playerPosition)
        {
            if (!IsAlive) return;

            if (hitCooldownTimer > 0)
            {
                hitCooldownTimer -= deltaTime;
            }

            if (damageCooldown > 0)
            {
                damageCooldown -= deltaTime;
            }
            
            if (jumpCooldown > 0)
            {
                jumpCooldown -= deltaTime;
            }

            // AI Logic
            Vector2 directionToPlayer = playerPosition - Position;
            if (Math.Abs(directionToPlayer.X) > 5)
            {
                velocity.X = directionToPlayer.X > 0 ? MOVE_SPEED : -MOVE_SPEED;
            }
            else
            {
                velocity.X = 0;
            }

            velocity.Y += GRAVITY;
            if (velocity.Y > MAX_FALL_SPEED)
            {
                velocity.Y = MAX_FALL_SPEED;
            }

            ApplyPhysics();
        }

        private void ApplyPhysics()
        {
            // Try horizontal movement first
            Vector2 newPosition = new Vector2(Position.X + velocity.X, Position.Y);
            bool blockedHorizontally = CheckCollision(newPosition);
            
            if (!blockedHorizontally)
            {
                Position = newPosition;
            }
            else
            {
                // We hit a wall - check if we can jump over it
                if (isOnGround && jumpCooldown <= 0 && velocity.X != 0)
                {
                    // Check if there's a wall ahead and how high it is (up to 2 blocks)
                    int tileSize = StarshroudHollows.World.World.TILE_SIZE;
                    int checkX = velocity.X > 0 ? (int)((Position.X + WIDTH + 2) / tileSize) : (int)((Position.X - 2) / tileSize);
                    int currentY = (int)((Position.Y + HEIGHT) / tileSize);
                    
                    // Check how many blocks high the wall is
                    int wallHeight = 0;
                    for (int i = 0; i < 3; i++) // Check up to 2 blocks (3 tiles total)
                    {
                        if (world.IsSolidAtPosition(checkX, currentY - i))
                        {
                            wallHeight++;
                        }
                        else
                        {
                            break; // Found air, wall stops here
                        }
                    }
                    
                    // If wall is 1-2 blocks high, jump!
                    if (wallHeight > 0 && wallHeight <= 2)
                    {
                        velocity.Y = JUMP_FORCE;
                        jumpCooldown = JUMP_COOLDOWN_TIME;
                        isOnGround = false;
                    }
                }
                
                velocity.X = 0;
            }

            // Vertical movement
            newPosition = new Vector2(Position.X, Position.Y + velocity.Y);
            if (!CheckCollision(newPosition))
            {
                Position = newPosition;
                isOnGround = false;
            }
            else
            {
                if (velocity.Y > 0)
                {
                    // Landing on ground
                    int hitTileY = (int)((Position.Y + velocity.Y + HEIGHT) / StarshroudHollows.World.World.TILE_SIZE);
                    Position = new Vector2(Position.X, hitTileY * StarshroudHollows.World.World.TILE_SIZE - HEIGHT);
                    isOnGround = true;
                }
                else if (velocity.Y < 0)
                {
                    // Hit ceiling
                    isOnGround = false;
                }
                velocity.Y = 0;
            }
        }

        private bool CheckCollision(Vector2 position)
        {
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
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

        // CHANGED: Simplified Draw method for the new static sprite
        public void Draw(SpriteBatch spriteBatch, Texture2D enemySprite)
        {
            if (!IsAlive) return;
            spriteBatch.Draw(enemySprite, GetHitbox(), Color.White);
        }
    }
}