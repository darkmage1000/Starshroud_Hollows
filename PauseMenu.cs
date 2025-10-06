using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Claude4_5Terraria.UI
{
    public class PauseMenu
    {
        private bool isPaused;
        private KeyboardState previousKeyState;
        private MouseState previousMouseState;
        private Action onOpenSaveMenu;
        private Action onQuitToMenu;

        private Rectangle saveButton;
        private Rectangle quitButton;
        private bool saveButtonHovered;
        private bool quitButtonHovered;

        public PauseMenu(Action onOpenSaveMenu = null, Action onQuitToMenu = null)
        {
            isPaused = false;
            previousKeyState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            this.onOpenSaveMenu = onOpenSaveMenu;
            this.onQuitToMenu = onQuitToMenu;
        }

        public bool IsPaused => isPaused;

        public void Update()
        {
            KeyboardState currentKeyState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            if (currentKeyState.IsKeyDown(Keys.Escape) && previousKeyState.IsKeyUp(Keys.Escape))
            {
                isPaused = !isPaused;
            }

            if (isPaused)
            {
                Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
                saveButtonHovered = saveButton.Contains(mousePoint);
                quitButtonHovered = quitButton.Contains(mousePoint);

                if (currentMouseState.LeftButton == ButtonState.Pressed &&
                    previousMouseState.LeftButton == ButtonState.Released)
                {
                    if (saveButtonHovered)
                    {
                        onOpenSaveMenu?.Invoke();
                    }
                    else if (quitButtonHovered)
                    {
                        onQuitToMenu?.Invoke();
                    }
                }
            }

            previousKeyState = currentKeyState;
            previousMouseState = currentMouseState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isPaused) return;

            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.5f);

            int menuWidth = 600;
            int menuHeight = 550;
            Rectangle menuBg = new Rectangle(
                (screenWidth - menuWidth) / 2,
                (screenHeight - menuHeight) / 2,
                menuWidth,
                menuHeight
            );

            spriteBatch.Draw(pixelTexture, menuBg, new Color(20, 20, 30));
            DrawBorder(spriteBatch, pixelTexture, menuBg, 4, Color.White);

            string title = "PAUSED";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                menuBg.X + (menuWidth - titleSize.X) / 2,
                menuBg.Y + 30
            );
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Yellow);

            int yPos = menuBg.Y + 100;
            int lineHeight = 30;

            DrawTextLine(spriteBatch, font, menuBg.X + 40, yPos, "CONTROLS", Color.Cyan);
            yPos += lineHeight + 10;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "A/D - Move Left/Right", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "SPACE - Jump", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "Left Click - Mine", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "Right Click - Place", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "E - Open Inventory", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "1-9 - Select Hotbar", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "T - Toggle Mining Outlines", Color.White);
            yPos += lineHeight + 20;

            DrawTextLine(spriteBatch, font, menuBg.X + 40, yPos, "MUSIC", Color.Cyan);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "+/- Keys - Volume", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "M - Mute/Unmute", Color.White);
            yPos += lineHeight + 30;

            // Button dimensions
            int buttonWidth = 200;
            int buttonHeight = 50;
            int buttonSpacing = 20;

            // Save Game button
            saveButton = new Rectangle(
                menuBg.X + (menuWidth - buttonWidth) / 2,
                yPos,
                buttonWidth,
                buttonHeight
            );

            Color saveButtonColor = saveButtonHovered ? Color.DarkGreen : Color.DarkSlateGray;
            spriteBatch.Draw(pixelTexture, saveButton, saveButtonColor);
            DrawBorder(spriteBatch, pixelTexture, saveButton, 3, saveButtonHovered ? Color.Lime : Color.Gray);

            string saveText = "SAVE GAME";
            Vector2 saveTextSize = font.MeasureString(saveText);
            Vector2 saveTextPos = new Vector2(
                saveButton.X + (saveButton.Width - saveTextSize.X) / 2,
                saveButton.Y + (saveButton.Height - saveTextSize.Y) / 2
            );
            spriteBatch.DrawString(font, saveText, saveTextPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, saveText, saveTextPos, Color.White);

            // Quit to Menu button
            yPos += buttonHeight + buttonSpacing;
            quitButton = new Rectangle(
                menuBg.X + (menuWidth - buttonWidth) / 2,
                yPos,
                buttonWidth,
                buttonHeight
            );

            Color quitButtonColor = quitButtonHovered ? Color.DarkRed : Color.DarkSlateGray;
            spriteBatch.Draw(pixelTexture, quitButton, quitButtonColor);
            DrawBorder(spriteBatch, pixelTexture, quitButton, 3, quitButtonHovered ? Color.Red : Color.Gray);

            string quitText = "QUIT TO MENU";
            Vector2 quitTextSize = font.MeasureString(quitText);
            Vector2 quitTextPos = new Vector2(
                quitButton.X + (quitButton.Width - quitTextSize.X) / 2,
                quitButton.Y + (quitButton.Height - quitTextSize.Y) / 2
            );
            spriteBatch.DrawString(font, quitText, quitTextPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, quitText, quitTextPos, Color.White);

            string escText = "Press ESC to Resume";
            Vector2 escSize = font.MeasureString(escText);
            Vector2 escPos = new Vector2(
                menuBg.X + (menuWidth - escSize.X) / 2,
                menuBg.Bottom - 50
            );
            spriteBatch.DrawString(font, escText, escPos, Color.Gray);
        }

        private void DrawTextLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
        {
            Vector2 pos = new Vector2(x, y);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, text, pos, color);
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