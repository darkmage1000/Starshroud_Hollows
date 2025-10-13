using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Interfaces;
using System.Collections.Generic;

namespace Claude4_5Terraria.Systems.Summons
{
    public abstract class Summon
    {
        // CHANGED: Made the 'set' public to ensure it can always be modified.
        public Vector2 Position { get; set; }
        public bool IsActive { get; protected set; }
        public float Damage { get; protected set; }
        public float MaxRangeFromPlayer { get; protected set; }
        public float DetectionRange { get; protected set; }

        protected Texture2D texture;
        protected float animationTimer = 0f;
        protected int currentFrame = 0;
        protected const float FRAME_TIME = 0.15f;

        protected IDamageable currentTarget;
        protected Vector2 idlePosition;
        protected float lifetime = 0f;

        protected Summon(Vector2 spawnPosition, float damage, float maxRange, float detectionRange)
        {
            Position = spawnPosition;
            idlePosition = spawnPosition;
            IsActive = true;
            Damage = damage;
            MaxRangeFromPlayer = maxRange;
            DetectionRange = detectionRange;
        }

        public abstract void Update(float deltaTime, Vector2 playerPosition, List<IDamageable> targets, World.World world);
        public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture);
        public abstract Rectangle GetHitbox();

        public void SetTexture(Texture2D tex) => texture = tex;
        public void Deactivate() => IsActive = false;

        protected bool IsInRangeOfPlayer(Vector2 playerPosition)
        {
            return Vector2.Distance(Position, playerPosition) <= MaxRangeFromPlayer * World.World.TILE_SIZE;
        }

        protected IDamageable FindNearestTarget(List<IDamageable> targets, Vector2 searchPosition)
        {
            IDamageable nearest = null;
            float nearestDist = DetectionRange * World.World.TILE_SIZE;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || !target.CanBeDamaged()) continue;

                float dist = Vector2.Distance(searchPosition, target.Position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = target;
                }
            }
            return nearest;
        }
    }
}