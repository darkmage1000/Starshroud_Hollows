using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Enums;
using System;
using System.Collections.Generic;
using StarshroudHollows.Interfaces;

namespace StarshroudHollows.Entities
{
    public class Worm : IDamageable
    {
        public Vector2 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }

        private Vector2 velocity;
        private const float BURROW_SPEED = 3.5f; // Fast through blocks
        private const float AIR_SPEED = 1.0f; // Slow in air
        private bool isBurrowing = false;
        
        private const int WIDTH = 192; // 6 blocks wide (32 * 6)
        private const int HEIGHT = 32;

        public const float CONTACT_DAMAGE = 4f; // Weak damage
        
        private float damageCooldown = 0f;
        private const float DAMAGE_COOLDOWN_TIME = 1.0f;
        private float hitCooldownTimer = 0f;
        private const float HIT_COOLDOWN_TIME = 0.5f;

        private StarshroudHollows.World.World world;
        
        // Worm AI state
        private enum WormState
        {
            Burrowing,
            Emerging,
            Flying
        }
        private WormState currentState = WormState.Burrowing;
        private float stateTimer = 0f;
        private const float MIN_BURROW_TIME = 3f;
        private const float MAX_BURROW_TIME = 6f;
        private Vector2 targetDirection;
        private Random random; // For random direction picking

        public Worm(Vector2 spawnPosition, StarshroudHollows.World.World world)
        {
            Position = spawnPosition;
            this.world = world;
            MaxHealth = 25f; // Moderate health
            Health = MaxHealth;
            IsAlive = true;
            velocity = Vector2.Zero;
            random = new Random();
            PickNewDirection();
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

            stateTimer -= deltaTime;

            // Update AI state
            switch (currentState)
            {
                case WormState.Burrowing:
                    UpdateBurrowing(deltaTime, playerPosition);
                    break;
                case WormState.Emerging:
                    UpdateEmerging(deltaTime);
                    break;
                case WormState.Flying:
                    UpdateFlying(deltaTime, playerPosition);
                    break;
            }

            // Apply movement
            Position += velocity * deltaTime * 60f; // Scale by 60 for frame-rate independence

            // Clamp position to world bounds
            Position = new Vector2(
                Math.Max(0, Math.Min(Position.X, StarshroudHollows.World.World.WORLD_WIDTH * StarshroudHollows.World.World.TILE_SIZE - WIDTH)),
                Math.Max(0, Math.Min(Position.Y, StarshroudHollows.World.World.WORLD_HEIGHT * StarshroudHollows.World.World.TILE_SIZE - HEIGHT))
            );
        }

        private void UpdateBurrowing(float deltaTime, Vector2 playerPosition)
        {
            isBurrowing = true;
            
            // Move toward player while burrowing through blocks
            Vector2 directionToPlayer = playerPosition - Position;
            directionToPlayer.Normalize();
            
            velocity = directionToPlayer * BURROW_SPEED;

            // Check if we've hit air/open space
            if (IsInOpenAir())
            {
                currentState = WormState.Emerging;
                stateTimer = 1f; // 1 second emerge time
            }
            // If timer runs out, emerge anyway
            else if (stateTimer <= 0)
            {
                currentState = WormState.Emerging;
                stateTimer = 1f;
            }
        }

        private void UpdateEmerging(float deltaTime)
        {
            isBurrowing = false;
            
            // Slow down while emerging
            velocity *= 0.9f;

            if (stateTimer <= 0)
            {
                currentState = WormState.Flying;
                stateTimer = 2f; // 2 seconds in air before re-burrowing
            }
        }

        private void UpdateFlying(float deltaTime, Vector2 playerPosition)
        {
            isBurrowing = false;
            
            // Float slowly in air toward player
            Vector2 directionToPlayer = playerPosition - Position;
            directionToPlayer.Normalize();
            
            velocity = directionToPlayer * AIR_SPEED;

            // Check if we can burrow again (hit solid ground)
            if (!IsInOpenAir() || stateTimer <= 0)
            {
                currentState = WormState.Burrowing;
                stateTimer = (float)(random.NextDouble() * (MAX_BURROW_TIME - MIN_BURROW_TIME)) + MIN_BURROW_TIME;
                PickNewDirection();
            }
        }

        private bool IsInOpenAir()
        {
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            // Check center of worm
            int tileX = (int)((Position.X + WIDTH / 2) / tileSize);
            int tileY = (int)((Position.Y + HEIGHT / 2) / tileSize);
            
            var tile = world.GetTile(tileX, tileY);
            return tile == null || !tile.IsActive;
        }

        private void PickNewDirection()
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            targetDirection = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
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

        public void Draw(SpriteBatch spriteBatch, Texture2D wormSprite, Texture2D pixelTexture)
        {
            if (!IsAlive) return;
            
            // Draw worm sprite (or placeholder if sprite not loaded)
            if (wormSprite != null)
            {
                spriteBatch.Draw(wormSprite, GetHitbox(), Color.White);
            }
            else
            {
                // Placeholder: brown/tan segmented worm
                Rectangle hitbox = GetHitbox();
                // Draw multiple segments
                int segmentWidth = 32;
                for (int i = 0; i < 6; i++)
                {
                    Rectangle segment = new Rectangle(
                        hitbox.X + (i * segmentWidth),
                        hitbox.Y,
                        segmentWidth,
                        HEIGHT
                    );
                    Color segmentColor = i % 2 == 0 ? new Color(139, 90, 43) : new Color(160, 110, 60);
                    spriteBatch.Draw(pixelTexture, segment, segmentColor);
                }
            }
        }
    }
}
