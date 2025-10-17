using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace StarshroudHollows.Entities
{
    /// <summary>
    /// Visual portal that teleports the player to boss arenas
    /// </summary>
    public class Portal
    {
        public Vector2 Position { get; set; }
        public bool IsActive { get; private set; }
        public PortalType Type { get; private set; }
        public Systems.BossType DestinationBoss { get; private set; }
        
        // Visual properties
        private float rotationAngle = 0f;
        private float pulseTimer = 0f;
        private const float ROTATION_SPEED = 2f;
        private const float PULSE_SPEED = 3f;
        
        // Portal dimensions
        private const int PORTAL_WIDTH = 64; // 2 tiles wide
        private const int PORTAL_HEIGHT = 96; // 3 tiles tall
        
        // Particle effect
        private Random random;
        private float particleSpawnTimer = 0f;
        private const float PARTICLE_SPAWN_RATE = 0.05f;

        public Portal(Vector2 position, PortalType type, Systems.BossType bossType)
        {
            Position = position;
            Type = type;
            DestinationBoss = bossType;
            IsActive = true;
            random = new Random();
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            rotationAngle += ROTATION_SPEED * deltaTime;
            if (rotationAngle > MathHelper.TwoPi)
                rotationAngle -= MathHelper.TwoPi;

            pulseTimer += PULSE_SPEED * deltaTime;
            if (pulseTimer > MathHelper.TwoPi)
                pulseTimer -= MathHelper.TwoPi;

            particleSpawnTimer += deltaTime;
        }

        public bool CheckPlayerCollision(Vector2 playerPosition, int playerWidth, int playerHeight)
        {
            if (!IsActive) return false;

            Rectangle portalRect = GetHitbox();
            Rectangle playerRect = new Rectangle(
                (int)playerPosition.X,
                (int)playerPosition.Y,
                playerWidth,
                playerHeight
            );

            return portalRect.Intersects(playerRect);
        }

        public Rectangle GetHitbox()
        {
            return new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                PORTAL_WIDTH,
                PORTAL_HEIGHT
            );
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsActive) return;

            Rectangle portalRect = GetHitbox();
            
            // Get portal color based on type
            Color portalColor = GetPortalColor();
            
            // Pulsing effect
            float pulseScale = 0.8f + (float)Math.Sin(pulseTimer) * 0.2f;
            Color pulsedColor = portalColor * pulseScale;

            // Draw outer glow
            Rectangle glowRect = new Rectangle(
                portalRect.X - 8,
                portalRect.Y - 8,
                portalRect.Width + 16,
                portalRect.Height + 16
            );
            spriteBatch.Draw(pixelTexture, glowRect, portalColor * 0.3f);

            // Draw main portal body with layers
            for (int i = 3; i >= 0; i--)
            {
                int offset = i * 4;
                Rectangle layerRect = new Rectangle(
                    portalRect.X + offset,
                    portalRect.Y + offset,
                    portalRect.Width - (offset * 2),
                    portalRect.Height - (offset * 2)
                );
                
                float layerAlpha = 0.4f + (i * 0.15f);
                spriteBatch.Draw(pixelTexture, layerRect, pulsedColor * layerAlpha);
            }

            // Draw swirling effect (simulate rotation with rectangles)
            DrawSwirlEffect(spriteBatch, pixelTexture, portalRect, pulsedColor);

            // Draw bright center
            Rectangle centerRect = new Rectangle(
                portalRect.X + PORTAL_WIDTH / 3,
                portalRect.Y + PORTAL_HEIGHT / 3,
                PORTAL_WIDTH / 3,
                PORTAL_HEIGHT / 3
            );
            spriteBatch.Draw(pixelTexture, centerRect, Color.White * pulseScale * 0.8f);
        }

        private void DrawSwirlEffect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            // Draw rotating "arms" to simulate swirl
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            int armCount = 4;
            
            for (int i = 0; i < armCount; i++)
            {
                float angle = rotationAngle + (i * MathHelper.TwoPi / armCount);
                int armLength = 20;
                
                int endX = centerX + (int)(Math.Cos(angle) * armLength);
                int endY = centerY + (int)(Math.Sin(angle) * armLength);
                
                Rectangle armRect = new Rectangle(
                    Math.Min(centerX, endX),
                    Math.Min(centerY, endY),
                    Math.Abs(endX - centerX) + 4,
                    Math.Abs(endY - centerY) + 4
                );
                
                spriteBatch.Draw(pixel, armRect, color * 0.6f);
            }
        }

        private Color GetPortalColor()
        {
            switch (Type)
            {
                case PortalType.Entrance:
                    // Entrance portals are blue/purple
                    return new Color(100, 100, 255);
                    
                case PortalType.Exit:
                    // Exit portals are green
                    return new Color(100, 255, 100);
                    
                default:
                    return Color.Purple;
            }
        }
    }

    /// <summary>
    /// Types of portals
    /// </summary>
    public enum PortalType
    {
        Entrance,  // Takes you TO the arena
        Exit       // Takes you FROM the arena back to overworld
    }
}