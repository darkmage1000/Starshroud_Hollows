using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Claude4_5Terraria.Entities
{
    public abstract class Projectile
    {
        public Vector2 Position { get; protected set; }
        public Vector2 Velocity { get; protected set; }
        public float Damage { get; protected set; }
        public bool IsActive { get; protected set; }
        protected Texture2D texture;
        protected float lifetime;
        protected float maxLifetime;
        protected float rotation;
        protected Color tintColor = Color.White;
        protected float scale = 1.0f;

        public Projectile(Vector2 position, Vector2 velocity, float damage, float maxLifetime = 5.0f)
        {
            Position = position;
            Velocity = velocity;
            Damage = damage;
            IsActive = true;
            this.maxLifetime = maxLifetime;
            lifetime = 0f;
            rotation = (float)Math.Atan2(velocity.Y, velocity.X);
        }

        public virtual void Update(float deltaTime)
        {
            if (!IsActive) return;

            lifetime += deltaTime;
            if (lifetime >= maxLifetime)
            {
                IsActive = false;
                return;
            }

            Position += Velocity * deltaTime;
        }

        public virtual void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsActive) return;

            if (texture != null)
            {
                Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
                spriteBatch.Draw(texture, Position, null, tintColor, rotation, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // Default drawing with pixel texture
                spriteBatch.Draw(pixelTexture, new Rectangle((int)Position.X - 4, (int)Position.Y - 4, 8, 8), tintColor);
            }
        }

        public void SetTexture(Texture2D tex)
        {
            texture = tex;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public Rectangle GetBounds()
        {
            if (texture != null)
            {
                int width = (int)(texture.Width * scale);
                int height = (int)(texture.Height * scale);
                return new Rectangle((int)Position.X - width / 2, (int)Position.Y - height / 2, width, height);
            }
            return new Rectangle((int)Position.X - 4, (int)Position.Y - 4, 8, 8);
        }
    }
}
