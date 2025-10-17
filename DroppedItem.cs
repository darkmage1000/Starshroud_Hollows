using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarshroudHollows.Enums;
using System;
using Microsoft.Xna.Framework.Content;

namespace StarshroudHollows.Entities
{
    public class DroppedItem
    {
        public Vector2 Position; // Made public field to fix modification error
        public Vector2 Velocity; // Made public field to fix modification error
        public ItemType ItemType { get; private set; }

        private const float GRAVITY = 0.3f;
        private const float MAX_FALL_SPEED = 8f;
        private const float BOUNCE_DAMPING = 0.5f;
        private const int ITEM_SIZE = 16;

        private bool isOnGround;
        private float lifetime;
        private const float DESPAWN_TIME = 300f;
        private float pickupDelay;
        private const float PICKUP_DELAY_TIME = 1.0f;

        private Texture2D itemSprite;
        private static ContentManager staticContent;

        public static void SetStaticContent(ContentManager content)
        {
            staticContent = content;
        }

        public DroppedItem(Vector2 position, ItemType itemType)
        {
            Position = position;
            ItemType = itemType;
            Random rand = new Random();
            Velocity = new Vector2((float)(rand.NextDouble() * 4 - 2), -3f);
            isOnGround = false;
            lifetime = 0f;
            pickupDelay = PICKUP_DELAY_TIME;
            LoadItemSprite();
        }

        private void LoadItemSprite()
        {
            string spriteName = GetSpriteName(ItemType);
            if (!string.IsNullOrEmpty(spriteName) && staticContent != null)
            {
                try { itemSprite = staticContent.Load<Texture2D>(spriteName); }
                catch { itemSprite = null; }
            }
        }

        private string GetSpriteName(ItemType type)
        {
            // This method should contain all your item sprite names
            // (Using a condensed version for brevity)
            switch (type)
            {
                case ItemType.Dirt: return "dirt";
                case ItemType.Stone: return "stone";
                case ItemType.Wood: return "wood";
                case ItemType.Iron: return "ironore";
                case ItemType.Gold: return "goldore";
                case ItemType.IronBar: return "ironbar";
                case ItemType.GoldBar: return "goldbar";
                // ... add all other items here ...
                default: return null;
            }
        }

        public void Update(float deltaTime, StarshroudHollows.World.World world)
        {
            lifetime += deltaTime;
            if (pickupDelay > 0) pickupDelay -= deltaTime;
            if (!isOnGround) Velocity.Y = Math.Min(Velocity.Y + GRAVITY, MAX_FALL_SPEED);

            Vector2 newPosition = Position + Velocity;
            int tileX = (int)(newPosition.X / StarshroudHollows.World.World.TILE_SIZE);
            int tileY = (int)((newPosition.Y + ITEM_SIZE) / StarshroudHollows.World.World.TILE_SIZE);

            if (world.IsSolidAtPosition(tileX, tileY))
            {
                if (Velocity.Y > 0)
                {
                    Velocity.Y *= -BOUNCE_DAMPING;
                    Velocity.X *= 0.8f;
                    if (Math.Abs(Velocity.Y) < 0.5f) { Velocity.Y = 0; isOnGround = true; }
                }
            }
            else { Position = newPosition; isOnGround = false; }
            if (isOnGround) { Velocity.X *= 0.95f; if (Math.Abs(Velocity.X) < 0.1f) Velocity.X = 0; }
        }

        // ADDED: Missing method
        public void ApplyMagnetism(Vector2 playerCenter, float magnetRange)
        {
            float distance = Vector2.Distance(Position, playerCenter);
            if (distance < magnetRange && distance > 10f)
            {
                Velocity += Vector2.Normalize(playerCenter - Position) * 5f * 0.1f;
            }
        }

        public bool CanCollect(Vector2 pPos, int pW, int pH)
        {
            if (pickupDelay > 0) return false;
            return new Rectangle((int)Position.X, (int)Position.Y, ITEM_SIZE, ITEM_SIZE)
                   .Intersects(new Rectangle((int)pPos.X, (int)pPos.Y, pW, pH));
        }

        public bool ShouldDespawn() => lifetime >= DESPAWN_TIME;

        // ADDED: Missing method
        public Rectangle GetBounds()
        {
            return new Rectangle((int)Position.X - 8, (int)Position.Y - 8, 16, 16);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            Rectangle itemRect = new Rectangle((int)Position.X, (int)Position.Y, ITEM_SIZE, ITEM_SIZE);
            if (itemSprite != null)
            {
                spriteBatch.Draw(itemSprite, itemRect, Color.White);
            }
            else
            {
                spriteBatch.Draw(pixelTexture, itemRect, GetItemColor(ItemType));
            }
        }

        private Color GetItemColor(ItemType type)
        {
            switch (type)
            {
                case ItemType.Dirt: return new Color(150, 75, 0);
                case ItemType.Stone: return new Color(128, 128, 128);
                case ItemType.Iron: return new Color(191, 148, 112);
                case ItemType.Gold: return new Color(255, 215, 0);
                case ItemType.IronBar: return new Color(210, 190, 170);
                case ItemType.GoldBar: return new Color(255, 215, 0);
                // ... add all other items here ...
                default: return Color.White;
            }
        }
    }
}