using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace StarshroudHollows.UI
{
    public class OptionsMenu
    {
        private bool isOpen;
        private KeyboardState previousKeyState;
        private MouseState previousMouseState;

        // Music Slider Fields
        private Rectangle musicSliderTrack;
        private Rectangle musicSliderHandle;
        private bool isDraggingMusicSlider;

        // Sound Effects Slider Fields
        private Rectangle soundSliderTrack;
        private Rectangle soundSliderHandle;
        private bool isDraggingSoundSlider;

        private float musicVolume;
        private Action<float> onMusicVolumeChanged;

        // NEW: Sound Effects Volume Field and Callback
        private float soundEffectsVolume;
        private Action<float> onSoundEffectsVolumeChanged;

        // NEW: Action to trigger the test sound in Game1
        private Action onTestSoundPlayed;

        private Action onToggleFullscreen;

        private Rectangle fullscreenButton;
        private bool fullscreenButtonHovered;

        private const int SLIDER_WIDTH = 400;
        private const int SLIDER_HEIGHT = 20;
        private const int HANDLE_WIDTH = 10;
        private const int SLIDER_SPACING = 80; // Vertical space between sliders

        // UPDATED: Constructor now takes the test sound action
        public OptionsMenu(float initialMusicVolume, Action<float> onMusicVolumeChanged, float initialSoundEffectsVolume, Action<float> onSoundEffectsVolumeChanged, Action onToggleFullscreen = null, Action onTestSoundPlayed = null)
        {
            this.musicVolume = initialMusicVolume;
            this.onMusicVolumeChanged = onMusicVolumeChanged;
            this.soundEffectsVolume = initialSoundEffectsVolume;
            this.onSoundEffectsVolumeChanged = onSoundEffectsVolumeChanged;
            this.onToggleFullscreen = onToggleFullscreen;
            this.onTestSoundPlayed = onTestSoundPlayed; // NEW
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

        public void SetSoundEffectsVolume(float volume)
        {
            soundEffectsVolume = MathHelper.Clamp(volume, 0f, 1f);
        }

        public void Update(int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
            Point mousePoint = new Point(mouseState.X, mouseState.Y);

            // Close with Escape
            if (keyState.IsKeyDown(Keys.Escape) && !previousKeyState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            // --- Calculate slider positions ---
            int sliderCenterX = (screenWidth - SLIDER_WIDTH) / 2;
            int startY = screenHeight / 2 - SLIDER_SPACING / 2; // Adjusted start Y to center the two sliders

            musicSliderTrack = new Rectangle(sliderCenterX, startY, SLIDER_WIDTH, SLIDER_HEIGHT);
            soundSliderTrack = new Rectangle(sliderCenterX, startY + SLIDER_SPACING, SLIDER_WIDTH, SLIDER_HEIGHT); // NEW Position

            // Calculate slider handles
            int musicHandleX = sliderCenterX + (int)(musicVolume * (SLIDER_WIDTH - HANDLE_WIDTH));
            musicSliderHandle = new Rectangle(musicHandleX, musicSliderTrack.Y - 5, HANDLE_WIDTH, SLIDER_HEIGHT + 10);

            int soundHandleX = sliderCenterX + (int)(soundEffectsVolume * (SLIDER_WIDTH - HANDLE_WIDTH));
            soundSliderHandle = new Rectangle(soundHandleX, soundSliderTrack.Y - 5, HANDLE_WIDTH, SLIDER_HEIGHT + 10); // NEW Handle

            // Fullscreen button position (moved down to accommodate the second slider)
            fullscreenButton = new Rectangle(
                (screenWidth - 250) / 2,
                startY + SLIDER_SPACING * 2, // Positioned below both sliders
                250,
                50
            );

            fullscreenButtonHovered = fullscreenButton.Contains(mousePoint);

            // Handle fullscreen button click
            bool isNewClick = mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;

            if (isNewClick && fullscreenButtonHovered)
            {
                onToggleFullscreen?.Invoke();
            }

            // --- Handle mouse dragging (Music Slider) ---
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                // Start dragging Music Slider
                if ((musicSliderHandle.Contains(mousePoint) || musicSliderTrack.Contains(mousePoint)) && !isDraggingSoundSlider)
                {
                    isDraggingMusicSlider = true;
                }

                // Start dragging Sound Effects Slider
                if ((soundSliderHandle.Contains(mousePoint) || soundSliderTrack.Contains(mousePoint)) && !isDraggingMusicSlider)
                {
                    isDraggingSoundSlider = true;
                }

                // Update volume while dragging Music
                if (isDraggingMusicSlider)
                {
                    int relativeX = mouseState.X - sliderCenterX;
                    float newVolume = MathHelper.Clamp((float)relativeX / SLIDER_WIDTH, 0f, 1f);
                    musicVolume = newVolume;
                    onMusicVolumeChanged?.Invoke(musicVolume);
                }

                // Update volume while dragging Sound Effects
                if (isDraggingSoundSlider)
                {
                    int relativeX = mouseState.X - sliderCenterX;
                    float newVolume = MathHelper.Clamp((float)relativeX / SLIDER_WIDTH, 0f, 1f);

                    // Only update and play test sound if the value actually changed
                    if (Math.Abs(soundEffectsVolume - newVolume) > 0.01f)
                    {
                        soundEffectsVolume = newVolume;
                        onSoundEffectsVolumeChanged?.Invoke(soundEffectsVolume);
                        onTestSoundPlayed?.Invoke(); // NEW: Play sound immediately
                    }
                }
            }
            else
            {
                isDraggingMusicSlider = false;
                isDraggingSoundSlider = false;
            }

            previousKeyState = keyState;
            previousMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            // Dark overlay
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.8f);

            // Menu panel (increased height for two sliders)
            int panelWidth = 700;
            int panelHeight = 450; // Increased Height
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

            // --- 1. Music Volume Slider ---
            DrawSlider(spriteBatch, pixelTexture, font, musicSliderTrack, musicSliderHandle, musicVolume, isDraggingMusicSlider, "Music Volume:");

            // --- 2. Sound Effects Volume Slider (NEW) ---
            DrawSlider(spriteBatch, pixelTexture, font, soundSliderTrack, soundSliderHandle, soundEffectsVolume, isDraggingSoundSlider, "S.F.X. Volume:");

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

        private void DrawSlider(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, Rectangle track, Rectangle handle, float volume, bool isDragging, string label)
        {
            // Label
            Vector2 labelPos = new Vector2(track.X, track.Y - 40);
            spriteBatch.DrawString(font, label, labelPos, Color.White);

            // Draw slider track
            spriteBatch.Draw(pixelTexture, track, Color.DarkGray);
            DrawBorder(spriteBatch, pixelTexture, track, 2, Color.Gray);

            // Draw slider fill
            if (volume > 0)
            {
                int fillWidth = (int)(volume * SLIDER_WIDTH);
                Rectangle fillRect = new Rectangle(track.X, track.Y, fillWidth, SLIDER_HEIGHT);

                // Color gradient: Green at full, yellow at mid, red at low
                Color fillColor;
                if (volume > 0.5f)
                {
                    // Green to yellow
                    float t = (volume - 0.5f) * 2f;
                    fillColor = Color.Lerp(Color.Yellow, Color.Lime, t);
                }
                else
                {
                    // Red to yellow
                    float t = volume * 2f;
                    fillColor = Color.Lerp(Color.DarkRed, Color.Yellow, t);
                }

                spriteBatch.Draw(pixelTexture, fillRect, fillColor);
            }

            // Draw slider handle
            Color handleColor = isDragging ? Color.Yellow : Color.White;
            spriteBatch.Draw(pixelTexture, handle, handleColor);
            DrawBorder(spriteBatch, pixelTexture, handle, 2, Color.Black);

            // Volume percentage
            int volumePercent = (int)(volume * 100);
            string volumeText = volume == 0 ? "MUTED" : $"{volumePercent}%";
            Vector2 volumeTextPos = new Vector2(track.Right + 20, track.Y);
            spriteBatch.DrawString(font, volumeText, volumeTextPos + new Vector2(1, 1), Color.Black);

            Color volumeColor = volume == 0 ? Color.Red : Color.White;
            spriteBatch.DrawString(font, volumeText, volumeTextPos, volumeColor);
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