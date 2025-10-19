using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StarshroudHollows.UI;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.UI
{
    public class PauseMenu
    {
        public bool IsPaused { get; private set; }
        public MenuState CurrentState { get; private set; }

        private bool showOptions;
        private Action openSaveMenuCallback;
        private Action quitToMenuCallback;
        private Action<float> musicVolumeCallback;
        private float currentMusicVolume;
        private Action<float> gameSoundsVolumeCallback;
        private float currentGameSoundsVolume;
        private Action toggleFullscreenCallback;
        private Action toggleFullscreenMapCallback;
        private HUD hud;
        private Action<bool?> toggleAutoMiningCallback;
        private bool isAutoMiningActive;

        private Slider musicVolumeSlider;
        private Slider gameSoundsVolumeSlider;
        private Slider minimapOpacitySlider;

        private Button resumeButton;
        private Button optionsButton;
        private Button saveButton;
        private Button quitButton;
        private Button fullscreenMapButton;
        private Button autoMineButton;

        private MouseState previousMouseState;
        private KeyboardState previousKeyboardState;

        #region Inner Classes
        private class Slider
        {
            public string Label { get; }
            public float MinValue { get; }
            public float MaxValue { get; }
            private float currentValue;
            public Action<float> OnValueChanged { get; set; }
            private bool isDragging = false;

            public float Value
            {
                get => currentValue;
                set { currentValue = MathHelper.Clamp(value, MinValue, MaxValue); OnValueChanged?.Invoke(currentValue); }
            }

            public Slider(string label, float min, float max, float defaultValue, Action<float> callback)
            {
                Label = label; MinValue = min; MaxValue = max; Value = defaultValue; OnValueChanged = callback;
            }

            public void Update(MouseState m, MouseState p, Rectangle b)
            {
                Rectangle r = new Rectangle(b.X + 150, b.Y + 10, b.Width - 170, b.Height - 10);
                if (m.LeftButton == ButtonState.Pressed) { if (r.Contains(m.Position) && p.LeftButton == ButtonState.Released) isDragging = true; if (isDragging) Value = MinValue + MathHelper.Clamp((m.X - r.X) / (float)r.Width, 0f, 1f) * (MaxValue - MinValue); } else isDragging = false;
            }

            public void Draw(SpriteBatch s, Texture2D t, SpriteFont f, Rectangle b)
            {
                Rectangle r = new Rectangle(b.X + 150, b.Y + 10, b.Width - 170, b.Height - 10);
                s.DrawString(f, Label, new Vector2(b.X, b.Y), Color.White);
                s.Draw(t, r, Color.Gray * 0.5f);
                Rectangle fR = new Rectangle(r.X, r.Y, (int)(r.Width * ((Value - MinValue) / (MaxValue - MinValue))), r.Height);
                s.Draw(t, fR, Color.Lerp(Color.DarkRed, Color.LimeGreen, Value / MaxValue));
                int kX = r.X + (int)(r.Width * ((Value - MinValue) / (MaxValue - MinValue)));
                s.Draw(t, new Rectangle(kX - 5, r.Y - 5, 10, r.Height + 10), Color.White);
                s.DrawString(f, Value == 0 ? "MUTED" : $"{(int)(Value * 100)}%", new Vector2(r.Right + 10, r.Y), Value == 0 ? Color.Red : Color.White);
            }
        }
        private class Button
        {
            public string Text { get; set; }
            public Rectangle Rect { get; set; }
            public bool IsHovered { get; private set; }
            public Action OnClick { get; set; }

            public Button(string text, Rectangle rect, Action onClick) { Text = text; Rect = rect; OnClick = onClick; }
            public void Update(MouseState m) => IsHovered = Rect.Contains(m.Position);
            public void Draw(SpriteBatch s, Texture2D t, SpriteFont f) { s.Draw(t, Rect, IsHovered ? Color.Gray * 0.8f : Color.Gray * 0.5f); DrawBorder(s, t, Rect, 1, Color.White); Vector2 tS = f.MeasureString(Text); s.DrawString(f, Text, new Vector2(Rect.X + (Rect.Width - tS.X) / 2, Rect.Y + (Rect.Height - tS.Y) / 2), Color.White); }
        }
        private static void DrawBorder(SpriteBatch s, Texture2D t, Rectangle r, int h, Color c) { s.Draw(t, new Rectangle(r.X, r.Y, r.Width, h), c); s.Draw(t, new Rectangle(r.X, r.Bottom - h, r.Width, h), c); s.Draw(t, new Rectangle(r.X, r.Y, h, r.Height), c); s.Draw(t, new Rectangle(r.Right - h, r.Y, h, r.Height), c); }
        #endregion

        public PauseMenu(Action o, Action q, Action<float> mC, float mV, Action tF, Action tFM, HUD h, Action<bool?> tAM, bool iAS, Action<float> gSC, float gSV)
        {
            openSaveMenuCallback = o; quitToMenuCallback = q; musicVolumeCallback = mC; currentMusicVolume = mV; gameSoundsVolumeCallback = gSC; currentGameSoundsVolume = gSV;
            toggleFullscreenCallback = tF; toggleFullscreenMapCallback = tFM; this.hud = h; toggleAutoMiningCallback = tAM; isAutoMiningActive = iAS;
            IsPaused = false; CurrentState = MenuState.Paused; showOptions = false;
            previousMouseState = Mouse.GetState(); previousKeyboardState = Keyboard.GetState();

            int sW = 1920, sH = 1080, bW = 200, bH = 40, sY = (sH / 2) - 100;
            resumeButton = new Button("Resume", new Rectangle((sW - bW) / 2, sY, bW, bH), TogglePause);
            optionsButton = new Button("Options", new Rectangle((sW - bW) / 2, sY + 50, bW, bH), () => showOptions = true);
            saveButton = new Button("Save", new Rectangle((sW - bW) / 2, sY + 100, bW, bH), openSaveMenuCallback);
            quitButton = new Button("Quit to Menu", new Rectangle((sW - bW) / 2, sY + 150, bW, bH), quitToMenuCallback);
            musicVolumeSlider = new Slider("Music Volume", 0f, 1f, currentMusicVolume, (v) => { currentMusicVolume = v; musicVolumeCallback?.Invoke(v); });
            gameSoundsVolumeSlider = new Slider("SFX Volume", 0f, 1f, currentGameSoundsVolume, (v) => { currentGameSoundsVolume = v; gameSoundsVolumeCallback?.Invoke(v); });
            minimapOpacitySlider = new Slider("Minimap Opacity", 0f, 1f, this.hud.MinimapOpacity, (v) => { this.hud.MinimapOpacity = v; });
            fullscreenMapButton = new Button("Toggle Fullscreen Map", new Rectangle(), toggleFullscreenMapCallback);
            autoMineButton = new Button("Toggle Auto-Mine (L)", new Rectangle(), () => { isAutoMiningActive = !isAutoMiningActive; toggleAutoMiningCallback?.Invoke(isAutoMiningActive); });
        }

        public void Update()
        {
            if (!IsPaused) return;

            MouseState mS = Mouse.GetState();
            KeyboardState kS = Keyboard.GetState();
            bool isNewClick = mS.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released;

            if (hud?.IsMapFullscreen == true) 
            { 
                if (isNewClick) 
                { 
                    hud.ToggleFullscreenMap(); 
                } 
                previousMouseState = mS; 
                previousKeyboardState = kS; 
                return; 
            }
            
            if (showOptions)
            {
                // Update sliders and buttons for options menu
                int bW = 400, bH = 40;
                int startY = (1080 / 2) - 150;
                int sW = 1920;
                int spacing = 60;
                
                // Update music volume slider
                Rectangle musicSliderRect = new Rectangle((sW - bW) / 2, startY, bW, bH);
                musicVolumeSlider.Update(mS, previousMouseState, musicSliderRect);
                
                // Update SFX volume slider
                Rectangle sfxSliderRect = new Rectangle((sW - bW) / 2, startY + spacing, bW, bH);
                gameSoundsVolumeSlider.Update(mS, previousMouseState, sfxSliderRect);
                
                // Update minimap opacity slider
                Rectangle minimapSliderRect = new Rectangle((sW - bW) / 2, startY + spacing * 2, bW, bH);
                minimapOpacitySlider.Update(mS, previousMouseState, minimapSliderRect);
                
                // Update fullscreen button
                int buttonW = 300, buttonH = 40;
                fullscreenMapButton.Rect = new Rectangle((sW - buttonW) / 2, startY + spacing * 3 + 20, buttonW, buttonH);
                fullscreenMapButton.Update(mS);
                
                // Update back button
                int backButtonY = startY + spacing * 4 + 40;
                Button backButton = new Button("Back", new Rectangle((sW - 200) / 2, backButtonY, 200, 40), () => showOptions = false);
                backButton.Update(mS);
                
                if (isNewClick)
                {
                    if (fullscreenMapButton.IsHovered) fullscreenMapButton.OnClick();
                    if (backButton.IsHovered) backButton.OnClick();
                }
            }
            else
            {
                // CRITICAL FIX: Update button positions BEFORE checking for clicks
                int bW = 200, bH = 40;
                int sY = (1080 / 2) - 100; // Use fixed screen height for now
                int sW = 1920; // Use fixed screen width for now
                
                resumeButton.Rect = new Rectangle((sW - bW) / 2, sY, bW, bH);
                optionsButton.Rect = new Rectangle((sW - bW) / 2, sY + 50, bW, bH);
                saveButton.Rect = new Rectangle((sW - bW) / 2, sY + 100, bW, bH);
                quitButton.Rect = new Rectangle((sW - bW) / 2, sY + 150, bW, bH);
                
                resumeButton.Update(mS); 
                optionsButton.Update(mS); 
                saveButton.Update(mS); 
                quitButton.Update(mS);
                
                if (isNewClick)
                {
                    if (resumeButton.IsHovered) resumeButton.OnClick(); 
                    if (optionsButton.IsHovered) optionsButton.OnClick();
                    if (saveButton.IsHovered) saveButton.OnClick(); 
                    if (quitButton.IsHovered) quitButton.OnClick();
                }
            }

            previousMouseState = mS; 
            previousKeyboardState = kS;
        }

        public void Draw(SpriteBatch s, Texture2D t, SpriteFont f, int sW, int sH)
        {
            if (!IsPaused || hud?.IsMapFullscreen == true) return;
            s.Draw(t, new Rectangle(0, 0, sW, sH), Color.Black * 0.5f);
            
            if (showOptions) 
            { 
                // Draw Options Menu
                string title = "OPTIONS";
                Vector2 titleSize = f.MeasureString(title);
                Vector2 titlePos = new Vector2((sW - titleSize.X) / 2, sH / 4);
                s.DrawString(f, title, titlePos + new Vector2(2, 2), Color.Black);
                s.DrawString(f, title, titlePos, Color.Yellow);
                
                // Draw sliders
                int bW = 400, bH = 40;
                int startY = (sH / 2) - 150;
                int spacing = 60;
                
                // Music volume slider
                Rectangle musicSliderRect = new Rectangle((sW - bW) / 2, startY, bW, bH);
                musicVolumeSlider.Draw(s, t, f, musicSliderRect);
                
                // SFX volume slider
                Rectangle sfxSliderRect = new Rectangle((sW - bW) / 2, startY + spacing, bW, bH);
                gameSoundsVolumeSlider.Draw(s, t, f, sfxSliderRect);
                
                // Minimap opacity slider
                Rectangle minimapSliderRect = new Rectangle((sW - bW) / 2, startY + spacing * 2, bW, bH);
                minimapOpacitySlider.Draw(s, t, f, minimapSliderRect);
                
                // Fullscreen toggle button
                int buttonW = 300, buttonH = 40;
                fullscreenMapButton.Text = "Toggle Fullscreen (F11)";
                fullscreenMapButton.Rect = new Rectangle((sW - buttonW) / 2, startY + spacing * 3 + 20, buttonW, buttonH);
                fullscreenMapButton.Draw(s, t, f);
                
                // Draw Back button
                int backButtonY = startY + spacing * 4 + 40;
                Rectangle backRect = new Rectangle((sW - 200) / 2, backButtonY, 200, 40);
                
                bool isHovered = backRect.Contains(Mouse.GetState().Position);
                s.Draw(t, backRect, isHovered ? Color.Gray * 0.8f : Color.Gray * 0.5f);
                DrawBorder(s, t, backRect, 1, Color.White);
                
                Vector2 backTextSize = f.MeasureString("Back");
                Vector2 backTextPos = new Vector2(backRect.X + (backRect.Width - backTextSize.X) / 2, backRect.Y + (backRect.Height - backTextSize.Y) / 2);
                s.DrawString(f, "Back", backTextPos + new Vector2(2, 2), Color.Black);
                s.DrawString(f, "Back", backTextPos, Color.White);
            }
            else
            {
                int bW = 200, bH = 40, sY = (sH / 2) - 100;
                resumeButton.Rect = new Rectangle((sW - bW) / 2, sY, bW, bH); 
                optionsButton.Rect = new Rectangle((sW - bW) / 2, sY + 50, bW, bH);
                saveButton.Rect = new Rectangle((sW - bW) / 2, sY + 100, bW, bH); 
                quitButton.Rect = new Rectangle((sW - bW) / 2, sY + 150, bW, bH);
                resumeButton.Draw(s, t, f); 
                optionsButton.Draw(s, t, f); 
                saveButton.Draw(s, t, f); 
                quitButton.Draw(s, t, f);
            }
        }

        public void TogglePause() { IsPaused = !IsPaused; if (IsPaused) { CurrentState = MenuState.Paused; showOptions = false; } }
        public void SetState(MenuState s) => CurrentState = s;
    }
}