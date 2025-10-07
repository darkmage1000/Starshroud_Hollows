using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Claude4_5Terraria.Systems;

namespace Claude4_5Terraria.UI
{
    public class SaveMenu
    {
        public bool IsOpen { get; private set; }

        private int selectedSlot;
        private const int TOTAL_SLOTS = 3;
        private KeyboardState previousKeyState;
        private Action<int> onSaveCallback;

        // CACHED save info - only check once when menu opens
        private SaveSlotInfo[] cachedSlotInfo;

        public SaveMenu(Action<int> onSaveCallback)
        {
            this.onSaveCallback = onSaveCallback;
            IsOpen = false;
            selectedSlot = 0;
            previousKeyState = Keyboard.GetState();
            cachedSlotInfo = new SaveSlotInfo[TOTAL_SLOTS];
        }

        public void Open()
        {
            IsOpen = true;
            selectedSlot = 0;

            // Cache save slot info ONCE when menu opens, not every frame!
            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                cachedSlotInfo[i] = SaveSystem.GetSaveSlotInfo(i);
            }
        }

        public void Close()
        {
            IsOpen = false;
        }

        public void Update(float deltaTime, int screenWidth, int screenHeight)
        {
            if (!IsOpen) return;

            KeyboardState keyState = Keyboard.GetState();

            if (keyState.IsKeyDown(Keys.Down) && !previousKeyState.IsKeyDown(Keys.Down))
            {
                selectedSlot = (selectedSlot + 1) % TOTAL_SLOTS;
            }

            if (keyState.IsKeyDown(Keys.Up) && !previousKeyState.IsKeyDown(Keys.Up))
            {
                selectedSlot = (selectedSlot - 1 + TOTAL_SLOTS) % TOTAL_SLOTS;
            }

            if (keyState.IsKeyDown(Keys.Enter) && !previousKeyState.IsKeyDown(Keys.Enter))
            {
                onSaveCallback?.Invoke(selectedSlot);
                Close();
            }

            if (keyState.IsKeyDown(Keys.Escape) && !previousKeyState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            previousKeyState = keyState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!IsOpen) return;

            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.8f);

            int menuWidth = 600;
            int menuHeight = 400;
            int menuX = (screenWidth - menuWidth) / 2;
            int menuY = (screenHeight - menuHeight) / 2;

            Rectangle menuBg = new Rectangle(menuX, menuY, menuWidth, menuHeight);
            spriteBatch.Draw(pixelTexture, menuBg, Color.Black * 0.9f);
            DrawBorder(spriteBatch, pixelTexture, menuBg, 3, Color.White);

            string title = "Save Game";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, menuY + 30);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.Gold);

            int slotStartY = menuY + 100;
            int slotSpacing = 80;

            for (int i = 0; i < TOTAL_SLOTS; i++)
            {
                DrawSaveSlot(spriteBatch, pixelTexture, font, i, slotStartY + (i * slotSpacing),
                    i == selectedSlot, screenWidth, menuWidth);
            }

            string instructions = "Arrow Keys: Navigate | Enter: Save | Escape: Cancel";
            Vector2 instructSize = font.MeasureString(instructions);
            Vector2 instructPos = new Vector2((screenWidth - instructSize.X) / 2, menuY + menuHeight - 40);
            spriteBatch.DrawString(font, instructions, instructPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, instructions, instructPos, Color.LightGray);
        }

        private void DrawSaveSlot(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font,
            int slotIndex, int y, bool selected, int screenWidth, int menuWidth)
        {
            string slotText = $"Slot {slotIndex + 1}";

            // USE CACHED slot info instead of calling LoadGame every frame!
            SaveSlotInfo slotInfo = cachedSlotInfo[slotIndex];

            if (slotInfo.HasSave)
            {
                slotText += $" - {slotInfo.SaveName}";
            }
            else
            {
                slotText += " - Empty";
            }

            Vector2 textSize = font.MeasureString(slotText);
            Vector2 textPos = new Vector2((screenWidth - textSize.X) / 2, y);

            Color textColor = selected ? Color.Yellow : Color.White;

            if (selected)
            {
                int slotBgWidth = (int)textSize.X + 40;
                int slotBgHeight = (int)textSize.Y + 20;
                Rectangle slotBg = new Rectangle(
                    (int)textPos.X - 20,
                    (int)textPos.Y - 10,
                    slotBgWidth,
                    slotBgHeight
                );
                spriteBatch.Draw(pixelTexture, slotBg, Color.Yellow * 0.2f);
                DrawBorder(spriteBatch, pixelTexture, slotBg, 2, Color.Yellow);
            }

            spriteBatch.DrawString(font, slotText, textPos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, slotText, textPos, textColor);
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