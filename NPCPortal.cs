using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace StarshroudHollows.Systems.Housing
{
    /// <summary>
    /// Special portal that spawns NPCs - appears in valid houses
    /// </summary>
    public class NPCPortal
    {
        public Vector2 Position { get; private set; }
        public bool IsActive { get; private set; }
        public House TargetHouse { get; private set; }
        
        // Visual properties
        private float rotationAngle = 0f;
        private float pulseTimer = 0f;
        private float lifeTimer = 0f;
        private const float ROTATION_SPEED = 3f;
        private const float PULSE_SPEED = 4f;
        private const float PORTAL_LIFETIME = 5f; // Portal stays for 5 seconds
        
        // Portal dimensions (smaller than boss portals)
        private const int PORTAL_WIDTH = 48;  // 1.5 tiles wide
        private const int PORTAL_HEIGHT = 64; // 2 tiles tall
        
        private Random random;
        
        // NPC spawning
        private bool hasSpawnedNPC = false;
        private const float NPC_SPAWN_DELAY = 1.5f; // NPC appears after 1.5 seconds
        
        public bool HasSpawnedNPC => hasSpawnedNPC;

        public NPCPortal(Vector2 position, House targetHouse)
        {
            Position = position;
            TargetHouse = targetHouse;
            IsActive = true;
            random = new Random();
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            lifeTimer += deltaTime;
            rotationAngle += ROTATION_SPEED * deltaTime;
            if (rotationAngle > MathHelper.TwoPi)
                rotationAngle -= MathHelper.TwoPi;

            pulseTimer += PULSE_SPEED * deltaTime;
            if (pulseTimer > MathHelper.TwoPi)
                pulseTimer -= MathHelper.TwoPi;

            // Mark NPC as ready to spawn
            if (lifeTimer >= NPC_SPAWN_DELAY && !hasSpawnedNPC)
            {
                hasSpawnedNPC = true;
            }

            // Portal disappears after lifetime
            if (lifeTimer >= PORTAL_LIFETIME)
            {
                IsActive = false;
            }
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

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            if (!IsActive) return;

            Rectangle portalRect = GetHitbox();
            
            // NPC portals are green/emerald colored (helpful/friendly)
            Color portalColor = new Color(50, 255, 150); // Bright emerald green
            
            // Pulsing effect
            float pulseScale = 0.7f + (float)Math.Sin(pulseTimer) * 0.3f;
            Color pulsedColor = portalColor * pulseScale;

            // Draw outer glow (larger during spawn)
            float spawnGlowScale = 1.0f;
            if (lifeTimer < NPC_SPAWN_DELAY)
            {
                // Extra glow when spawning NPC
                spawnGlowScale = 1.0f + (float)Math.Sin(lifeTimer * 10f) * 0.5f;
            }
            
            Rectangle glowRect = new Rectangle(
                portalRect.X - (int)(8 * spawnGlowScale),
                portalRect.Y - (int)(8 * spawnGlowScale),
                portalRect.Width + (int)(16 * spawnGlowScale),
                portalRect.Height + (int)(16 * spawnGlowScale)
            );
            spriteBatch.Draw(pixelTexture, glowRect, portalColor * 0.3f * pulseScale);

            // Draw main portal body with layers
            for (int i = 3; i >= 0; i--)
            {
                int offset = i * 3;
                Rectangle layerRect = new Rectangle(
                    portalRect.X + offset,
                    portalRect.Y + offset,
                    portalRect.Width - (offset * 2),
                    portalRect.Height - (offset * 2)
                );
                
                float layerAlpha = 0.5f + (i * 0.15f);
                spriteBatch.Draw(pixelTexture, layerRect, pulsedColor * layerAlpha);
            }

            // Draw swirling effect (magical energy)
            DrawSwirlEffect(spriteBatch, pixelTexture, portalRect, pulsedColor);

            // Draw bright center
            Rectangle centerRect = new Rectangle(
                portalRect.X + PORTAL_WIDTH / 3,
                portalRect.Y + PORTAL_HEIGHT / 3,
                PORTAL_WIDTH / 3,
                PORTAL_HEIGHT / 3
            );
            spriteBatch.Draw(pixelTexture, centerRect, Color.White * pulseScale);
            
            // Draw sparkles around portal (magical arrival effect)
            if (lifeTimer < NPC_SPAWN_DELAY + 1f)
            {
                DrawSparkles(spriteBatch, pixelTexture, portalRect);
            }
        }

        private void DrawSwirlEffect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
        {
            // Draw rotating "arms" to simulate swirl
            int centerX = rect.X + rect.Width / 2;
            int centerY = rect.Y + rect.Height / 2;
            int armCount = 6; // More arms for magical effect
            
            for (int i = 0; i < armCount; i++)
            {
                float angle = rotationAngle + (i * MathHelper.TwoPi / armCount);
                int armLength = 18;
                
                int endX = centerX + (int)(Math.Cos(angle) * armLength);
                int endY = centerY + (int)(Math.Sin(angle) * armLength);
                
                Rectangle armRect = new Rectangle(
                    Math.Min(centerX, endX) - 1,
                    Math.Min(centerY, endY) - 1,
                    Math.Abs(endX - centerX) + 3,
                    Math.Abs(endY - centerY) + 3
                );
                
                spriteBatch.Draw(pixel, armRect, color * 0.7f);
            }
        }
        
        private void DrawSparkles(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect)
        {
            // Draw floating sparkles around the portal
            for (int i = 0; i < 10; i++)
            {
                float sparkleAngle = (lifeTimer * 2f + i * 0.6f) % MathHelper.TwoPi;
                float sparkleRadius = 30 + (float)Math.Sin(lifeTimer * 3f + i) * 10;
                
                int sparkleX = rect.X + rect.Width / 2 + (int)(Math.Cos(sparkleAngle) * sparkleRadius);
                int sparkleY = rect.Y + rect.Height / 2 + (int)(Math.Sin(sparkleAngle) * sparkleRadius);
                
                float sparkleAlpha = (float)Math.Sin(lifeTimer * 5f + i) * 0.5f + 0.5f;
                Rectangle sparkleRect = new Rectangle(sparkleX, sparkleY, 3, 3);
                spriteBatch.Draw(pixel, sparkleRect, Color.White * sparkleAlpha);
            }
        }
    }
}
