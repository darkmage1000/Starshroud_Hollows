using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Enums;
using System;
using Claude4_5Terraria.Interfaces;

namespace Claude4_5Terraria.Entities
{
    /// <summary>
    /// Zombie - Slower but smarter enemy with better pathfinding and jumping ability
    /// Spawns at night on surface and underground in dark areas
    /// </summary>
    public class Zombie : IDamageable
    {
        public Vector2 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }
        public bool IsSurfaceZombie { get; set; } // Tracked for despawning at dawn

        private Vector2 velocity;
        private const float MOVE_SPEED = 1.0f; // Slower than Ooze
        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private const float JUMP_FORCE = -12f; // Same as player
        private bool isOnGround = false;
        private float jumpCooldown = 0f;
        private const float JUMP_COOLDOWN_TIME = 0.5f;
        
        private const int WIDTH = 32;
        private const int HEIGHT = 62; // Same as player height

        public const float CONTACT_DAMAGE = 5f;
        private float damageCooldown = 0f;
        private const float DAMAGE_COOLDOWN_TIME = 1.0f;

        private float hitCooldownTimer = 0f;
        private const float HIT_COOLDOWN_TIME = 0.5f;

        // Better AI tracking
        private float pathfindingTimer = 0f;
        private const float PATHFINDING_INTERVAL = 0.5f; // Re-evaluate path every 0.5 seconds
        private bool shouldJump = false;
        private float stuckTimer = 0f;
        private const float STUCK_THRESHOLD = 2.0f; // If stuck for 2 seconds, try alternative
        private Vector2 lastPosition;

        private World.World world;

        public Zombie(Vector2 spawnPosition, World.World world, bool isSurfaceSpawn = false)
        {
            Position = spawnPosition;
            lastPosition = spawnPosition;
            this.world = world;
            MaxHealth = 10f; // Health set to 10 for balanced early game combat
            Health = MaxHealth;
            IsAlive = true;
            IsSurfaceZombie = isSurfaceSpawn;
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

            pathfindingTimer += deltaTime;

            // Check if stuck
            if (Vector2.Distance(Position, lastPosition) < 2f)
            {
                stuckTimer += deltaTime;
            }
            else
            {
                stuckTimer = 0f;
                lastPosition = Position;
            }

            // Better AI pathfinding logic
            if (pathfindingTimer >= PATHFINDING_INTERVAL || stuckTimer > STUCK_THRESHOLD)
            {
                pathfindingTimer = 0f;
                EvaluatePathToPlayer(playerPosition);
            }

            // AI Logic - move toward player
            Vector2 directionToPlayer = playerPosition - Position;
            float distanceToPlayer = directionToPlayer.Length();

            if (distanceToPlayer > 5)
            {
                velocity.X = directionToPlayer.X > 0 ? MOVE_SPEED : -MOVE_SPEED;
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

            // Execute jump if pathfinding determined we should
            if (shouldJump && isOnGround && jumpCooldown <= 0)
            {
                velocity.Y = JUMP_FORCE;
                jumpCooldown = JUMP_COOLDOWN_TIME;
                isOnGround = false;
                shouldJump = false;
            }

            ApplyPhysics();
        }

        /// <summary>
        /// Intelligent pathfinding - checks if there's a clear path or obstacle to jump over
        /// </summary>
        private void EvaluatePathToPlayer(Vector2 playerPosition)
        {
            shouldJump = false;

            Vector2 directionToPlayer = playerPosition - Position;
            if (Math.Abs(directionToPlayer.X) < 10) return; // Too close, don't need pathfinding

            int tileSize = World.World.TILE_SIZE;
            int horizontalDir = directionToPlayer.X > 0 ? 1 : -1;
            
            // Check 1-2 tiles ahead
            int checkX = (int)((Position.X + WIDTH / 2 + (horizontalDir * tileSize * 1.5f)) / tileSize);
            int currentY = (int)((Position.Y + HEIGHT) / tileSize);

            // Check if there's an obstacle ahead
            bool obstacleFound = false;
            int obstacleHeight = 0;

            for (int i = 0; i < 4; i++) // Check up to 3 blocks high
            {
                if (world.IsSolidAtPosition(checkX, currentY - i))
                {
                    obstacleFound = true;
                    obstacleHeight++;
                }
                else
                {
                    break; // Found air
                }
            }

            // If there's an obstacle 1-3 blocks high, mark for jumping
            if (obstacleFound && obstacleHeight > 0 && obstacleHeight <= 3)
            {
                shouldJump = true;
            }

            // Check if there's a pit ahead (and maybe we should NOT move forward)
            // This prevents zombies from walking off cliffs mindlessly
            int pitCheckX = (int)((Position.X + WIDTH / 2 + (horizontalDir * tileSize * 0.5f)) / tileSize);
            bool groundAhead = world.IsSolidAtPosition(pitCheckX, currentY);
            bool groundBelowAhead = world.IsSolidAtPosition(pitCheckX, currentY + 1);

            // If no ground ahead and we're on ground, we're at an edge
            // Still move forward but be aware (zombies aren't afraid of heights)
        }

        private void ApplyPhysics()
        {
            // Try horizontal movement
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
                    int hitTileY = (int)((Position.Y + velocity.Y + HEIGHT) / World.World.TILE_SIZE);
                    Position = new Vector2(Position.X, hitTileY * World.World.TILE_SIZE - HEIGHT);
                    isOnGround = true;
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

        public void Draw(SpriteBatch spriteBatch, Texture2D zombieSprite, Texture2D fallbackTexture)
        {
            if (!IsAlive) return;

            if (zombieSprite != null)
            {
                spriteBatch.Draw(zombieSprite, GetHitbox(), Color.White);
            }
            else
            {
                // Fallback: Draw as dark green rectangle
                spriteBatch.Draw(fallbackTexture, GetHitbox(), Color.DarkGreen);
            }
        }
    }
}
