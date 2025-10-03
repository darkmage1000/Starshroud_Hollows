using Claude4_5Terraria.Systems;
using Claude4_5Terraria.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Claude4_5Terraria.UI
{
    public class MiningOverlay
    {
        private World.World world;
        private MiningSystem miningSystem;

        public MiningOverlay(World.World world, MiningSystem miningSystem)
        {
            this.world = world;
            this.miningSystem = miningSystem;
        }

        public void DrawBlockOutlines(SpriteBatch spriteBatch, Texture2D pixelTexture, Camera camera, Vector2 playerCenter, bool showOutlines)
        {
            if (!showOutlines) return;

            int tileSize = World.World.TILE_SIZE;
            float maxDistance = 4 * tileSize; // 4 tile range

            // Get visible area
            Rectangle visibleArea = camera.GetVisibleArea(tileSize);

            for (int x = visibleArea.Left; x < visibleArea.Right; x++)
            {
                for (int y = visibleArea.Top; y < visibleArea.Bottom; y++)
                {
                    Tile tile = world.GetTile(x, y);
                    if (tile != null && tile.IsActive)
                    {
                        // Check distance from player
                        Vector2 tileCenter = new Vector2(
                            x * tileSize + tileSize / 2,
                            y * tileSize + tileSize / 2
                        );

                        float distance = Vector2.Distance(playerCenter, tileCenter);

                        if (distance <= maxDistance)
                        {
                            // Draw outline
                            Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                            DrawBorder(spriteBatch, pixelTexture, tileRect, 1, Color.Yellow * 0.5f);
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
            Vector2 edge = end - start;
            float angle = (float)System.Math.Atan2(edge.Y, edge.X);

            Rectangle lineRect = new Rectangle(
                (int)start.X,
                (int)start.Y,
                (int)edge.Length(),
                thickness
            );

            spriteBatch.Draw(pixelTexture, lineRect, null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}