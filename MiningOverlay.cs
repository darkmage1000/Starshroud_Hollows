using Claude4_5Terraria.Systems;
using Claude4_5Terraria.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Claude4_5Terraria.UI
{
    public class MiningOverlay
    {
        private World.World world;
        private MiningSystem miningSystem;
        private const int MINING_RANGE = 4;

        public MiningOverlay(World.World world, MiningSystem miningSystem)
        {
            this.world = world;
            this.miningSystem = miningSystem;
        }

        public void DrawBlockOutlines(SpriteBatch spriteBatch, Texture2D pixelTexture, Camera camera, Vector2 playerCenter, bool showOutlines)
        {
            if (!showOutlines) return;

            int tileSize = World.World.TILE_SIZE;
            float maxDistance = MINING_RANGE * tileSize;

            // Player tile position
            int playerTileX = (int)(playerCenter.X / tileSize);
            int playerTileY = (int)(playerCenter.Y / tileSize);

            // Loop around player in a small radius (efficient for small range)
            for (int dx = -MINING_RANGE; dx <= MINING_RANGE; dx++)
            {
                for (int dy = -MINING_RANGE; dy <= MINING_RANGE; dy++)
                {
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist <= MINING_RANGE)
                    {
                        int tileX = playerTileX + dx;
                        int tileY = playerTileY + dy;

                        Tile tile = world.GetTile(tileX, tileY);
                        if (tile != null && tile.IsActive)
                        {
                            // Check exact distance from player center
                            Vector2 tileCenter = new Vector2(
                                tileX * tileSize + tileSize / 2,
                                tileY * tileSize + tileSize / 2
                            );
                            float distance = Vector2.Distance(playerCenter, tileCenter);

                            if (distance <= maxDistance)
                            {
                                // Draw outline
                                Rectangle tileRect = new Rectangle(tileX * tileSize, tileY * tileSize, tileSize, tileSize);
                                DrawBorder(spriteBatch, pixelTexture, tileRect, 2, Color.Yellow);  // Thicker and fully opaque for visibility
                            }
                        }
                    }
                }
            }
        }

        public void DrawMiningProgress(SpriteBatch spriteBatch, Texture2D pixelTexture)
        {
            Point? miningTile = miningSystem.GetCurrentlyMiningTile();
            if (!miningTile.HasValue) return;

            float progress = miningSystem.GetMiningProgress();
            int tileSize = World.World.TILE_SIZE;

            // Draw cracks overlay
            DrawCracks(spriteBatch, pixelTexture, miningTile.Value, progress, tileSize);

            // Draw health bar above block
            DrawHealthBar(spriteBatch, pixelTexture, miningTile.Value, progress, tileSize);
        }

        private void DrawCracks(SpriteBatch spriteBatch, Texture2D pixelTexture, Point tile, float progress, int tileSize)
        {
            // Crack intensity based on damage
            Color crackColor = Color.Black * (progress * 0.6f);

            Rectangle tileRect = new Rectangle(
                tile.X * tileSize,
                tile.Y * tileSize,
                tileSize,
                tileSize
            );

            // Draw crack pattern (simple lines for now)
            if (progress > 0.2f)
            {
                // First crack
                DrawLine(spriteBatch, pixelTexture,
                    new Vector2(tileRect.Left + 5, tileRect.Top + 5),
                    new Vector2(tileRect.Right - 5, tileRect.Bottom - 5),
                    2, crackColor);
            }

            if (progress > 0.5f)
            {
                // Second crack
                DrawLine(spriteBatch, pixelTexture,
                    new Vector2(tileRect.Right - 5, tileRect.Top + 5),
                    new Vector2(tileRect.Left + 5, tileRect.Bottom - 5),
                    2, crackColor);
            }

            if (progress > 0.75f)
            {
                // Third crack (horizontal)
                DrawLine(spriteBatch, pixelTexture,
                    new Vector2(tileRect.Left + 5, tileRect.Center.Y),
                    new Vector2(tileRect.Right - 5, tileRect.Center.Y),
                    2, crackColor);
            }
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Texture2D pixelTexture, Point tile, float progress, int tileSize)
        {
            int barWidth = tileSize;
            int barHeight = 4;
            int barX = tile.X * tileSize;
            int barY = tile.Y * tileSize - 8;

            // Background (black with transparency)
            Rectangle barBg = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(pixelTexture, barBg, Color.Black * 0.7f);

            // Health remaining (green to yellow to red based on damage)
            float healthRemaining = 1f - progress;
            Color barColor = Color.Lerp(Color.Red, Color.Lime, healthRemaining);

            Rectangle barFill = new Rectangle(barX, barY, (int)(barWidth * healthRemaining), barHeight);
            spriteBatch.Draw(pixelTexture, barFill, barColor);

            // Border
            DrawBorder(spriteBatch, pixelTexture, barBg, 1, Color.White * 0.5f);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int thickness, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Texture2D pixelTexture, Vector2 start, Vector2 end, int thickness, Color color)
        {
            float length = Vector2.Distance(start, end);
            if (length == 0f) return;

            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            Vector2 origin = new Vector2(0.5f, 0.5f);  // Center of the 1x1 texture
            Vector2 midPoint = (start + end) / 2f;
            Vector2 scale = new Vector2(length, thickness);

            spriteBatch.Draw(pixelTexture, midPoint, null, color, angle, origin, scale, SpriteEffects.None, 0f);
        }
    }
}