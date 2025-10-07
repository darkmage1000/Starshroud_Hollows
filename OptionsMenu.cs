using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Claude4_5Terraria.UI
{
    public class OptionsMenu
    {
        private bool isOpen;
        private KeyboardState previousKeyState;
        private MouseState previousMouseState;

        private Rectangle musicSliderTrack;
        private Rectangle musicSliderHandle;
        private bool isDraggingMusicSlider;

        private float musicVolume;
        private Action<float> onMusicVolumeChanged;
        private Action onToggleFullscreen;
        
        private Rectangle fullscreenButton;
        private bool fullscreenButtonHovered;

        private const int SLIDER_WIDTH = 400;
        private const int SLIDER_HEIGHT = 20;
        private const int HANDLE_WIDTH = 10;

        public OptionsMenu(float initialMusicVolume, Action<float> onMusicVolumeChanged, Action onToggleFullscreen = null)
        {
            this.musicVolume = initialMusicVolume;
            this.onMusicVolumeChanged = onMusicVolumeChanged;
            this.onToggleFullscreen = onToggleFullscreen;
            isOpen = false;
            previousKeyState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
        }

        public bool IsOpen => isOpen;

        public void Open()
        {
            isOpen = true;
        }

        public void Close()
        {
            isOpen = false;
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = MathHelper.Clamp(volume, 0f, 1f);
        }

        public void Update(int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // Close with Escape
            if (keyState.IsKeyDown(Keys.Escape) && !previousKeyState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            // Calculate slider positions
            int sliderX = (screenWidth - SLIDER_WIDTH) / 2;
            int sliderY = screenHeight / 2;

            musicSliderTrack = new Rectangle(sliderX, sliderY, SLIDER_WIDTH, SLIDER_HEIGHT);

            int handleX = sliderX + (int)(musicVolume * (SLIDER_WIDTH - HANDLE_WIDTH));
            musicSliderHandle = new Rectangle(handleX, sliderY - 5, HANDLE_WIDTH, SLIDER_HEIGHT + 10);
            
            // Fullscreen button
            fullscreenButton = new Rectangle(
                (screenWidth - 250) / 2,
                sliderY + 80,
                250,
                50
            );
            
            Point mousePoint = new Point(mouseState.X, mouseState.Y);
            fullscreenButtonHovered = fullscreenButton.Contains(mousePoint);

            // Handle fullscreen button click
            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                if (fullscreenButtonHovered)
                {
                    onToggleFullscreen?.Invoke();
                }
            }
            
            // Handle mouse dragging
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                // Start dragging if clicked on handle or track
                if (musicSliderHandle.Contains(mousePoint) || musicSliderTrack.Contains(mousePoint))
                {
                    isDraggingMusicSlider = true;
                }

                // Update volume while dragging
                if (isDraggingMusicSlider)
                {
                    int relativeX = mouseState.X - sliderX;
                    float newVolume = MathHelper.Clamp((float)relativeX / SLIDER_WIDTH, 0f, 1f);
                    musicVolume = newVolume;
                    onMusicVolumeChanged?.Invoke(musicVolume);
                }
            }
            else
            {
                isDraggingMusicSlider = false;
            }

            previousKeyState = keyState;
            previousMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            // Dark overlay
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.8f);

            // Menu panel
            int panelWidth = 700;
            int panelHeight = 400;
            Rectangle panelBg = new Rectangle(
                (screenWidth - panelWidth) / 2,
                (screenHeight - panelHeight) / 2,
                panelWidth,
                panelHeight
            );
            spriteBatch.Draw(pixelTexture, panelBg, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, panelBg, 3, Color.White);

            // Title
            string title = "OPTIONS";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(
                (screenWidth - titleSize.X) / 2,
                panelBg.Y + 30
            );
            spriteBatch.DrawString(font, title, titlePos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            // Music Volume Label
            string musicLabel = "Music Volume:";
            Vector2 labelPos = new Vector2(musicSliderTrack.X, musicSliderTrack.Y - 40);
            spriteBatch.DrawString(font, musicLabel, labelPos, Color.White);

            // Draw slider track
            spriteBatch.Draw(pixelTexture, musicSliderTrack, Color.DarkGray);
            DrawBorder(spriteBatch, pixelTexture, musicSliderTrack, 2, Color.Gray);

            // Draw slider fill (gradient from green to red)
            if (musicVolume > 0)
            {
                int fillWidth = (int)(musicVolume * SLIDER_WIDTH);
                Rectangle fillRect = new Rectangle(musicSliderTrack.X, musicSliderTrack.Y, fillWidth, SLIDER_HEIGHT);

                // Color gradient: Green at full, yellow at mid, red at low
                Color fillColor;
                if (musicVolume > 0.5f)
                {
                    // Green to yellow
                    float t = (musicVolume - 0.5f) * 2f;
                    fillColor = Color.Lerp(Color.Yellow, Color.Lime, t);
                }
                else
                {
                    // Red to yellow
                    float t = musicVolume * 2f;
                    fillColor = Color.Lerp(Color.DarkRed, Color.Yellow, t);
                }

                spriteBatch.Draw(pixelTexture, fillRect, fillColor);
            }

            // Draw slider handle
            Color handleColor = isDraggingMusicSlider ? Color.Yellow : Color.White;
            spriteBatch.Draw(pixelTexture, musicSliderHandle, handleColor);
            DrawBorder(spriteBatch, pixelTexture, musicSliderHandle, 2, Color.Black);

            // Volume percentage
            int volumePercent = (int)(musicVolume * 100);
            string volumeText = musicVolume == 0 ? "MUTED" : $"{volumePercent}%";
            Vector2 volumeTextPos = new Vector2(musicSliderTrack.Right + 20, musicSliderTrack.Y);
            spriteBatch.DrawString(font, volumeText, volumeTextPos + new Vector2(1, 1), Color.Black);

            Color volumeColor = musicVolume == 0 ? Color.Red : Color.White;
            spriteBatch.DrawString(font, volumeText, volumeTextPos, volumeColor);
            
            // Fullscreen button
            Color buttonBg = fullscreenButtonHovered ? Color.DarkBlue : Color.DarkSlateGray;
            Color buttonBorder = fullscreenButtonHovered ? Color.Cyan : Color.Gray;
            spriteBatch.Draw(pixelTexture, fullscreenButton, buttonBg);
            DrawBorder(spriteBatch, pixelTexture, fullscreenButton, 3, buttonBorder);
            
            string fullscreenText = "TOGGLE FULLSCREEN";
            Vector2 fullscreenTextSize = font.MeasureString(fullscreenText);
            Vector2 fullscreenTextPos = new Vector2(
                fullscreenButton.X + (fullscreenButton.Width - fullscreenTextSize.X) / 2,
                fullscreenButton.Y + (fullscreenButton.Height - fullscreenTextSize.Y) / 2
            );
            spriteBatch.DrawString(font, fullscreenText, fullscreenTextPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, fullscreenText, fullscreenTextPos, Color.White);

            // Instructions
            string instructions = "Drag slider to adjust | ESC to close";
            Vector2 instructSize = font.MeasureString(instructions);
            Vector2 instructPos = new Vector2(
                (screenWidth - instructSize.X) / 2,
                panelBg.Bottom - 50
            );
            spriteBatch.DrawString(font, instructions, instructPos, Color.Gray);
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