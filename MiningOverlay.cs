using StarshroudHollows.Systems;
using StarshroudHollows.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace StarshroudHollows.UI
{
    public class MiningOverlay
    {
        private StarshroudHollows.World.World world;
        private MiningSystem miningSystem;
        private ChestSystem chestSystem;
        private const int MINING_RANGE = 4;

        public MiningOverlay(StarshroudHollows.World.World world, MiningSystem miningSystem, ChestSystem chestSystem)
        {
            this.world = world;
            this.miningSystem = miningSystem;
            this.chestSystem = chestSystem;
        }

        public void DrawBlockOutlines(SpriteBatch spriteBatch, Texture2D pixelTexture, Camera camera, Vector2 playerCenter, bool showOutlines)
        {
            if (!showOutlines) return;

            int tileSize = StarshroudHollows.World.World.TILE_SIZE;
            
            // Get visible area
            Rectangle visibleArea = camera.GetVisibleArea(tileSize);
            int startTileX = Math.Max(0, visibleArea.Left / tileSize);
            int endTileX = Math.Min(StarshroudHollows.World.World.WORLD_WIDTH - 1, visibleArea.Right / tileSize);
            int startTileY = Math.Max(0, visibleArea.Top / tileSize);
            int endTileY = Math.Min(StarshroudHollows.World.World.WORLD_HEIGHT - 1, visibleArea.Bottom / tileSize);
            
            // Optimize: Only draw every other tile for grid effect (reduces draw calls by 50%)
            for (int x = startTileX; x <= endTileX; x++)
            {
                for (int y = startTileY; y <= endTileY; y++)
                {
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    
                    // Get tile
                    Tile tile = world.GetTile(x, y);
                    
                    // Check if tile is in mining range
                    Vector2 tileCenter = new Vector2(x * tileSize + tileSize / 2, y * tileSize + tileSize / 2);
                    float distance = Vector2.Distance(playerCenter, tileCenter);
                    bool inMiningRange = distance <= MINING_RANGE * tileSize;
                    
                    // Choose color based on state
                    Color gridColor;
                    if (tile != null && tile.IsActive)
                    {
                        // Solid block - only show if in range
                        if (inMiningRange)
                        {
                            gridColor = Color.Yellow * 0.5f; // Bright yellow for mineable blocks
                            DrawBorder(spriteBatch, pixelTexture, tileRect, 1, gridColor);
                        }
                    }
                    else
                    {
                        // Empty space - show placement grid
                        if (inMiningRange)
                        {
                            gridColor = Color.Cyan * 0.3f; // Cyan for placeable spaces
                            DrawBorder(spriteBatch, pixelTexture, tileRect, 1, gridColor);
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
            int tileSize = StarshroudHollows.World.World.TILE_SIZE;

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

        // NEW: Draw chest name tooltip when hovering over a chest
        public void DrawChestTooltip(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, Camera camera, Vector2 mouseScreenPos)
        {
            // Convert mouse screen position to world position
            Vector2 worldPos = camera.ScreenToWorld(mouseScreenPos);
            int tileX = (int)(worldPos.X / StarshroudHollows.World.World.TILE_SIZE);
            int tileY = (int)(worldPos.Y / StarshroudHollows.World.World.TILE_SIZE);

            // Check if hovering over a chest tile
            Tile tile = world.GetTile(tileX, tileY);
            if (tile != null && (tile.Type == Enums.TileType.WoodChest ||
                tile.Type == Enums.TileType.SilverChest ||
                tile.Type == Enums.TileType.MagicChest))
            {
                // Get the chest at this position
                Point tilePos = new Point(tileX, tileY);
                Chest chest = chestSystem.GetChest(tilePos);
                
                if (chest != null)
                {
                    // Draw tooltip near mouse cursor
                    string tooltipText = chest.Name;
                    Vector2 textSize = font.MeasureString(tooltipText);
                    
                    // Position tooltip slightly offset from cursor
                    Vector2 tooltipPos = new Vector2(mouseScreenPos.X + 15, mouseScreenPos.Y + 15);
                    
                    // Create background rectangle with padding
                    int padding = 8;
                    Rectangle tooltipBg = new Rectangle(
                        (int)tooltipPos.X - padding,
                        (int)tooltipPos.Y - padding,
                        (int)textSize.X + padding * 2,
                        (int)textSize.Y + padding * 2
                    );
                    
                    // Draw tooltip background
                    spriteBatch.Draw(pixelTexture, tooltipBg, Color.Black * 0.9f);
                    DrawBorder(spriteBatch, pixelTexture, tooltipBg, 2, Color.Gold);
                    
                    // Draw chest name text
                    spriteBatch.DrawString(font, tooltipText, tooltipPos, Color.White);
                }
            }
        }
    }
}