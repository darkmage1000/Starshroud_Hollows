using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Claude4_5Terraria.Interfaces;

namespace Claude4_5Terraria.Entities
{
    /// <summary>
    /// Cave Troll Boss - Giant slow boss with club attacks
    /// Summons with Troll Bait item, requires flat arena space
    /// </summary>
    public class CaveTroll : IDamageable
    {
        public Vector2 Position { get; set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsAlive { get; private set; }
        public bool IsDefeated { get; private set; }

        private Vector2 velocity;
        private const float MOVE_SPEED = 0.8f; // Very slow movement
        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private bool isOnGround = false;

        // Boss dimensions - GIANT
        private const int WIDTH = 80;  // 5 tiles wide
        private const int HEIGHT = 160; // 10 tiles tall

        // Attack properties
        public const float SLAM_DAMAGE = 20f;
        public const float AOE_DAMAGE = 10f;
        private const float SLAM_COOLDOWN = 3.0f;
        private float slamTimer = 0f;
        private bool isAttacking = false;
        private float attackAnimationTimer = 0f;
        private const float ATTACK_DURATION = 1.0f;

        // Charge attack (under 50% HP)
        private const float CHARGE_SPEED = 4.0f;
        private const float CHARGE_COOLDOWN = 30.0f;
        private float chargeCooldownTimer = 0f;
        private bool isCharging = false;
        private float chargeDistance = 0f;
        private const float CHARGE_MAX_DISTANCE = 160f; // 10 blocks
        private int chargeDirection = 0;

        // Ground ripple AOE tracking
        public List<GroundRipple> ActiveRipples { get; private set; }

        private float hitCooldownTimer = 0f;
        private const float HIT_COOLDOWN_TIME = 0.5f;

        private World.World world;

        public CaveTroll(Vector2 spawnPosition, World.World world)
        {
            Position = spawnPosition;
            this.world = world;
            MaxHealth = 50f;
            Health = MaxHealth;
            IsAlive = true;
            IsDefeated = false;
            velocity = Vector2.Zero;
            ActiveRipples = new List<GroundRipple>();
            slamTimer = 1.0f; // Allow the first attack to happen quickly
            chargeCooldownTimer = CHARGE_COOLDOWN; // Start on cooldown
        }

        public void TakeDamage(float damage)
        {
            if (!CanBeDamaged()) return;

            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                IsAlive = false;
                IsDefeated = true;
            }
            ResetHitCooldown();
        }

        public bool CanBeDamaged()
        {
            return hitCooldownTimer <= 0 && IsAlive;
        }

        public void ResetHitCooldown()
        {
            hitCooldownTimer = HIT_COOLDOWN_TIME;
        }

        public void Update(float deltaTime, Vector2 playerPosition)
        {
            if (!IsAlive) return;

            // Update timers
            if (hitCooldownTimer > 0)
                hitCooldownTimer -= deltaTime;

            if (slamTimer > 0)
                slamTimer -= deltaTime;

            if (chargeCooldownTimer > 0)
                chargeCooldownTimer -= deltaTime;

            // Update attack animation
            if (isAttacking)
            {
                attackAnimationTimer += deltaTime;
                if (attackAnimationTimer >= ATTACK_DURATION)
                {
                    isAttacking = false;
                    attackAnimationTimer = 0f;
                }
                // Don't move during attack
                velocity.X = 0;
            }
            // Handle charge attack
            else if (isCharging)
            {
                HandleChargeAttack(deltaTime);
            }
            // Normal AI behavior
            else
            {
                // Check if we should do charge attack (under 50% HP and cooldown ready)
                if (Health <= MaxHealth / 2f && chargeCooldownTimer <= 0 && isOnGround)
                {
                    StartChargeAttack(playerPosition);
                }
                // Normal movement and slam attack
                else
                {
                    HandleNormalBehavior(deltaTime, playerPosition);
                }
            }

            // Apply gravity
            velocity.Y += GRAVITY;
            if (velocity.Y > MAX_FALL_SPEED)
            {
                velocity.Y = MAX_FALL_SPEED;
            }

            ApplyPhysics();

            // Update ground ripples
            for (int i = ActiveRipples.Count - 1; i >= 0; i--)
            {
                ActiveRipples[i].Update(deltaTime);
                if (!ActiveRipples[i].IsActive)
                {
                    ActiveRipples.RemoveAt(i);
                }
            }
        }

        private void HandleNormalBehavior(float deltaTime, Vector2 playerPosition)
        {
            // Get the center of the troll for more accurate distance checking
            Vector2 trollCenter = new Vector2(Position.X + WIDTH / 2, Position.Y + HEIGHT / 2);
            Vector2 directionToPlayer = playerPosition - trollCenter;
            float distanceToPlayer = Math.Abs(directionToPlayer.X);

            // The troll will stop and attack if the player is within this range.
            // (Half the troll's width + 1.5 tiles of reach)
            const float ATTACK_RANGE = (WIDTH / 2) + 48f;

            if (distanceToPlayer > ATTACK_RANGE)
            {
                // Player is out of range, move towards them
                velocity.X = directionToPlayer.X > 0 ? MOVE_SPEED : -MOVE_SPEED;
            }
            else
            {
                // Player is in range, stop moving and try to attack
                velocity.X = 0;
                if (slamTimer <= 0 && isOnGround && !isAttacking)
                {
                    PerformSlamAttack();
                }
            }
        }

