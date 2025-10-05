using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Claude4_5Terraria.UI
{
    public class SaveMenu
    {
        private bool isOpen;
        private MouseState previousMouseState;
        private KeyboardState previousKeyState;

        private Rectangle[] slotButtons;
        private Rectangle closeButton;
        private int hoveredSlot;

        private Action<int> onSaveToSlot;
        private string statusMessage;
        private float statusMessageTimer;

        public SaveMenu(Action<int> onSaveToSlot)
        {
            this.onSaveToSlot = onSaveToSlot;
            isOpen = false;
            previousMouseState = Mouse.GetState();
            previousKeyState = Keyboard.GetState();
            slotButtons = new Rectangle[3];
            hoveredSlot = -1;
            statusMessage = "";
            statusMessageTimer = 0f;
        }

        public bool IsOpen => isOpen;

        public void Open()
        {
            isOpen = true;
            statusMessage = "";
        }

        public void Close()
        {
            isOpen = false;
        }

        public void SetStatusMessage(string message)
        {
            statusMessage = message;
            statusMessageTimer = 3f;
        }

        public void Update(float deltaTime, int screenWidth, int screenHeight)
        {
            if (!isOpen) return;

            if (statusMessageTimer > 0)
            {
                statusMessageTimer -= deltaTime;
            }

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
                else if (hoveredSlot >= 0)
                {
                    onSaveToSlot?.Invoke(hoveredSlot);
                    SetStatusMessage($"Game saved to Slot {hoveredSlot + 1}!");
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

            string title = "SAVE GAME";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 30);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            spriteBatch.Draw(pixelTexture, closeButton, hoveredSlot == -2 ? Color.Red : Color.DarkRed);
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
                Systems.SaveSlotInfo slotInfo = Systems.SaveSystem.GetSaveSlotInfo(i);

                Color slotColor = hoveredSlot == i ? Color.Gray : Color.DarkGray;
                spriteBatch.Draw(pixelTexture, slotRect, slotColor);
                DrawBorder(spriteBatch, pixelTexture, slotRect, 3, hoveredSlot == i ? Color.Yellow : Color.Gray);

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

            if (statusMessageTimer > 0)
            {
                Vector2 statusSize = font.MeasureString(statusMessage);
                Vector2 statusPos = new Vector2(
                    panelX + (panelWidth - statusSize.X) / 2,
                    panelY + panelHeight - 60
                );
                spriteBatch.DrawString(font, statusMessage, statusPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, statusMessage, statusPos, Color.Lime);
            }

            string instructions = "Click a slot to save | ESC to close";
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