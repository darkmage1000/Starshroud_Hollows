using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Interfaces;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.Systems.Summons
{
    public class EchoWisp : Summon
    {
        private const float MOVE_SPEED = 200f;
        private const float ATTACK_COOLDOWN = 0.5f;
        private const float IDLE_HOVER_SPEED = 50f;
        private const float IDLE_HOVER_RANGE = 20f;
        private const int WIDTH = 24;
        private const int HEIGHT = 24;

        private float attackTimer = 0f;
        private float hoverTimer = 0f;
        private Vector2 hoverOffset = Vector2.Zero;
        private bool returningToPlayer = false;

        public EchoWisp(Vector2 spawnPosition)
            : base(spawnPosition, 1f, 40f, 20f) { }

        public override void Update(float deltaTime, Vector2 playerPosition, List<IDamageable> targets, StarshroudHollows.World.World world)
        {
            if (!IsActive) return;

            animationTimer += deltaTime;
            if (animationTimer >= FRAME_TIME)
            {
                animationTimer -= FRAME_TIME;
                currentFrame = (currentFrame + 1) % 4;
            }

            if (!IsInRangeOfPlayer(playerPosition))
            {
                returningToPlayer = true;
                currentTarget = null;
            }

            if (currentTarget == null || !currentTarget.IsAlive || !currentTarget.CanBeDamaged())
            {
                currentTarget = FindNearestTarget(targets, Position);
                if (currentTarget == null) currentTarget = FindNearestTarget(targets, playerPosition);
            }

            if (currentTarget != null && IsInRangeOfPlayer(playerPosition))
            {
                returningToPlayer = false;
                MoveTowards(currentTarget.GetHitbox().Center.ToVector2(), MOVE_SPEED, deltaTime, world);
                AttackBehavior(deltaTime);
            }
            else if (returningToPlayer)
            {
                MoveTowards(playerPosition, MOVE_SPEED * 1.5f, deltaTime, world);
                if (Vector2.Distance(Position, playerPosition) < 64f) returningToPlayer = false;
            }
            else
            {
                IdleHoverBehavior(deltaTime, playerPosition, world);
            }

            if (attackTimer > 0) attackTimer -= deltaTime;
        }

        private void AttackBehavior(float deltaTime)
        {
            if (currentTarget == null) return;
            float distance = Vector2.Distance(Position, currentTarget.GetHitbox().Center.ToVector2());

            if (distance < 50f && attackTimer <= 0 && currentTarget.CanBeDamaged())
            {
                currentTarget.TakeDamage(Damage);
                currentTarget.ResetHitCooldown();
                attackTimer = ATTACK_COOLDOWN;
            }
        }

        private void IdleHoverBehavior(float deltaTime, Vector2 playerPosition, StarshroudHollows.World.World world)
        {
            hoverTimer += deltaTime;
            float hoverX = (float)Math.Sin(hoverTimer * 2f) * IDLE_HOVER_RANGE;
            float hoverY = (float)Math.Cos(hoverTimer * 1.5f) * IDLE_HOVER_RANGE;
            Vector2 targetIdlePos = playerPosition + new Vector2(hoverX, hoverY - 40);

            MoveTowards(targetIdlePos, IDLE_HOVER_SPEED, deltaTime, world);
        }

        private void MoveTowards(Vector2 target, float speed, float deltaTime, StarshroudHollows.World.World world)
        {
            Vector2 direction = target - Position;
            if (direction.LengthSquared() < 1) return;
            direction.Normalize();

            Vector2 desiredPosition = Position + direction * speed * deltaTime;

            // IMPROVED: Try direct path first
            if (CanMoveTo(desiredPosition, world))
            {
                Position = desiredPosition;
                return;
            }

            // IMPROVED: Smart obstacle avoidance - try multiple angles
            // Try 8 directions around the obstacle
            float[] angles = { 45f, -45f, 90f, -90f, 135f, -135f, 22.5f, -22.5f };
            
            foreach (float angle in angles)
            {
                float radians = MathHelper.ToRadians(angle);
                Vector2 rotatedDir = new Vector2(
                    direction.X * (float)Math.Cos(radians) - direction.Y * (float)Math.Sin(radians),
                    direction.X * (float)Math.Sin(radians) + direction.Y * (float)Math.Cos(radians)
                );
                
                Vector2 newPos = Position + rotatedDir * speed * deltaTime;
                if (CanMoveTo(newPos, world))
                {
                    Position = newPos;
                    return;
                }
            }

            // IMPROVED: If completely stuck, try moving straight up (phase through)
            Vector2 moveUp = Position + new Vector2(0, -speed * deltaTime * 2f);
            if (CanMoveTo(moveUp, world))
            {
                Position = moveUp;
                return;
            }
            
            // Last resort: Try moving in any cardinal direction
            Vector2[] escapeDirections = {
                new Vector2(1, 0),
                new Vector2(-1, 0),
                new Vector2(0, 1),
                new Vector2(0, -1)
            };
            
            foreach (Vector2 escapeDir in escapeDirections)
            {
                Vector2 escapePos = Position + escapeDir * speed * deltaTime;
                if (CanMoveTo(escapePos, world))
                {
                    Position = escapePos;
                    return;
                }
            }
        }

        private bool CanMoveTo(Vector2 targetPosition, StarshroudHollows.World.World world)
        {
            int tileX = (int)(targetPosition.X / StarshroudHollows.World.World.TILE_SIZE);
            int tileY = (int)(targetPosition.Y / StarshroudHollows.World.World.TILE_SIZE);

            // --- THIS IS THE FIX ---
            // Reverted to the simple, reliable collision check that exists in your World.cs file.
            return !world.IsSolidAtPosition(tileX, tileY);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsActive) return;
            Rectangle destRect = new Rectangle((int)Position.X - WIDTH / 2, (int)Position.Y - HEIGHT / 2, WIDTH, HEIGHT);
            if (texture != null)
            {
                int frameWidth = texture.Width / 4;
                Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, texture.Height);
                spriteBatch.Draw(texture, destRect, sourceRect, Color.White);
            }
            else
            {
                spriteBatch.Draw(pixelTexture, destRect, Color.LightCyan * 0.7f);
            }
        }

        public override Rectangle GetHitbox()
        {
            return new Rectangle((int)Position.X - WIDTH / 2, (int)Position.Y - HEIGHT / 2, WIDTH, HEIGHT);
        }
    }
}