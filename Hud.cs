using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Claude4_5Terraria.World;
using System;
using System.Collections.Generic;

namespace Claude4_5Terraria.UI
{
    public class HUD
    {
        private bool showBlockOutlines;
        private bool showMinimap = true;
        private float minimapOpacity = 0.7f;
        private const int MINIMAP_WIDTH = 200;
        private const int MINIMAP_HEIGHT = 400;
        private bool isMapFullscreen = false;
        public bool IsMapFullscreen => isMapFullscreen;

        private const int MINIMAP_ZOOM_RADIUS = 50;

        private const int BAR_WIDTH = 200;
        private const int BAR_HEIGHT = 18;
        private const int BAR_START_X = 10;
        private const int BAR_START_Y = 10;

        private Texture2D minimapTexture;
        private Color[] minimapColorData;
        private bool minimapNeedsUpdate = true;

        public HUD()
        {
            showBlockOutlines = false;
        }

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            int worldW = World.World.WORLD_WIDTH;
            int worldH = World.World.WORLD_HEIGHT;

            minimapTexture = new Texture2D(graphicsDevice, worldW, worldH);
            minimapColorData = new Color[worldW * worldH];

            for (int i = 0; i < minimapColorData.Length; i++)
            {
                minimapColorData[i] = Color.Transparent;
            }

            minimapNeedsUpdate = true;
        }

        public void FlagMinimapUpdate()
        {
            minimapNeedsUpdate = true;
        }

        public void ToggleBlockOutlines()
        {
            showBlockOutlines = !showBlockOutlines;
        }

        public bool ShowBlockOutlines => showBlockOutlines;

        public bool ShowMinimap { get => showMinimap; set => showMinimap = value; }
        public float MinimapOpacity { get => minimapOpacity; set => minimapOpacity = MathHelper.Clamp(value, 0f, 1f); }
        public void ToggleMinimap() { showMinimap = !showMinimap; }

        public void ToggleFullscreenMap()
        {
            isMapFullscreen = !isMapFullscreen;
            showMinimap = !isMapFullscreen;
        }

        public void UpdateMinimapData(Claude4_5Terraria.World.World world)
        {
            if (minimapTexture == null || !minimapNeedsUpdate) return;

            int worldW = World.World.WORLD_WIDTH;
            int worldH = World.World.WORLD_HEIGHT;

            foreach (Point tilePos in world.ExploredTiles)
            {
                int x = tilePos.X;
                int y = tilePos.Y;

                if (x >= 0 && x < worldW && y >= 0 && y < worldH)
                {
                    int index = y * worldW + x;

                    if (index >= 0 && index < minimapColorData.Length)
                    {
                        Tile tile = world.GetTile(x, y);

                        if (tile != null && tile.IsActive)
                        {
                            minimapColorData[index] = world.GetTileColor(tile.Type);
                        }
                        else
                        {
                            minimapColorData[index] = Color.Black * 0.15f;
                        }
                    }
                }
            }

            minimapTexture.SetData(minimapColorData);
            minimapNeedsUpdate = false;
        }

        public void DrawMinimap(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight, Vector2 playerPosition, Claude4_5Terraria.World.World world)
        {
            int playerTileX = (int)(playerPosition.X / World.World.TILE_SIZE);
            int playerTileY = (int)(playerPosition.Y / World.World.TILE_SIZE);
            float scaleX, scaleY;
            int playerScreenX, playerScreenY;
            Rectangle playerDot;

            int sourceTileSize = MINIMAP_ZOOM_RADIUS * 2;
            int sourceX = playerTileX - MINIMAP_ZOOM_RADIUS;
            int sourceY = playerTileY - MINIMAP_ZOOM_RADIUS;
            Rectangle sourceRect = new Rectangle(sourceX, sourceY, sourceTileSize, sourceTileSize);


            if (isMapFullscreen)
            {
                // Draw a very low opacity background overlay so the game is still visible
                spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.1f);

                Rectangle fullScreenRect = new Rectangle(0, 0, screenWidth, screenHeight);

                spriteBatch.Draw(minimapTexture, fullScreenRect, sourceRect, Color.White * minimapOpacity);

                int dotSize = 10;
                playerScreenX = (screenWidth / 2);
                playerScreenY = (screenHeight / 2);

                playerDot = new Rectangle(playerScreenX - dotSize / 2, playerScreenY - dotSize / 2, dotSize, dotSize);
                spriteBatch.Draw(pixelTexture, playerDot, Color.Red);

                return;
            }

            // --- Small Minimap (Default Zoomed View) ---
            if (!showMinimap || minimapTexture == null) return;

            Rectangle minimapRect = new Rectangle(10, screenHeight - MINIMAP_HEIGHT - 10, MINIMAP_WIDTH, MINIMAP_HEIGHT);

            spriteBatch.Draw(minimapTexture, minimapRect, sourceRect, Color.White * minimapOpacity);

            DrawBorder(spriteBatch, pixelTexture, minimapRect, 2, Color.White * minimapOpacity);

            playerScreenX = minimapRect.X + (int)(MINIMAP_WIDTH * 0.5f);
            playerScreenY = minimapRect.Y + (int)(MINIMAP_HEIGHT * 0.5f);

