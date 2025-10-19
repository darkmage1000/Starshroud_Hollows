using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace StarshroudHollows.UI
{
    public class LoadMenu
    {
        private bool isOpen;
        private MouseState previousMouseState;
        private KeyboardState previousKeyState;

        private Rectangle[] slotButtons;
        private Rectangle closeButton;
        private int hoveredSlot;
        private int selectedSlot;

        private Action<int> onLoadSlot;

        // CACHED save info - only load once when menu opens
        private Systems.SaveSlotInfo[] cachedSaveInfo;

        public LoadMenu(Action<int> onLoadSlot)
        {
            this.onLoadSlot = onLoadSlot;
            isOpen = false;
            previousMouseState = Mouse.GetState();
            previousKeyState = Keyboard.GetState();
            slotButtons = new Rectangle[3];
            cachedSaveInfo = new Systems.SaveSlotInfo[3];
            hoveredSlot = -1;
            selectedSlot = -1;
        }

        public bool IsOpen => isOpen;
        public int GetSelectedSlot() => selectedSlot;

        public void Open()
        {
            isOpen = true;
            selectedSlot = -1;

            // LOAD save info ONCE when opening menu
            for (int i = 0; i < 3; i++)
            {
                cachedSaveInfo[i] = Systems.SaveSystem.GetSaveSlotInfo(i);
            }
        }

        public void Close()
        {
            isOpen = false;
        }

        public void Update(int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (keyState.IsKeyDown(Keys.Escape) && !previousKeyState.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            int panelWidth = 700;
            int panelHeight = 500;
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;

            int slotHeight = 80;
            int slotSpacing = 20;
            int startY = panelY + 100;

            for (int i = 0; i < 3; i++)
            {
                slotButtons[i] = new Rectangle(
                    panelX + 50,
                    startY + i * (slotHeight + slotSpacing),
                    panelWidth - 100,
                    slotHeight
                );
            }

            closeButton = new Rectangle(panelX + panelWidth - 120, panelY + 20, 100, 40);

            Point mousePoint = new Point(mouseState.X, mouseState.Y);
            hoveredSlot = -1;

            for (int i = 0; i < 3; i++)
            {
                if (slotButtons[i].Contains(mousePoint))
                {
                    hoveredSlot = i;
                    break;
                }
            }

            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                if (closeButton.Contains(mousePoint))
                {
                    Close();
                }
                else if (hoveredSlot >= 0 && cachedSaveInfo[hoveredSlot].HasSave)
                {
                    selectedSlot = hoveredSlot;
                    onLoadSlot?.Invoke(selectedSlot);
                    Close();
                }
            }

            previousMouseState = mouseState;
            previousKeyState = keyState;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.7f);

            int panelWidth = 700;
            int panelHeight = 500;
            int panelX = (screenWidth - panelWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;

            Rectangle panelBg = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            spriteBatch.Draw(pixelTexture, panelBg, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, panelBg, 3, Color.White);

            string title = "LOAD GAME";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 30);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            spriteBatch.Draw(pixelTexture, closeButton, Color.DarkRed);
            DrawBorder(spriteBatch, pixelTexture, closeButton, 2, Color.White);
            string closeText = "Close";
            Vector2 closeTextSize = font.MeasureString(closeText);
            Vector2 closeTextPos = new Vector2(
                closeButton.X + (closeButton.Width - closeTextSize.X) / 2,
                closeButton.Y + (closeButton.Height - closeTextSize.Y) / 2
            );
            spriteBatch.DrawString(font, closeText, closeTextPos, Color.White);

            for (int i = 0; i < 3; i++)
            {
                Rectangle slotRect = slotButtons[i];

                // USE CACHED save info instead of loading from disk every frame!
                Systems.SaveSlotInfo slotInfo = cachedSaveInfo[i];

                bool canLoad = slotInfo.HasSave;
                Color slotColor = canLoad ? (hoveredSlot == i ? Color.Gray : Color.DarkGray) : Color.Black;
                spriteBatch.Draw(pixelTexture, slotRect, slotColor);
                DrawBorder(spriteBatch, pixelTexture, slotRect, 3,
                    canLoad && hoveredSlot == i ? Color.Yellow : Color.Gray);

                string slotText = $"Slot {i + 1}";
                Vector2 slotTextPos = new Vector2(slotRect.X + 20, slotRect.Y + 10);
                spriteBatch.DrawString(font, slotText, slotTextPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, slotText, slotTextPos, Color.Cyan);

                if (slotInfo.HasSave)
                {
                    string nameText = slotInfo.SaveName;
                    Vector2 nameTextPos = new Vector2(slotRect.X + 20, slotRect.Y + 35);
                    spriteBatch.DrawString(font, nameText, nameTextPos, Color.White);

                    string dateText = slotInfo.SaveDate;
                    Vector2 dateTextPos = new Vector2(slotRect.X + 20, slotRect.Y + 55);
                    spriteBatch.DrawString(font, dateText, dateTextPos, Color.LightGray);
                }
                else
                {
                    string emptyText = "Empty Slot";
                    Vector2 emptyTextPos = new Vector2(slotRect.X + 20, slotRect.Y + 45);
                    spriteBatch.DrawString(font, emptyText, emptyTextPos, Color.DarkGray);
                }
            }

            string instructions = "Click a slot to load | ESC to close";
            Vector2 instructSize = font.MeasureString(instructions);
            Vector2 instructPos = new Vector2(
                panelX + (panelWidth - instructSize.X) / 2,
                panelY + panelHeight - 30
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