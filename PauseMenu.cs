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
        private Action<float> onVolumeChange;
        private Action onToggleFullscreen;

        private PauseMenuState currentState;
        private enum PauseMenuState
        {
            Main,
            Controls,
            Music
        }

        // Main menu buttons
        private Rectangle controlsButton;
        private Rectangle musicButton;
        private Rectangle fullscreenButton;
        private Rectangle saveButton;
        private Rectangle quitButton;

        // Back button (for sub-menus)
        private Rectangle backButton;

        // Volume controls
        private Rectangle volumeSlider;
        private Rectangle volumeFill;
        private Rectangle muteButton;
        private bool isDraggingVolume;

        private bool controlsButtonHovered;
        private bool musicButtonHovered;
        private bool fullscreenButtonHovered;
        private bool saveButtonHovered;
        private bool quitButtonHovered;
        private bool backButtonHovered;
        private bool muteButtonHovered;

        private float currentVolume;
        private bool isMuted;

        public PauseMenu(Action onOpenSaveMenu = null, Action onQuitToMenu = null, Action<float> onVolumeChange = null, float initialVolume = 0.1f, Action onToggleFullscreen = null)
        {
            isPaused = false;
            previousKeyState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            this.onOpenSaveMenu = onOpenSaveMenu;
            this.onQuitToMenu = onQuitToMenu;
            this.onVolumeChange = onVolumeChange;
            this.onToggleFullscreen = onToggleFullscreen;
            currentState = PauseMenuState.Main;
            currentVolume = initialVolume;
            isMuted = false;
            isDraggingVolume = false;
        }

        public bool IsPaused => isPaused;

        public void Update()
        {
            KeyboardState currentKeyState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            if (currentKeyState.IsKeyDown(Keys.Escape) && previousKeyState.IsKeyUp(Keys.Escape))
            {
                if (currentState != PauseMenuState.Main)
                {
                    currentState = PauseMenuState.Main;
                }
                else
                {
                    isPaused = !isPaused;
                }
            }

            if (isPaused)
            {
                Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);

                if (currentState == PauseMenuState.Main)
                {
                    UpdateMainMenu(mousePoint, currentMouseState);
                }
                else if (currentState == PauseMenuState.Controls)
                {
                    UpdateControlsMenu(mousePoint, currentMouseState);
                }
                else if (currentState == PauseMenuState.Music)
                {
                    UpdateMusicMenu(mousePoint, currentMouseState);
                }
            }

            previousKeyState = currentKeyState;
            previousMouseState = currentMouseState;
        }

        private void UpdateMainMenu(Point mousePoint, MouseState currentMouseState)
        {
            controlsButtonHovered = controlsButton.Contains(mousePoint);
            musicButtonHovered = musicButton.Contains(mousePoint);
            fullscreenButtonHovered = fullscreenButton.Contains(mousePoint);
            saveButtonHovered = saveButton.Contains(mousePoint);
            quitButtonHovered = quitButton.Contains(mousePoint);

            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                if (controlsButtonHovered)
                {
                    currentState = PauseMenuState.Controls;
                }
                else if (musicButtonHovered)
                {
                    currentState = PauseMenuState.Music;
                }
                else if (fullscreenButtonHovered)
                {
                    onToggleFullscreen?.Invoke();
                }
                else if (saveButtonHovered)
                {
                    onOpenSaveMenu?.Invoke();
                }
                else if (quitButtonHovered)
                {
                    onQuitToMenu?.Invoke();
                }
            }
        }

        private void UpdateControlsMenu(Point mousePoint, MouseState currentMouseState)
        {
            backButtonHovered = backButton.Contains(mousePoint);

            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                if (backButtonHovered)
                {
                    currentState = PauseMenuState.Main;
                }
            }
        }

        private void UpdateMusicMenu(Point mousePoint, MouseState currentMouseState)
        {
            backButtonHovered = backButton.Contains(mousePoint);
            muteButtonHovered = muteButton.Contains(mousePoint);

            // Only start dragging if user clicks on the slider (not just hovers)
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                volumeSlider.Contains(mousePoint))
            {
                isDraggingVolume = true;
            }

            // Continue dragging if already dragging
            if (isDraggingVolume && currentMouseState.LeftButton == ButtonState.Pressed)
            {
                float relativeX = mousePoint.X - volumeSlider.X;
                currentVolume = MathHelper.Clamp(relativeX / volumeSlider.Width, 0f, 1f);
                onVolumeChange?.Invoke(isMuted ? 0f : currentVolume);
            }

            // Stop dragging when mouse button is released
            if (currentMouseState.LeftButton == ButtonState.Released)
            {
                isDraggingVolume = false;
            }

            // Handle button clicks
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                if (backButtonHovered)
                {
                    currentState = PauseMenuState.Main;
                }
                else if (muteButtonHovered)
                {
                    isMuted = !isMuted;
                    onVolumeChange?.Invoke(isMuted ? 0f : currentVolume);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isPaused) return;

            // Dark overlay
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.5f);

            int menuWidth = 600;
            int menuHeight = 500;
            Rectangle menuBg = new Rectangle(
                (screenWidth - menuWidth) / 2,
                (screenHeight - menuHeight) / 2,
                menuWidth,
                menuHeight
            );

            spriteBatch.Draw(pixelTexture, menuBg, new Color(20, 20, 30));
            DrawBorder(spriteBatch, pixelTexture, menuBg, 4, Color.White);

            if (currentState == PauseMenuState.Main)
            {
                DrawMainMenu(spriteBatch, pixelTexture, font, menuBg, screenWidth);
            }
            else if (currentState == PauseMenuState.Controls)
            {
                DrawControlsMenu(spriteBatch, pixelTexture, font, menuBg);
            }
            else if (currentState == PauseMenuState.Music)
            {
                DrawMusicMenu(spriteBatch, pixelTexture, font, menuBg);
            }
        }

        private void DrawMainMenu(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, Rectangle menuBg, int screenWidth)
        {
            // Title
            string title = "PAUSED";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(menuBg.X + (menuBg.Width - titleSize.X) / 2, menuBg.Y + 30);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Yellow);

            // Button layout
            int buttonWidth = 250;
            int buttonHeight = 50;
            int buttonSpacing = 20;
            int startY = menuBg.Y + 120;

            // Controls Button
            controlsButton = new Rectangle(
                menuBg.X + (menuBg.Width - buttonWidth) / 2,
                startY,
                buttonWidth,
                buttonHeight
            );
            DrawButton(spriteBatch, pixelTexture, font, controlsButton, "CONTROLS",
                controlsButtonHovered ? Color.DarkCyan : Color.DarkSlateGray,
                controlsButtonHovered ? Color.Cyan : Color.Gray);

            // Music Button
            musicButton = new Rectangle(
                menuBg.X + (menuBg.Width - buttonWidth) / 2,
                startY + buttonHeight + buttonSpacing,
                buttonWidth,
                buttonHeight
            );
            DrawButton(spriteBatch, pixelTexture, font, musicButton, "MUSIC",
                musicButtonHovered ? Color.DarkMagenta : Color.DarkSlateGray,
                musicButtonHovered ? Color.Magenta : Color.Gray);

            // Fullscreen Button
            fullscreenButton = new Rectangle(
                menuBg.X + (menuBg.Width - buttonWidth) / 2,
                startY + (buttonHeight + buttonSpacing) * 2,
                buttonWidth,
                buttonHeight
            );
            DrawButton(spriteBatch, pixelTexture, font, fullscreenButton, "FULLSCREEN",
                fullscreenButtonHovered ? Color.DarkBlue : Color.DarkSlateGray,
                fullscreenButtonHovered ? Color.Cyan : Color.Gray);

            // Save Game Button
            saveButton = new Rectangle(
                menuBg.X + (menuBg.Width - buttonWidth) / 2,
                startY + (buttonHeight + buttonSpacing) * 3,
                buttonWidth,
                buttonHeight
            );
            DrawButton(spriteBatch, pixelTexture, font, saveButton, "SAVE GAME",
                saveButtonHovered ? Color.DarkGreen : Color.DarkSlateGray,
                saveButtonHovered ? Color.Lime : Color.Gray);

            // Quit to Menu Button
            quitButton = new Rectangle(
                menuBg.X + (menuBg.Width - buttonWidth) / 2,
                startY + (buttonHeight + buttonSpacing) * 4,
                buttonWidth,
                buttonHeight
            );
            DrawButton(spriteBatch, pixelTexture, font, quitButton, "QUIT TO MENU",
                quitButtonHovered ? Color.DarkRed : Color.DarkSlateGray,
                quitButtonHovered ? Color.Red : Color.Gray);

            // Instructions at bottom
            string escText = "Press ESC to Resume";
            Vector2 escSize = font.MeasureString(escText);
            Vector2 escPos = new Vector2(
                menuBg.X + (menuBg.Width - escSize.X) / 2,
                menuBg.Bottom - 40
            );
            spriteBatch.DrawString(font, escText, escPos, Color.Gray);
        }

        private void DrawControlsMenu(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, Rectangle menuBg)
        {
            // Title
            string title = "CONTROLS";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(menuBg.X + (menuBg.Width - titleSize.X) / 2, menuBg.Y + 30);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Cyan);

            int yPos = menuBg.Y + 100;
            int lineHeight = 30;

            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "A/D - Move Left/Right", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "W - Jump", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "S - Fast Fall", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "Left Click - Mine", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "Right Click - Place", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "E - Open Inventory", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "1-9 - Select Hotbar Slot", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "T - Toggle Mining Outlines", Color.White);
            yPos += lineHeight;
            DrawTextLine(spriteBatch, font, menuBg.X + 60, yPos, "ESC - Pause Menu", Color.White);

            // Back button
            backButton = new Rectangle(
                menuBg.X + (menuBg.Width - 200) / 2,
                menuBg.Bottom - 70,
                200,
                50
            );
            DrawButton(spriteBatch, pixelTexture, font, backButton, "BACK",
                backButtonHovered ? Color.DarkGray : Color.DarkSlateGray,
                backButtonHovered ? Color.White : Color.Gray);
        }

        private void DrawMusicMenu(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, Rectangle menuBg)
        {
            // Title
            string title = "MUSIC";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(menuBg.X + (menuBg.Width - titleSize.X) / 2, menuBg.Y + 30);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Magenta);

            int centerY = menuBg.Y + 180;

            // Volume label
            string volumeLabel = "Volume";
            Vector2 volumeLabelSize = font.MeasureString(volumeLabel);
            Vector2 volumeLabelPos = new Vector2(menuBg.X + (menuBg.Width - volumeLabelSize.X) / 2, centerY);
            spriteBatch.DrawString(font, volumeLabel, volumeLabelPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, volumeLabel, volumeLabelPos, Color.White);

            // Volume slider
            int sliderWidth = 400;
            int sliderHeight = 30;
            volumeSlider = new Rectangle(
                menuBg.X + (menuBg.Width - sliderWidth) / 2,
                centerY + 40,
                sliderWidth,
                sliderHeight
            );
            spriteBatch.Draw(pixelTexture, volumeSlider, Color.DarkGray);
            DrawBorder(spriteBatch, pixelTexture, volumeSlider, 2, Color.White);

            // Volume fill
            volumeFill = new Rectangle(
                volumeSlider.X,
                volumeSlider.Y,
                (int)(volumeSlider.Width * currentVolume),
                volumeSlider.Height
            );
            spriteBatch.Draw(pixelTexture, volumeFill, Color.Magenta * 0.6f);

            // Volume percentage
            string volumeText = $"{(int)(currentVolume * 100)}%";
            Vector2 volumeTextSize = font.MeasureString(volumeText);
            Vector2 volumeTextPos = new Vector2(
                volumeSlider.X + (volumeSlider.Width - volumeTextSize.X) / 2,
                volumeSlider.Y + (volumeSlider.Height - volumeTextSize.Y) / 2
            );
            spriteBatch.DrawString(font, volumeText, volumeTextPos, Color.White);

            // Mute button
            muteButton = new Rectangle(
                menuBg.X + (menuBg.Width - 200) / 2,
                centerY + 100,
                200,
                50
            );
            string muteText = isMuted ? "UNMUTE" : "MUTE";
            DrawButton(spriteBatch, pixelTexture, font, muteButton, muteText,
                muteButtonHovered ? (isMuted ? Color.DarkGreen : Color.DarkRed) : Color.DarkSlateGray,
                muteButtonHovered ? (isMuted ? Color.Lime : Color.Red) : Color.Gray);

            // Back button
            backButton = new Rectangle(
                menuBg.X + (menuBg.Width - 200) / 2,
                menuBg.Bottom - 70,
                200,
                50
            );
            DrawButton(spriteBatch, pixelTexture, font, backButton, "BACK",
                backButtonHovered ? Color.DarkGray : Color.DarkSlateGray,
                backButtonHovered ? Color.White : Color.Gray);
        }

        private void DrawButton(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font,
            Rectangle button, string text, Color bgColor, Color borderColor)
        {
            spriteBatch.Draw(pixelTexture, button, bgColor);
            DrawBorder(spriteBatch, pixelTexture, button, 3, borderColor);

            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2(
                button.X + (button.Width - textSize.X) / 2,
                button.Y + (button.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(font, text, textPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, text, textPos, Color.White);
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