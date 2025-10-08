using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Claude4_5Terraria.UI
{
    // The MenuState enum definition has been removed from here 
    // to resolve the CS0101 error, as it is defined elsewhere in 
    // the Claude4_5Terraria.UI namespace (e.g., in StartMenu.cs).

    public class PauseMenu
    {
        public bool IsPaused { get; private set; }
        public MenuState CurrentState { get; private set; }

        private Rectangle pauseRect;
        private Rectangle optionsRect;
        private Rectangle saveRect;
        private bool showOptions;
        private bool showSaveMenu;

        private Action openSaveMenuCallback;
        private Action quitToMenuCallback;
        private Action<float> musicVolumeCallback;
        private float currentMusicVolume;
        private Action toggleFullscreenCallback;

        // NEW: Fullscreen map toggle action
        private Action toggleFullscreenMapCallback;
        private HUD hud;

        // Sliders for options
        private Slider musicVolumeSlider;
        private Slider minimapOpacitySlider;

        // Buttons
        private Button resumeButton;
        private Button optionsButton;
        private Button saveButton;
        private Button quitButton;

        // NEW: Button for Fullscreen Map
        private Button fullscreenMapButton;

        // Store previous mouse state for click detection (CRITICAL FIX)
        private MouseState previousMouseState;

        // NEW: Slider class
        private class Slider
        {
            public string Label { get; }
            public float MinValue { get; }
            public float MaxValue { get; }
            public float Increment { get; }
            private float currentValue;
            public Action<float> OnValueChanged { get; set; }

            private Rectangle sliderRect;
            private bool isDragging = false;

            public float Value
            {
                get => currentValue;
                set
                {
                    currentValue = MathHelper.Clamp(value, MinValue, MaxValue);
                    OnValueChanged?.Invoke(currentValue);
                }
            }

            public Slider(string label, float min, float max, float defaultValue, float increment, Action<float> callback)
            {
                Label = label;
                MinValue = min;
                MaxValue = max;
                Increment = increment;
                Value = defaultValue;
                OnValueChanged = callback;
            }

            public void Update(MouseState mouseState, Rectangle bounds)
            {
                // Define slider area relative to the bounds passed by PauseMenu.Update
                // We assume bounds.Width/Height includes space for the label and value text
                Rectangle currentSliderRect = new Rectangle(bounds.X + 150, bounds.Y + 10, bounds.Width - 170, bounds.Height - 10);

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (currentSliderRect.Contains(new Point(mouseState.X, mouseState.Y)) && !isDragging)
                    {
                        isDragging = true;
                    }
                    if (isDragging)
                    {
                        float percent = MathHelper.Clamp((mouseState.X - currentSliderRect.X) / (float)currentSliderRect.Width, 0f, 1f);
                        Value = MinValue + percent * (MaxValue - MinValue);
                    }
                }
                else
                {
                    isDragging = false;
                }
            }

            public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, Rectangle bounds)
            {
                // Define slider area relative to the bounds passed by PauseMenu.Draw
                Rectangle currentSliderRect = new Rectangle(bounds.X + 150, bounds.Y + 10, bounds.Width - 170, bounds.Height - 10);

                // Label
                spriteBatch.DrawString(font, Label, new Vector2(bounds.X, bounds.Y), Color.White);

                // Slider bar background
                spriteBatch.Draw(pixelTexture, currentSliderRect, Color.Gray * 0.5f);

                // Slider fill
                Rectangle fillRect = new Rectangle(currentSliderRect.X, currentSliderRect.Y,
                    (int)(currentSliderRect.Width * ((Value - MinValue) / (MaxValue - MinValue))), currentSliderRect.Height);
                spriteBatch.Draw(pixelTexture, fillRect, Color.Blue);

                // Knob
                int knobX = currentSliderRect.X + (int)(currentSliderRect.Width * ((Value - MinValue) / (MaxValue - MinValue)));
                Rectangle knob = new Rectangle(knobX - 5, currentSliderRect.Y - 5, 10, currentSliderRect.Height + 10);
                spriteBatch.Draw(pixelTexture, knob, Color.White);

                // Value text
                string valueText = $"{(int)(Value * 100)}%";
                Vector2 textSize = font.MeasureString(valueText);
                spriteBatch.DrawString(font, valueText, new Vector2(currentSliderRect.Right + 10, currentSliderRect.Y), Color.White);
            }
        }

        // NEW: Button class
        private class Button
        {
            // FIX CS0200: Text property must be writable to change the label in Draw
            public string Text { get; set; }
            public Rectangle Rect { get; set; }
            public bool IsHovered { get; private set; }
            public Action OnClick { get; set; }

            public Button(string text, Rectangle rect, Action onClick)
            {
                Text = text;
                Rect = rect;
                OnClick = onClick;
            }

            public void Update(MouseState mouseState)
            {
                IsHovered = Rect.Contains(new Point(mouseState.X, mouseState.Y));
            }

            public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
            {
                Color bgColor = IsHovered ? Color.Gray * 0.8f : Color.Gray * 0.5f;
                spriteBatch.Draw(pixelTexture, Rect, bgColor);
                DrawBorder(spriteBatch, pixelTexture, Rect, 1, Color.White);

                Vector2 textSize = font.MeasureString(Text);
                Vector2 textPos = new Vector2(Rect.X + (Rect.Width - textSize.X) / 2, Rect.Y + (Rect.Height - textSize.Y) / 2);
                spriteBatch.DrawString(font, Text, textPos, Color.White);
            }
        }

        // NEW: Border helper
        private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle rect, int thickness, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        // UPDATED CONSTRUCTOR SIGNATURE (7 arguments): Removed the unnecessary zoom toggle action
        public PauseMenu(Action openSaveMenu, Action quitToMenu, Action<float> musicCallback, float musicVol, Action toggleFullscreen, Action toggleFullscreenMap, HUD hud)
        {
            openSaveMenuCallback = openSaveMenu;
            quitToMenuCallback = quitToMenu;
            musicVolumeCallback = musicCallback;
            currentMusicVolume = musicVol;
            toggleFullscreenCallback = toggleFullscreen;
            toggleFullscreenMapCallback = toggleFullscreenMap; // <-- Fullscreen Map Toggle
            this.hud = hud;

            IsPaused = false;
            CurrentState = MenuState.Paused;
            showOptions = false;
            showSaveMenu = false;
            previousMouseState = Mouse.GetState();

            // Initialize buttons (centered on screen - default values)
            int screenWidth = 1920;
            int screenHeight = 1080;
            int buttonWidth = 200;
            int buttonHeight = 40;
            int startY = (screenHeight / 2) - 100;

            resumeButton = new Button("Resume", new Rectangle((screenWidth - buttonWidth) / 2, startY, buttonWidth, buttonHeight), () => IsPaused = false);
            optionsButton = new Button("Options", new Rectangle((screenWidth - buttonWidth) / 2, startY + 50, buttonWidth, buttonHeight), () => showOptions = true); // Set to true to open
            saveButton = new Button("Save", new Rectangle((screenWidth - buttonWidth) / 2, startY + 100, buttonWidth, buttonHeight), openSaveMenu);
            quitButton = new Button("Quit to Menu", new Rectangle((screenWidth - buttonWidth) / 2, startY + 150, buttonWidth, buttonHeight), quitToMenu);

            // Initialize sliders
            musicVolumeSlider = new Slider("Music Volume", 0f, 1f, currentMusicVolume, 0.05f, (value) =>
            {
                currentMusicVolume = value;
                musicVolumeCallback?.Invoke(value);
            });

            minimapOpacitySlider = new Slider("Minimap Opacity", 0f, 1f, this.hud.MinimapOpacity, 0.05f, (value) =>
            {
                this.hud.MinimapOpacity = value;
            });

            // NEW: Initialize Fullscreen Map Button
            // Positioned later in options panel
            fullscreenMapButton = new Button("Toggle Fullscreen Map", new Rectangle(0, 0, 400, 40), toggleFullscreenMap);
        }

        public void Update()
        {
            if (!IsPaused) return;

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();

            // CRITICAL: isNewClick must be defined once here
            bool isNewClick = mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;

            // --- ESCAPE KEY HANDLING ---
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                if (showOptions)
                {
                    showOptions = false; // Close options menu
                    return;
                }
            }

            // --- CRITICAL FIX: If map is fullscreen, allow clicks/Escape to exit it ---
            if (hud != null && hud.IsMapFullscreen)
            {
                // The variable isNewClick is already declared at the start of Update().

                if (isNewClick || keyboardState.IsKeyDown(Keys.Escape))
                {
                    // This relies on Game1's update to call TogglePause() on ESC, 
                    // and click detection here to call the HUD toggle manually.
                    if (hud.IsMapFullscreen)
                    {
                        hud.ToggleFullscreenMap(); // Exit fullscreen map
                    }
                    else
                    {
                        IsPaused = false; // Unpause
                    }
                }

                // If the map is fullscreen, we don't process pause menu buttons.
                previousMouseState = mouseState;
                return;
            }

            // --- COMMON MOUSE CLICK LOGIC (Only runs if not fullscreen map) ---
            // The definition of isNewClick MUST NOT be repeated here (it is on line 239).

            // Define options area (Panel height increased to 300)
            Rectangle optionsPanel = new Rectangle((1920 - 500) / 2, 200, 500, 300);
            Rectangle backButtonRect = new Rectangle(optionsPanel.X + 10, optionsPanel.Y + 10, 80, 30);

            // Define button/slider areas inside the Options Panel
            Rectangle fullscreenButtonRect = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 60, 400, 40);
            Rectangle fullscreenMapButtonRect = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 110, 460, 40);
            Rectangle musicSliderArea = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 160, 400, 40);
            Rectangle minimapSliderArea = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 210, 400, 40);

            if (showOptions)
            {
                // Update buttons/sliders that are NOT sliders
                fullscreenMapButton.Rect = fullscreenMapButtonRect;
                fullscreenMapButton.Update(mouseState);

                // Update sliders 
                musicVolumeSlider.Update(mouseState, musicSliderArea);
                minimapOpacitySlider.Update(mouseState, minimapSliderArea);

                // Handle Options Menu Button Clicks
                if (isNewClick)
                {
                    // Back button click
                    if (backButtonRect.Contains(mouseState.Position))
                    {
                        showOptions = false;
                        previousMouseState = mouseState;
                        return;
                    }

                    // Fullscreen toggle click
                    if (fullscreenButtonRect.Contains(mouseState.Position))
                    {
                        toggleFullscreenCallback?.Invoke();
                    }

                    // Fullscreen map toggle click
                    if (fullscreenMapButtonRect.Contains(mouseState.Position))
                    {
                        fullscreenMapButton.OnClick?.Invoke();
                    }
                }
            }
            else // Main Pause Menu
            {
                // Handle Main Menu Button Clicks
                if (isNewClick)
                {
                    if (resumeButton.Rect.Contains(mouseState.Position)) resumeButton.OnClick();
                    if (optionsButton.Rect.Contains(mouseState.Position)) optionsButton.OnClick();
                    if (saveButton.Rect.Contains(mouseState.Position)) saveButton.OnClick();
                    if (quitButton.Rect.Contains(mouseState.Position)) quitButton.OnClick();
                }

                // Update hover state for main buttons
                resumeButton.Update(mouseState);
                optionsButton.Update(mouseState);
                saveButton.Update(mouseState);
                quitButton.Update(mouseState);
            }

            // Store current mouse state for the next frame's click detection
            previousMouseState = mouseState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!IsPaused) return;

            // --- CRITICAL FIX: Do NOT draw the pause menu overlay if the map is fullscreen ---
            if (hud != null && hud.IsMapFullscreen)
            {
                return;
            }

            // Semi-transparent overlay
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.5f);

            // --- Options Menu Drawing ---
            if (showOptions)
            {
                // Define panel area (matching Update's size)
                Rectangle optionsPanel = new Rectangle((screenWidth - 500) / 2, 200, 500, 300);
                spriteBatch.Draw(pixelTexture, optionsPanel, Color.Gray * 0.8f);
                DrawBorder(spriteBatch, pixelTexture, optionsPanel, 2, Color.White);

                // Back button
                Rectangle backButtonRect = new Rectangle(optionsPanel.X + 10, optionsPanel.Y + 10, 80, 30);
                string backText = "Back";
                bool isBackHovered = backButtonRect.Contains(Mouse.GetState().Position);
                Color backColor = isBackHovered ? Color.DarkGray * 0.8f : Color.Gray * 0.5f;
                spriteBatch.Draw(pixelTexture, backButtonRect, backColor);
                DrawBorder(spriteBatch, pixelTexture, backButtonRect, 1, Color.White);
                Vector2 backTextSize = font.MeasureString(backText);
                Vector2 backTextPos = new Vector2(backButtonRect.X + (backButtonRect.Width - backTextSize.X) / 2, backButtonRect.Y + (backButtonRect.Height - backTextSize.Y) / 2);
                spriteBatch.DrawString(font, backText, backTextPos, Color.White);

                // Fullscreen toggle display area
                string fullscreenText = "Windowed/Fullscreen Toggle";
                Rectangle fullscreenDisplayArea = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 60, 400, 40);
                spriteBatch.DrawString(font, fullscreenText, new Vector2(fullscreenDisplayArea.X, fullscreenDisplayArea.Y), Color.White);

                // NEW: Fullscreen map button
                string mapButtonText = "Toggle Fullscreen Map: " + (hud.IsMapFullscreen ? "On" : "Off");
                fullscreenMapButton.Text = mapButtonText;
                fullscreenMapButton.Rect = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 110, 460, 40);
                fullscreenMapButton.Draw(spriteBatch, pixelTexture, font);

                // Draw sliders
                Rectangle musicSliderArea = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 160, 400, 40);
                musicVolumeSlider.Draw(spriteBatch, pixelTexture, font, musicSliderArea);

                Rectangle minimapSliderArea = new Rectangle(optionsPanel.X + 20, optionsPanel.Y + 210, 400, 40);
                minimapOpacitySlider.Draw(spriteBatch, pixelTexture, font, minimapSliderArea);
            }
            // --- Main Menu Drawing ---
            else
            {
                // Draw pause menu buttons (centered)
                int buttonWidth = 200;
                int buttonHeight = 40;
                int startY = (screenHeight / 2) - 100;

                // Update button positions for current screen resolution
                resumeButton.Rect = new Rectangle((screenWidth - buttonWidth) / 2, startY, buttonWidth, buttonHeight);
                optionsButton.Rect = new Rectangle((screenWidth - buttonWidth) / 2, startY + 50, buttonWidth, buttonHeight);
                saveButton.Rect = new Rectangle((screenWidth - buttonWidth) / 2, startY + 100, buttonWidth, buttonHeight);
                quitButton.Rect = new Rectangle((screenWidth - buttonWidth) / 2, startY + 150, buttonWidth, buttonHeight);

                // Draw buttons
                resumeButton.Draw(spriteBatch, pixelTexture, font);
                optionsButton.Draw(spriteBatch, pixelTexture, font);
                saveButton.Draw(spriteBatch, pixelTexture, font);
                quitButton.Draw(spriteBatch, pixelTexture, font);
            }
        }

        // Public methods for external control
        public void TogglePause()
        {
            IsPaused = !IsPaused;
            if (IsPaused)
            {
                CurrentState = MenuState.Paused;
                // Reset state when pausing to show main pause buttons, not options
                showOptions = false;
            }
        }

        public void SetState(MenuState state)
        {
            CurrentState = state;
        }
    }
}