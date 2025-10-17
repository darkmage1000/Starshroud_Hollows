using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using StarshroudHollows.Interfaces;

namespace StarshroudHollows.Entities
{
    /// <summary>
    /// Cave Troll Boss - Giant slow boss with devastating attacks
    /// Arena: Flat ground so his AOE ripples can really shine
    /// Health: 100, same as player
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

        // Attack properties - BRUTAL DAMAGE
        public const float CLUB_SWING_DAMAGE = 30f;  // Melee club hit
        public const float AOE_RIPPLE_DAMAGE = 25f;  // Ground ripple AOE
        public const float DASH_DAMAGE = 50f;        // Charge attack at 50% HP
        
        private const float SLAM_COOLDOWN = 3.0f;
        private float slamTimer = 0f;
        private bool isAttacking = false;
        private float attackAnimationTimer = 0f;
        private const float ATTACK_DURATION = 1.2f; // Total animation time
        private const float CLUB_SWING_TIME = 0.8f; // Time before club hits ground
        private bool hasSpawnedRipples = false; // Track if ripples spawned this attack

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

        private StarshroudHollows.World.World world;

        public CaveTroll(Vector2 spawnPosition, StarshroudHollows.World.World world)
        {
            Position = spawnPosition;
            this.world = world;
            MaxHealth = 100f;  // Same as player!
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

                // Spawn ripples when club hits ground (at CLUB_SWING_TIME)
                if (attackAnimationTimer >= CLUB_SWING_TIME && !hasSpawnedRipples)
                {
                    SpawnGroundRipples();
                    hasSpawnedRipples = true;
                }

                if (attackAnimationTimer >= ATTACK_DURATION)
                {
                    isAttacking = false;
                    attackAnimationTimer = 0f;
                    hasSpawnedRipples = false;
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
                // Check if we should do charge attack (under 50% HP, cooldown ready, on ground, player at range)
                float distanceToPlayer = Vector2.Distance(Position, playerPosition);
                if (Health <= MaxHealth / 2f && chargeCooldownTimer <= 0 && isOnGround && distanceToPlayer > 160f) // 5+ tiles away
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

            // Attack ranges:
            // - Close range: Swing club for 30 damage if player gets too close
            // - Medium/Long range: Use ground ripples for 25 AOE damage
            const float CLUB_SWING_RANGE = 48f;          // ~1.5 tiles (very close for club swing)
            const float RIPPLE_MIN_RANGE = 48f;          // Min distance for ripple (1.5 tiles)
            const float RIPPLE_MAX_RANGE = 480f;         // Max distance for ripple (15 tiles)

            if (distanceToPlayer > RIPPLE_MAX_RANGE)
            {
                // Player is too far, chase them
                velocity.X = directionToPlayer.X > 0 ? MOVE_SPEED : -MOVE_SPEED;
            }
            else if (distanceToPlayer >= RIPPLE_MIN_RANGE)
            {
                // Player is at medium/long range - use AOE ripple attack
                velocity.X = 0;
                if (slamTimer <= 0 && isOnGround && !isAttacking)
                {
                    PerformSlamAttack();
                }
            }
            else if (distanceToPlayer < CLUB_SWING_RANGE)
            {
                // Player is very close - still slam for ripples, but also check club collision in BossSystem
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
            hasSpawnedRipples = false;
            slamTimer = SLAM_COOLDOWN;
        }

        private void SpawnGroundRipples()
        {
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

        public bool IsChargingAttack()
        {
            return isCharging;
        }

        public float GetAttackProgress()
        {
            return attackAnimationTimer / ATTACK_DURATION;
        }

        // Get the club swing progress (0.0 to 1.0) for animation
        public float GetClubSwingProgress()
        {
            if (!isAttacking) return 0f;

            // Return progress from 0 to 1 during the swing phase
            float swingProgress = attackAnimationTimer / CLUB_SWING_TIME;
            return Math.Min(swingProgress, 1f);
        }

        // Check if club has hit the ground (for visual/sound effects)
        public bool HasClubHitGround()
        {
            return isAttacking && attackAnimationTimer >= CLUB_SWING_TIME;
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

            // Charging glow effect
            if (isCharging)
            {
                drawColor = Color.Lerp(Color.White, Color.OrangeRed, 0.5f);
            }

            if (trollTexture != null)
            {
                // If you have animation frames in your sprite sheet, you can use GetClubSwingProgress()
                // to determine which frame to draw
                spriteBatch.Draw(trollTexture, GetHitbox(), drawColor);
            }
            else
            {
                // Fallback: Draw as dark brown rectangle
                spriteBatch.Draw(fallbackTexture, GetHitbox(), Color.SaddleBrown * 0.8f);
            }

            // Draw club swing visual effect
            if (isAttacking)
            {
                DrawClubSwingEffect(spriteBatch, fallbackTexture);
            }

            // Draw charge indicator
            if (isCharging)
            {
                DrawChargeEffect(spriteBatch, fallbackTexture);
            }

            // Draw health bar above boss
            DrawHealthBar(spriteBatch, fallbackTexture);
        }

        private void DrawClubSwingEffect(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Visual indicator of club swing
            float swingProgress = GetClubSwingProgress();

            // Calculate club position based on swing progress
            // Club starts at shoulder level (around 30% of height), swings down to ground
            float shoulderHeight = Position.Y + (HEIGHT * 0.3f); // Shoulder area
            float groundHeight = Position.Y + HEIGHT; // Ground level

            // Interpolate between shoulder and ground based on swing progress
            float clubY = shoulderHeight + ((groundHeight - shoulderHeight) * swingProgress);

            // Position club slightly to the side (right side of troll)
            float clubX = Position.X + WIDTH * 0.7f - 8;

            // Club dimensions
            int clubWidth = 16;
            int clubHeight = 40;

            // Color changes as it swings (yellow -> red at impact)
            Color clubColor = Color.Lerp(Color.Yellow, Color.OrangeRed, swingProgress);

            Rectangle clubRect = new Rectangle((int)clubX, (int)clubY, clubWidth, clubHeight);
            spriteBatch.Draw(pixel, clubRect, clubColor * 0.7f);

            // Draw impact effect when club hits ground
            if (HasClubHitGround())
            {
                // Ground impact flash
                float impactAlpha = 1f - ((attackAnimationTimer - CLUB_SWING_TIME) / (ATTACK_DURATION - CLUB_SWING_TIME));
                Rectangle impactRect = new Rectangle((int)(Position.X), (int)(Position.Y + HEIGHT - 10), WIDTH, 20);
                spriteBatch.Draw(pixel, impactRect, Color.Orange * impactAlpha * 0.8f);
            }
        }

        private void DrawChargeEffect(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Speed lines behind charging troll
            int lineCount = 5;
            for (int i = 0; i < lineCount; i++)
            {
                float offset = i * 20;
                Rectangle lineRect = new Rectangle(
                    (int)(Position.X - (chargeDirection * offset)),
                    (int)(Position.Y + (i * HEIGHT / lineCount)),
                    10,
                    HEIGHT / lineCount
                );
                spriteBatch.Draw(pixel, lineRect, Color.OrangeRed * (0.5f - (i * 0.1f)));
            }
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
    /// Deals 25 AOE damage to player
    /// </summary>
    public class GroundRipple
    {
        public Vector2 Position { get; private set; }
        public bool IsActive { get; private set; }
        public int Direction { get; private set; } // -1 for left, 1 for right

        private const float RIPPLE_SPEED = 3.0f;
        private const float RIPPLE_LIFETIME = 3.0f;
        private float lifetime;
        private StarshroudHollows.World.World world;

        private const int RIPPLE_WIDTH = 16;
        private const int RIPPLE_HEIGHT = 16;

        public GroundRipple(float x, float y, int direction, StarshroudHollows.World.World world)
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
            int tileX = (int)(newPos.X / StarshroudHollows.World.World.TILE_SIZE);
            int tileY = (int)((newPos.Y + RIPPLE_HEIGHT) / StarshroudHollows.World.World.TILE_SIZE);

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
