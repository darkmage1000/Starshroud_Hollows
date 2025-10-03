using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Claude4_5Terraria.UI
{
    public class PauseMenu
    {
        private bool isPaused;
        private KeyboardState previousKeyState;

        public PauseMenu()
        {
            isPaused = false;
            previousKeyState = Keyboard.GetState();
        }

        public bool IsPaused => isPaused;

        public void Update()
        {
            KeyboardState currentKeyState = Keyboard.GetState();

            if (currentKeyState.IsKeyDown(Keys.Escape) && previousKeyState.IsKeyUp(Keys.Escape))
            {
                isPaused = !isPaused;
            }

            previousKeyState = currentKeyState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isPaused) return;

            // Dark overlay
            Rectangle overlay = new Rectangle(0, 0, screenWidth, screenHeight);
            spriteBatch.Draw(pixelTexture, overlay, Color.Black * 0.7f);

            // Menu background
            int menuWidth = 500;
            int menuHeight = 550;
            Rectangle menuBg = new Rectangle(
                (screenWidth - menuWidth) / 2,
                (screenHeight - menuHeight) / 2,
                menuWidth,
                menuHeight
            );
            spriteBatch.Draw(pixelTexture, menuBg, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, menuBg, 3, Color.White);

            if (font == null) return;

            // Title
            string title = "PAUSED";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                menuBg.X + (menuWidth - titleSize.X) / 2,
                menuBg.Y + 30
            );
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            // Controls text
            int yPos = menuBg.Y + 100;
            int lineHeight = 30;

            DrawTextLine(spriteBatch, font, menuBg.X + 40, yPos, "MOVEMENT", Color.Yellow);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "W - Jump", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "A - Move Left", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "D - Move Right", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "S - Fast Fall", Color.White);
            yPos += lineHeight + 15;

            DrawTextLine(spriteBatch, font, menuBg.X + 40, yPos, "MINING & BUILDING", Color.Yellow);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "Left Mouse - Mine Block", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "Right Mouse - Place Block", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "T - Toggle Block Outlines", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "1-0 - Select Hotbar Slot", Color.White);
            yPos += lineHeight + 15;

            DrawTextLine(spriteBatch, font, menuBg.X + 40, yPos, "MENU", Color.Yellow);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "ESC - Pause Menu", Color.White);

            // Footer
            yPos = menuBg.Bottom - 50;
            string footerText = "Press ESC to resume";
            Vector2 footerSize = font.MeasureString(footerText);
            Vector2 footerPos = new Vector2(
                menuBg.X + (menuWidth - footerSize.X) / 2,
                yPos
            );
            spriteBatch.DrawString(font, footerText, footerPos, Color.LightGray);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int thickness, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawTextLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
        {
            spriteBatch.DrawString(font, text, new Vector2(x, y), color);
        }
    }
}