        private void StartChargeAttack(Vector2 playerPosition)
        {
            isCharging = true;
            chargeDistance = 0f;
            chargeDirection = (playerPosition.X > Position.X) ? 1 : -1;
            velocity.X = chargeDirection * CHARGE_SPEED;
            chargeCooldownTimer = CHARGE_COOLDOWN;
        }

        private void HandleChargeAttack(float deltaTime)
        {
            chargeDistance += Math.Abs(velocity.X);
            
            // End charge after max distance
            if (chargeDistance >= CHARGE_MAX_DISTANCE)
            {
                isCharging = false;
                velocity.X = 0;
                chargeDistance = 0f;
            }
        }

        private void PerformSlamAttack()
        {
            isAttacking = true;
            attackAnimationTimer = 0f;
            slamTimer = SLAM_COOLDOWN;

            // Create ground ripples going left and right
            float rippleY = Position.Y + HEIGHT; // Ground level
            CreateGroundRipple(Position.X, rippleY, -1); // Left
            CreateGroundRipple(Position.X + WIDTH, rippleY, 1); // Right
        }

        private void CreateGroundRipple(float startX, float startY, int direction)
        {
            ActiveRipples.Add(new GroundRipple(startX, startY, direction, world));
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
                // End charge if we hit a wall
                if (isCharging)
                {
                    isCharging = false;
                    chargeDistance = 0f;
                }
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
            // Check multiple points due to large size
            List<Vector2> checkPoints = new List<Vector2>
            {
                new Vector2(position.X + 5, position.Y + 5),
                new Vector2(position.X + WIDTH - 5, position.Y + 5),
                new Vector2(position.X + 5, position.Y + HEIGHT - 5),
                new Vector2(position.X + WIDTH - 5, position.Y + HEIGHT - 5),
                new Vector2(position.X + WIDTH / 2, position.Y + HEIGHT - 5),
                new Vector2(position.X + WIDTH / 4, position.Y + HEIGHT / 2),
                new Vector2(position.X + WIDTH * 3 / 4, position.Y + HEIGHT / 2)
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

        public bool IsAttackingAnimation()
        {
            return isAttacking;
        }

        public float GetAttackProgress()
        {
            return attackAnimationTimer / ATTACK_DURATION;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D trollTexture, Texture2D fallbackTexture)
        {
            if (!IsAlive) return;

            Color drawColor = Color.White;
            
            // Flash red when hit
            if (hitCooldownTimer > 0)
            {
                drawColor = Color.Red;
            }

            if (trollTexture != null)
            {
                spriteBatch.Draw(trollTexture, GetHitbox(), drawColor);
            }
            else
            {
                // Fallback: Draw as dark brown rectangle
                spriteBatch.Draw(fallbackTexture, GetHitbox(), Color.SaddleBrown * 0.8f);
            }

            // Draw health bar above boss
            DrawHealthBar(spriteBatch, fallbackTexture);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            int barWidth = 100;
            int barHeight = 8;
            int barX = (int)(Position.X + WIDTH / 2 - barWidth / 2);
            int barY = (int)(Position.Y - 20);

            // Background
            spriteBatch.Draw(pixel, new Rectangle(barX, barY, barWidth, barHeight), Color.Black * 0.7f);
            
            // Health
            float healthPercent = Health / MaxHealth;
            int healthWidth = (int)(barWidth * healthPercent);
            Color healthColor = healthPercent > 0.5f ? Color.Green : (healthPercent > 0.25f ? Color.Yellow : Color.Red);
            spriteBatch.Draw(pixel, new Rectangle(barX, barY, healthWidth, barHeight), healthColor);
        }
    }

    /// <summary>
    /// Ground ripple effect from troll slam attack
    /// </summary>
    public class GroundRipple
    {
        public Vector2 Position { get; private set; }
        public bool IsActive { get; private set; }
        public int Direction { get; private set; } // -1 for left, 1 for right
        
        private const float RIPPLE_SPEED = 3.0f;
        private const float RIPPLE_LIFETIME = 3.0f;
        private float lifetime;
        private World.World world;

        private const int RIPPLE_WIDTH = 16;
        private const int RIPPLE_HEIGHT = 16;

        public GroundRipple(float x, float y, int direction, World.World world)
        {
            Position = new Vector2(x, y);
            Direction = direction;
            this.world = world;
            IsActive = true;
            lifetime = RIPPLE_LIFETIME;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            lifetime -= deltaTime;
            if (lifetime <= 0)
            {
                IsActive = false;
                return;
            }

            // Move ripple along ground
            float moveAmount = RIPPLE_SPEED * Direction;
            Vector2 newPos = new Vector2(Position.X + moveAmount, Position.Y);

            // Check if still on solid ground
            int tileX = (int)(newPos.X / World.World.TILE_SIZE);
            int tileY = (int)((newPos.Y + RIPPLE_HEIGHT) / World.World.TILE_SIZE);
            
            // Stop if hit a wall or cliff
            bool groundBelow = world.IsSolidAtPosition(tileX, tileY);
            bool wallAhead = world.IsSolidAtPosition(tileX, tileY - 1);

            if (!groundBelow || wallAhead)
            {
                IsActive = false;
                return;
            }

            Position = newPos;
        }

        public Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X, (int)Position.Y - RIPPLE_HEIGHT, RIPPLE_WIDTH, RIPPLE_HEIGHT);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
        {
            if (!IsActive) return;

            // Draw as orange/yellow ripple effect
            float alpha = lifetime / RIPPLE_LIFETIME;
            spriteBatch.Draw(pixel, GetHitbox(), Color.Orange * alpha);
        }
    }
}
