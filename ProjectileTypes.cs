using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace StarshroudHollows.Entities
{
    // Magic Bolt - basic magic projectile
    public class MagicBolt : Projectile
    {
        public MagicBolt(Vector2 position, Vector2 direction, float damage) 
            : base(position, direction * 400f, damage, 3.0f)
        {
            tintColor = new Color(150, 100, 255);
            scale = 1.0f;
        }
    }

    // Fire Bolt - faster, fiery projectile
    public class FireBolt : Projectile
    {
        public FireBolt(Vector2 position, Vector2 direction, float damage) 
            : base(position, direction * 500f, damage, 2.5f)
        {
            tintColor = new Color(255, 100, 50);
            scale = 1.2f;
        }
    }

    // Lightning Blast - very fast lightning projectile
    public class LightningBlast : Projectile
    {
        private float pulseTimer = 0f;

        public LightningBlast(Vector2 position, Vector2 direction, float damage) 
            : base(position, direction * 800f, damage, 1.5f)
        {
            tintColor = new Color(200, 200, 255);
            scale = 0.8f;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            pulseTimer += deltaTime * 10f;
            float pulse = (float)Math.Sin(pulseTimer) * 0.2f + 1.0f;
            scale = 0.8f * pulse;
        }
    }

    // Water Bubble - bouncy water projectile
    public class WaterBubble : Projectile
    {
        private float bobTimer = 0f;

        public WaterBubble(Vector2 position, Vector2 direction, float damage) 
            : base(position, direction * 300f, damage, 4.0f)
        {
            tintColor = new Color(100, 150, 255, 200);
            scale = 1.5f;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            bobTimer += deltaTime * 5f;
            // Add a slight wobble effect
            Vector2 wobble = new Vector2(0, (float)Math.Sin(bobTimer) * 20f * deltaTime);
            Position += wobble;
        }
    }

    // Half Moon Slash - crescent-shaped slash
    public class HalfMoonSlash : Projectile
    {
        public HalfMoonSlash(Vector2 position, Vector2 direction, float damage) 
            : base(position, direction * 450f, damage, 2.0f)
        {
            tintColor = new Color(200, 200, 200);
            scale = 2.0f;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            rotation += deltaTime * 10f; // Spin the slash
        }
    }

    // Runic Laser - continuous beam
    public class RunicLaser : Projectile
    {
        private StarshroudHollows.World.World world;
        
        public RunicLaser(Vector2 position, Vector2 direction, float damage, StarshroudHollows.World.World world) 
            : base(position, direction * 1000f, damage, 0.2f)
        {
            this.world = world;
            tintColor = new Color(100, 255, 255);
            scale = 0.5f;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            
            // Check for tile collision
            int tileX = (int)(Position.X / StarshroudHollows.World.World.TILE_SIZE);
            int tileY = (int)(Position.Y / StarshroudHollows.World.World.TILE_SIZE);
            
            if (world != null)
            {
                if (world.IsSolidAtPosition(tileX, tileY))
                {
                    IsActive = false;
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsActive) return;

            if (texture != null)
            {
                // Draw a long beam
                Rectangle sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
                Vector2 origin = new Vector2(0, texture.Height / 2);
                float length = Velocity.Length() * 0.05f; // Visual length based on speed
                spriteBatch.Draw(texture, Position, sourceRect, tintColor, rotation, origin, 
                    new Vector2(length, scale), SpriteEffects.None, 0f);
            }
            else
            {
                // Draw a line with pixel texture
                Vector2 end = Position + Vector2.Normalize(Velocity) * 50f;
                DrawLine(spriteBatch, pixelTexture, Position, end, tintColor, 3);
            }
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
                null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }
    }

    // Nature Vine - stationary growing vine
    public class NatureVine : Projectile
    {
        private Vector2 targetPosition;
        private StarshroudHollows.World.World world;
        private float growthTimer = 0f;
        private const float GROWTH_DURATION = 0.5f;
        private Vector2 startPosition;

        public NatureVine(Vector2 startPos, Vector2 targetPos, float damage, StarshroudHollows.World.World world) 
            : base(startPos, Vector2.Zero, damage, 2.0f)
        {
            this.startPosition = startPos;
            this.targetPosition = targetPos;
            this.world = world;
            tintColor = new Color(50, 200, 50);
            scale = 1.0f;
        }

        public override void Update(float deltaTime)
        {
            if (!IsActive) return;

            lifetime += deltaTime;
            growthTimer += deltaTime;

            if (lifetime >= maxLifetime)
            {
                IsActive = false;
                return;
            }

            // Grow from start to target position
            if (growthTimer < GROWTH_DURATION)
            {
                float progress = growthTimer / GROWTH_DURATION;
                Position = Vector2.Lerp(startPosition, targetPosition, progress);
            }
            else
            {
                Position = targetPosition;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsActive) return;

            if (texture != null)
            {
                Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
                spriteBatch.Draw(texture, Position, null, tintColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // Draw vine as line from start to current position
                DrawVineLine(spriteBatch, pixelTexture, startPosition, Position, tintColor, 6);
            }
        }

        private void DrawVineLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            spriteBatch.Draw(pixel,
                new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness),
                null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
        }
    }
}
