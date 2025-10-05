using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.Enums;
using System;

namespace Claude4_5Terraria.Entities
{
    public class DroppedItem
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public ItemType ItemType { get; private set; }  // Changed from TileType to ItemType

        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private const float BOUNCE_DAMPING = 0.5f;
        private const int ITEM_SIZE = 16;

        private bool isOnGround;
        private float lifetime;
        private const float DESPAWN_TIME = 300f;

        public DroppedItem(Vector2 position, ItemType itemType)  // Changed parameter type
        {
            Position = position;
            ItemType = itemType;

            Random rand = new Random(Guid.NewGuid().GetHashCode());
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float speed = 2f + (float)(rand.NextDouble() * 2f);
            Velocity = new Vector2(
                (float)Math.Cos(angle) * speed,
                (float)Math.Sin(angle) * speed - 3f
            );

            isOnGround = false;
            lifetime = 0f;
        }

        public void Update(float deltaTime, World.World world)
        {
            lifetime += deltaTime;

            if (!isOnGround)
            {
                Velocity = new Vector2(Velocity.X, Velocity.Y + GRAVITY);

                if (Velocity.Y > MAX_FALL_SPEED)
                {
                    Velocity = new Vector2(Velocity.X, MAX_FALL_SPEED);
                }
            }

            Vector2 newPosition = Position + Velocity;

            int tileX = (int)(newPosition.X / World.World.TILE_SIZE);
            int tileY = (int)((newPosition.Y + ITEM_SIZE) / World.World.TILE_SIZE);

            if (world.IsSolidAtPosition(tileX, tileY))
            {
                if (Velocity.Y > 0)
                {
                    Velocity = new Vector2(Velocity.X * 0.8f, -Velocity.Y * BOUNCE_DAMPING);

                    if (Math.Abs(Velocity.Y) < 0.5f)
                    {
                        Velocity = new Vector2(Velocity.X * 0.9f, 0);
                        isOnGround = true;
                    }
                }
            }
            else
            {
                Position = newPosition;
                isOnGround = false;
            }

            if (isOnGround)
            {
                Velocity = new Vector2(Velocity.X * 0.95f, Velocity.Y);
                if (Math.Abs(Velocity.X) < 0.1f)
                {
                    Velocity = new Vector2(0, Velocity.Y);
                }
            }
        }

        public void ApplyMagnetism(Vector2 playerCenter, float magnetRange)
        {
            float distance = Vector2.Distance(Position, playerCenter);

            if (distance < magnetRange && distance > 10f)
            {
                Vector2 direction = Vector2.Normalize(playerCenter - Position);
                float pullStrength = 5f;
                Velocity += direction * pullStrength * 0.1f;
            }
        }

        public bool CanCollect(Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            Rectangle itemRect = new Rectangle((int)Position.X, (int)Position.Y, ITEM_SIZE, ITEM_SIZE);
            Rectangle playerRect = new Rectangle((int)playerPosition.X, (int)playerPosition.Y, playerWidth, playerHeight);

            return itemRect.Intersects(playerRect);
        }

        public bool ShouldDespawn()
        {
            return lifetime >= DESPAWN_TIME;
        }
        public Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X - 8, (int)Position.Y - 8, 16, 16);
        }
        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            Color itemColor = GetItemColor(ItemType);

            Rectangle itemRect = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                ITEM_SIZE,
                ITEM_SIZE
            );

            spriteBatch.Draw(pixelTexture, itemRect, itemColor);
            DrawBorder(spriteBatch, pixelTexture, itemRect, 1, Color.White * 0.8f);
        }

        private Color GetItemColor(ItemType type)
        {
            switch (type)
            {
                case ItemType.Dirt:
                    return new Color(150, 75, 0);
                case ItemType.Grass:
                    return new Color(34, 139, 34);
                case ItemType.Stone:
                    return new Color(128, 128, 128);
                case ItemType.Copper:
                    return new Color(255, 140, 0);
                case ItemType.Silver:
                    return new Color(192, 192, 192);
                case ItemType.Platinum:
                    return new Color(144, 238, 144);
                case ItemType.Wood:
                    return new Color(101, 67, 33);
                case ItemType.Coal:
                    return new Color(40, 40, 40);  // Dark gray/black
                case ItemType.Acorn:
                    return new Color(139, 90, 43);  // Brown acorn color
                default:
                    return Color.White;
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int thickness, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}