            playerDot = new Rectangle(playerScreenX - 2, playerScreenY - 2, 4, 4);
            spriteBatch.Draw(pixelTexture, playerDot, Color.Red);

            // Opacity label
            string opacityText = $"Opacity: {(int)(minimapOpacity * 100)}%";
            Vector2 textSize = font.MeasureString(opacityText);
            Vector2 textPos = new Vector2(minimapRect.X + (MINIMAP_WIDTH - textSize.X) / 2, minimapRect.Y - 20);
            spriteBatch.DrawString(font, opacityText, textPos, Color.White * minimapOpacity);

        }

        private void DrawBars(SpriteBatch spriteBatch, Texture2D pixelTexture, float currentHealth, float maxHealth, float currentAir, float maxAir)
        {
            // --- Health Bar (Red) ---
            float healthPercent = currentHealth / maxHealth;
            int healthFillWidth = (int)(BAR_WIDTH * healthPercent);

            Rectangle healthBgRect = new Rectangle(BAR_START_X, BAR_START_Y, BAR_WIDTH, BAR_HEIGHT);
            Rectangle healthFillRect = new Rectangle(BAR_START_X, BAR_START_Y, healthFillWidth, BAR_HEIGHT);

            // Background (Dark Red)
            spriteBatch.Draw(pixelTexture, healthBgRect, Color.DarkRed);
            // Health fill (Red) - based on current health
            spriteBatch.Draw(pixelTexture, healthFillRect, Color.Red);
            DrawBorder(spriteBatch, pixelTexture, healthBgRect, 1, Color.White);

            // --- Mana Bar (Blue) ---
            int manaY = BAR_START_Y + BAR_HEIGHT + 4;
            Rectangle manaBgRect = new Rectangle(BAR_START_X, manaY, BAR_WIDTH, BAR_HEIGHT);
            Rectangle manaFillRect = new Rectangle(BAR_START_X, manaY, BAR_WIDTH, BAR_HEIGHT);

            // Background (Dark Blue)
            spriteBatch.Draw(pixelTexture, manaBgRect, Color.DarkBlue);
            // Full Mana (Blue) - Not implemented yet, so full bar
            spriteBatch.Draw(pixelTexture, manaFillRect, Color.Blue);
            DrawBorder(spriteBatch, pixelTexture, manaBgRect, 1, Color.White);

            // --- Air Bubbles Bar (Cyan) - Only show when underwater ---
            if (currentAir < maxAir)
            {
                float airPercent = currentAir / maxAir;
                int airFillWidth = (int)(BAR_WIDTH * airPercent);

                int airY = manaY + BAR_HEIGHT + 4;
                Rectangle airBgRect = new Rectangle(BAR_START_X, airY, BAR_WIDTH, BAR_HEIGHT);
                Rectangle airFillRect = new Rectangle(BAR_START_X, airY, airFillWidth, BAR_HEIGHT);

                // Background (Dark Cyan)
                spriteBatch.Draw(pixelTexture, airBgRect, new Color(0, 100, 100));
                // Air fill (Cyan)
                spriteBatch.Draw(pixelTexture, airFillRect, Color.Cyan);
                DrawBorder(spriteBatch, pixelTexture, airBgRect, 1, Color.White);
            }
        }

        // UPDATED: Added bed/sleep parameters
        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight, Vector2 playerPosition, Claude4_5Terraria.World.World world, bool isAutoMiningActive, bool isRaining, float playerHealth, float playerMaxHealth, float playerAir, float playerMaxAir, Point? currentBedPosition, bool isSleeping, float sleepProgress, float sleepDuration)
        {
            if (font == null) return;

            var player = world.GetPlayer();

            // Draw Health and Mana Bars at the very top-left
            DrawBars(spriteBatch, pixelTexture, playerHealth, playerMaxHealth, playerAir, playerMaxAir);

            // --- Draw coordinates (Moved down to account for bars) ---
            int tileX = (int)(playerPosition.X / World.World.TILE_SIZE);
            int tileY = (int)(playerPosition.Y / World.World.TILE_SIZE);
            string coordText = $"X: {tileX}, Y: {tileY}";
            Vector2 coordSize = font.MeasureString(coordText);
            Vector2 coordPosition = new Vector2(BAR_START_X, BAR_START_Y + BAR_HEIGHT * 2 + 15);

            // Background for coordinates
            Rectangle coordBgRect = new Rectangle(
                (int)coordPosition.X - 5,
                (int)coordPosition.Y - 5,
                (int)coordSize.X + 10,
                (int)coordSize.Y + 10

            );
            spriteBatch.Draw(pixelTexture, coordBgRect, Color.Black * 0.7f);
            spriteBatch.DrawString(font, coordText, coordPosition, Color.Yellow);

            // --- Draw Weather Status (Below coordinates) ---
            string weatherText = isRaining ? "Weather: RAIN (R)" : "Weather: CLEAR (C)";
            Color weatherColor = isRaining ? Color.SkyBlue : Color.White;
            Vector2 weatherSize = font.MeasureString(weatherText);

            Vector2 weatherPosition = new Vector2(
                coordPosition.X,
                coordPosition.Y + coordSize.Y + 5
            );

            // Background
            Rectangle weatherBgRect = new Rectangle(
                (int)weatherPosition.X - 5,
                (int)weatherPosition.Y - 5,
                (int)weatherSize.X + 10,
                (int)weatherSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, weatherBgRect, Color.Black * 0.7f);

            // Text
            spriteBatch.DrawString(font, weatherText, weatherPosition, weatherColor);

            // --- Draw block outline hint in top-right ---
            string outlineHint = showBlockOutlines ? "T: Hide Outlines" : "T: Show Outlines";
            Vector2 outlineSize = font.MeasureString(outlineHint);
            Vector2 outlinePosition = new Vector2(screenWidth - outlineSize.X - 20, 10);

            // Background
            Rectangle outlineBgRect = new Rectangle(
                (int)outlinePosition.X - 10,
                (int)outlinePosition.Y - 5,
                (int)outlineSize.X + 20,
                (int)outlineSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, outlineBgRect, Color.Black * 0.6f);

            // Text
            spriteBatch.DrawString(font, outlineHint, outlinePosition, Color.White);

            // --- Draw Auto-Mine Status in top-right, just below outlines ---
            string autoMineStatus = $"L: Auto-Mine {(isAutoMiningActive ? "ON" : "OFF")}";
            Color autoMineColor = isAutoMiningActive ? Color.LimeGreen : Color.Gray;
            Vector2 autoMineSize = font.MeasureString(autoMineStatus);
            Vector2 autoMinePosition = new Vector2(screenWidth - autoMineSize.X - 20, outlinePosition.Y + outlineSize.Y + 5);

            // Background
            Rectangle autoMineBgRect = new Rectangle(
                (int)autoMinePosition.X - 10,
                (int)autoMinePosition.Y - 5,
                (int)autoMineSize.X + 20,
                (int)autoMineSize.Y + 10
            );
            spriteBatch.Draw(pixelTexture, autoMineBgRect, Color.Black * 0.6f);

            // Text
            spriteBatch.DrawString(font, autoMineStatus, autoMinePosition, autoMineColor);

            // --- NEW: Draw Bed Spawn Message ---
            if (player.GetLastSpawnSetTime() + TimeSpan.FromSeconds(player.GetSpawnMessageDuration()) > DateTime.Now)
            {
                string spawnMsg = "Spawn Point Set!";
                Vector2 msgSize = font.MeasureString(spawnMsg);
                Vector2 msgPosition = new Vector2((screenWidth - msgSize.X) / 2, (screenHeight / 2) - 100);

                Rectangle msgBgRect = new Rectangle(
                    (int)msgPosition.X - 10, (int)msgPosition.Y - 5, (int)msgSize.X + 20, (int)msgSize.Y + 10
                );
                spriteBatch.Draw(pixelTexture, msgBgRect, Color.Black * 0.7f);
                spriteBatch.DrawString(font, spawnMsg, msgPosition, Color.LimeGreen);
            }

            // --- NEW: Draw Sleep Progress Bar (if sleeping) ---
            if (isSleeping && currentBedPosition.HasValue)
            {
                float progress = sleepProgress / sleepDuration;
                int barW = 300;
                int barH = 20;
                int barX = (screenWidth - barW) / 2;
                int barY = (screenHeight - barH) / 2;

                Rectangle barBg = new Rectangle(barX, barY, barW, barH);
                spriteBatch.Draw(pixelTexture, barBg, Color.Black * 0.8f);

                int fillW = (int)(barW * progress);
                Rectangle barFill = new Rectangle(barX, barY, fillW, barH);
                spriteBatch.Draw(pixelTexture, barFill, Color.LightBlue);
                DrawBorder(spriteBatch, pixelTexture, barBg, 2, Color.White);

                string sleepText = $"Sleeping... {progress * 100:F0}%";
                Vector2 textSz = font.MeasureString(sleepText);
                Vector2 textPos = new Vector2(barX + (barW - textSz.X) / 2, barY + (barH - textSz.Y) / 2);
                spriteBatch.DrawString(font, sleepText, textPos, Color.White);
            }

            // --- NEW: Draw Bed Hover/Spawn Hint ---
            if (player.HasBedSpawn)
            {
                string hintMsg = "Spawn: BED. Right-click on bed to set spawn / sleep to skip night.";
                Vector2 hintSize = font.MeasureString(hintMsg);
                Vector2 hintPosition = new Vector2((screenWidth - hintSize.X) / 2, screenHeight - 50);

                Rectangle hintBgRect = new Rectangle(
                    (int)hintPosition.X - 10, (int)hintPosition.Y - 5, (int)hintSize.X + 20, (int)hintSize.Y + 10
                );
                spriteBatch.Draw(pixelTexture, hintBgRect, Color.Black * 0.7f);
                spriteBatch.DrawString(font, hintMsg, hintPosition, Color.LightBlue);
            }

            // Draw minimap in bottom left
            DrawMinimap(spriteBatch, pixelTexture, font, screenWidth, screenHeight, playerPosition, world);
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