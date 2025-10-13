using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Interfaces;
using System;
using System.Collections.Generic;

namespace Claude4_5Terraria.Systems.Summons
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

        public override void Update(float deltaTime, Vector2 playerPosition, List<IDamageable> targets, World.World world)
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

        private void IdleHoverBehavior(float deltaTime, Vector2 playerPosition, World.World world)
        {
            hoverTimer += deltaTime;
            float hoverX = (float)Math.Sin(hoverTimer * 2f) * IDLE_HOVER_RANGE;
            float hoverY = (float)Math.Cos(hoverTimer * 1.5f) * IDLE_HOVER_RANGE;
            Vector2 targetIdlePos = playerPosition + new Vector2(hoverX, hoverY - 40);

            MoveTowards(targetIdlePos, IDLE_HOVER_SPEED, deltaTime, world);
        }

        private void MoveTowards(Vector2 target, float speed, float deltaTime, World.World world)
        {
            Vector2 direction = target - Position;
            if (direction.LengthSquared() < 1) return;
            direction.Normalize();

            Vector2 desiredPosition = Position + direction * speed * deltaTime;

            if (CanMoveTo(desiredPosition, world))
            {
                Position = desiredPosition;
            }
            else
            {
                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
                if (CanMoveTo(Position + perpendicular * speed * deltaTime, world))
                {
                    Position += perpendicular * speed * deltaTime;
                }
                else if (CanMoveTo(Position - perpendicular * speed * deltaTime, world))
                {
                    Position -= perpendicular * speed * deltaTime;
                }
            }
        }

        private bool CanMoveTo(Vector2 targetPosition, World.World world)
        {
            int tileX = (int)(targetPosition.X / World.World.TILE_SIZE);
            int tileY = (int)(targetPosition.Y / World.World.TILE_SIZE);

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