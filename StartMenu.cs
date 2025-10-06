using Claude4_5Terraria.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Claude4_5Terraria.UI
{
    public enum MenuState
    {
        MainMenu,
        Options,
        LoadMenu,
        Loading,
        Playing
    }

    public class StartMenu
    {
        private MenuState currentState;
        private int selectedOption;
        private KeyboardState previousKeyState;

        private float loadingProgress;
        private string loadingMessage;

        private OptionsMenu optionsMenu;
        private LoadMenu loadMenu;
        private Texture2D backgroundTexture;

        private bool loadingSavedGame;
        private int loadingSlotIndex;

        private const int MENU_OPTION_PLAY = 0;
        private const int MENU_OPTION_LOAD = 1;
        private const int MENU_OPTION_OPTIONS = 2;
        private const int MENU_OPTIONS_COUNT = 3;

        public StartMenu(float initialMusicVolume, Action<float> onMusicVolumeChanged, Texture2D backgroundTexture = null)
        {
            currentState = MenuState.MainMenu;
            selectedOption = 0;
            previousKeyState = Keyboard.GetState();
            loadingProgress = 0f;
            loadingMessage = "Generating world...";
            optionsMenu = new OptionsMenu(initialMusicVolume, onMusicVolumeChanged);
            loadMenu = new LoadMenu(OnLoadSlotSelected);
            this.backgroundTexture = backgroundTexture;
            loadingSavedGame = false;
            loadingSlotIndex = -1;
        }

        public MenuState GetState() => currentState;
        public void SetState(MenuState state) => currentState = state;

        public bool IsLoadingSavedGame() => loadingSavedGame;
        public int GetLoadingSlotIndex() => loadingSlotIndex;

        private void OnLoadSlotSelected(int slotIndex)
        {
            loadingSavedGame = true;
            loadingSlotIndex = slotIndex;
            currentState = MenuState.Loading;
            Logger.Log($"[MENU] Load slot {slotIndex + 1} selected - transitioning to Loading state");
            Logger.Log($"[MENU] loadingSavedGame flag is now: {loadingSavedGame}");
        }

        public void SetLoadingProgress(float progress, string message)
        {
            loadingProgress = progress;
            loadingMessage = message;
        }

        public void SetMusicVolume(float volume)
        {
            optionsMenu.SetMusicVolume(volume);
        }

        public void Update(int screenWidth, int screenHeight)
        {
            if (currentState == MenuState.Loading)
            {
                return;
            }

            if (currentState == MenuState.Options)
            {
                optionsMenu.Update(screenWidth, screenHeight);

                if (!optionsMenu.IsOpen)
                {
                    currentState = MenuState.MainMenu;
                }
                return;
            }

            if (currentState == MenuState.LoadMenu)
            {
                loadMenu.Update(screenWidth, screenHeight);

                if (!loadMenu.IsOpen && currentState != MenuState.Loading)
                {
                    currentState = MenuState.MainMenu;
                }
                return;
            }

            if (currentState != MenuState.MainMenu) return;

            KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Keys.Down) && !previousKeyState.IsKeyDown(Keys.Down))
            {
                selectedOption = (selectedOption + 1) % MENU_OPTIONS_COUNT;
            }

            if (keyState.IsKeyDown(Keys.Up) && !previousKeyState.IsKeyDown(Keys.Up))
            {
                selectedOption = (selectedOption - 1 + MENU_OPTIONS_COUNT) % MENU_OPTIONS_COUNT;
            }

            if (keyState.IsKeyDown(Keys.Enter) && !previousKeyState.IsKeyDown(Keys.Enter))
            {
                HandleMenuSelection();
            }

            previousKeyState = keyState;
        }

        private void HandleMenuSelection()
        {
            switch (selectedOption)
            {
                case MENU_OPTION_PLAY:
                    loadingSavedGame = false;
                    loadingSlotIndex = -1;
                    currentState = MenuState.Loading;
                    Logger.Log("[MENU] Starting new game");
                    Logger.Log($"[MENU] loadingSavedGame flag is: {loadingSavedGame}");
                    break;
                case MENU_OPTION_LOAD:
                    if (SaveSystem.AnySaveExists())
                    {
                        currentState = MenuState.LoadMenu;
                        loadMenu.Open();
                        Logger.Log("[MENU] Opening load menu");
                    }
                    else
                    {
                        Logger.Log("[MENU] No save files found");
                    }
                    break;
                case MENU_OPTION_OPTIONS:
                    currentState = MenuState.Options;
                    optionsMenu.Open();
                    Logger.Log("[MENU] Opening options");
                    break;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (currentState == MenuState.MainMenu)
            {
                DrawMainMenu(spriteBatch, pixelTexture, font, screenWidth, screenHeight);
            }
            else if (currentState == MenuState.Loading)
            {
                DrawLoadingScreen(spriteBatch, pixelTexture, font, screenWidth, screenHeight);
            }
            else if (currentState == MenuState.Options)
            {
                DrawMainMenu(spriteBatch, pixelTexture, font, screenWidth, screenHeight);
                optionsMenu.Draw(spriteBatch, pixelTexture, font, screenWidth, screenHeight);
            }
            else if (currentState == MenuState.LoadMenu)
            {
                DrawMainMenu(spriteBatch, pixelTexture, font, screenWidth, screenHeight);
                loadMenu.Draw(spriteBatch, pixelTexture, font, screenWidth, screenHeight);
            }
        }

        private void DrawMainMenu(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (backgroundTexture != null)
            {
                Rectangle fullScreen = new Rectangle(0, 0, screenWidth, screenHeight);
                spriteBatch.Draw(backgroundTexture, fullScreen, Color.White);
            }
            else
            {
                spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.9f);
            }

            string title = "STARSHROUD HOLLOWS";
            Vector2 titleSize = font.MeasureString(title);
            float titleScale = 2.0f;
            Vector2 titlePos = new Vector2(
                (screenWidth - titleSize.X * titleScale) / 2,
                screenHeight * 0.45f
            );

            spriteBatch.DrawString(font, title, titlePos + new Vector2(3, 3), Color.Black, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, title, titlePos, Color.Gold, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

            int startY = (int)(screenHeight * 0.58f);
            int spacing = 65;

            if (loadingSavedGame && loadingSlotIndex >= 0)
            {
                string saveInfo = $"Ready to load: Slot {loadingSlotIndex + 1}";

                Vector2 infoSize = font.MeasureString(saveInfo);

                int infoY = startY - 100;
                Vector2 infoPos = new Vector2((screenWidth - infoSize.X) / 2, infoY);

                int padding = 20;
                int boxWidth = (int)infoSize.X + padding * 2;
                int boxHeight = 60;
                Rectangle infoBox = new Rectangle(
                    (screenWidth - boxWidth) / 2,
                    infoY - padding,
                    boxWidth,
                    boxHeight
                );
                spriteBatch.Draw(pixelTexture, infoBox, Color.Black * 0.7f);
                DrawBorder(spriteBatch, pixelTexture, infoBox, 2, Color.Yellow);

                spriteBatch.DrawString(font, saveInfo, infoPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, saveInfo, infoPos, Color.Cyan);
            }

            DrawMenuOption(spriteBatch, font, loadingSavedGame ? "Play (Load Save)" : "Play", startY, selectedOption == MENU_OPTION_PLAY, screenWidth);
            DrawMenuOption(spriteBatch, font, "Load", startY + spacing, selectedOption == MENU_OPTION_LOAD, screenWidth);
            DrawMenuOption(spriteBatch, font, "Options", startY + spacing * 2, selectedOption == MENU_OPTION_OPTIONS, screenWidth);

            string instructions = "Arrow Keys to Navigate | Enter to Select";
            Vector2 instructSize = font.MeasureString(instructions);
            Vector2 instructPos = new Vector2((screenWidth - instructSize.X) / 2, screenHeight - 80);
            spriteBatch.DrawString(font, instructions, instructPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, instructions, instructPos, Color.LightGray);
        }

        private void DrawMenuOption(SpriteBatch spriteBatch, SpriteFont font, string text, int y, bool selected, int screenWidth)
        {
            bool isDisabled = false;
            if (text == "Load" && !SaveSystem.AnySaveExists())
            {
                isDisabled = true;
                text = "Load (No Saves Found)";
            }

            Vector2 textSize = font.MeasureString(text);
            Vector2 textPos = new Vector2((screenWidth - textSize.X) / 2, y);

            Color textColor;
            if (isDisabled)
            {
                textColor = Color.DarkGray;
            }
            else
            {
                textColor = selected ? Color.Yellow : Color.White;
            }

            if (selected && !isDisabled)
            {
                string arrow = "> ";
                Vector2 arrowPos = new Vector2(textPos.X - 40, y);
                spriteBatch.DrawString(font, arrow, arrowPos, Color.Yellow);
            }

            spriteBatch.DrawString(font, text, textPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, text, textPos, textColor);
        }

        private void DrawLoadingScreen(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black);

            string loadingText = loadingSavedGame ? "Loading saved game..." : loadingMessage;
            Vector2 textSize = font.MeasureString(loadingText);
            Vector2 textPos = new Vector2((screenWidth - textSize.X) / 2, screenHeight / 2 - 50);
            spriteBatch.DrawString(font, loadingText, textPos, Color.White);

            int barWidth = 600;
            int barHeight = 40;
            int barX = (screenWidth - barWidth) / 2;
            int barY = screenHeight / 2 + 20;

            Rectangle barBg = new Rectangle(barX, barY, barWidth, barHeight);
            spriteBatch.Draw(pixelTexture, barBg, Color.DarkGray);

            Rectangle barFill = new Rectangle(barX, barY, (int)(barWidth * loadingProgress), barHeight);
            spriteBatch.Draw(pixelTexture, barFill, Color.Lime);

            DrawBorder(spriteBatch, pixelTexture, barBg, 3, Color.White);

            string percentText = $"{(int)(loadingProgress * 100)}%";
            Vector2 percentSize = font.MeasureString(percentText);
            Vector2 percentPos = new Vector2((screenWidth - percentSize.X) / 2, barY + barHeight + 20);
            spriteBatch.DrawString(font, percentText, percentPos, Color.White);
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