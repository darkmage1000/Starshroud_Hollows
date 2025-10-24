using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Enums;
using System;
using StarshroudHollows.Interfaces;

namespace StarshroudHollows.Entities
{
    public class Spider : IDamageable
    {
        public Vector2 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }

        private Vector2 velocity;
        private const float NORMAL_SPEED = 2.5f; // Slightly faster than player on stone
        private const float WEB_SPEED = 4.5f; // Very fast on webs
        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private bool isOnGround = false;
        private bool isOnWeb = false;
        
        private const int WIDTH = 32;
        private const int HEIGHT = 32;

        public const float CONTACT_DAMAGE = 3f; // Weak hit
        public const float POISON_DAMAGE = 1f; // Poison damage per second
        public const float POISON_DURATION = 5f; // 5 seconds of poison
        
        private float damageCooldown = 0f;
        private const float DAMAGE_COOLDOWN_TIME = 1.0f;
        private float hitCooldownTimer = 0f;
        private const float HIT_COOLDOWN_TIME = 0.5f;

        private StarshroudHollows.World.World world;

        public Spider(Vector2 spawnPosition, StarshroudHollows.World.World world)
        {
            Position = spawnPosition;
            this.world = world;
            MaxHealth = 15f; // Moderate health
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

            // Check if standing on spider web
            CheckIfOnWeb();

            // AI Logic - chase player
            Vector2 directionToPlayer = playerPosition - Position;
            float currentSpeed = isOnWeb ? WEB_SPEED : NORMAL_SPEED;
            
            if (Math.Abs(directionToPlayer.X) > 5)
            {
                velocity.X = directionToPlayer.X > 0 ? currentSpeed : -currentSpeed;
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

            ApplyPhysics();
        }

        private void CheckIfOnWeb()
        {
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            int tileX = (int)((Position.X + WIDTH / 2) / tileSize);
            int tileY = (int)((Position.Y + HEIGHT) / tileSize);
            
            var tile = world.GetTile(tileX, tileY);
            isOnWeb = (tile != null && tile.Type == TileType.SpiderWeb);
        }

        private void ApplyPhysics()
        {
            // Horizontal movement
            Vector2 newPosition = new Vector2(Position.X + velocity.X, Position.Y);
            bool blockedHorizontally = CheckCollision(newPosition);
            
            if (!blockedHorizontally)
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

        public void Draw(SpriteBatch spriteBatch, Texture2D spiderSprite, Texture2D pixelTexture)
        {
            if (!IsAlive) return;
            
            // Draw spider sprite (or placeholder if sprite not loaded)
            if (spiderSprite != null)
            {
                spriteBatch.Draw(spiderSprite, GetHitbox(), Color.White);
            }
            else
            {
                // Placeholder: dark purple rectangle
                spriteBatch.Draw(pixelTexture, GetHitbox(), new Color(60, 20, 80));
            }
        }
    }
}
