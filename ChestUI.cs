using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Systems;
using System;

namespace Claude4_5Terraria.UI
{
    public class ChestUI
    {
        private Chest currentChest;
        private Inventory playerInventory;
        private bool isOpen;
        
        private const int SLOT_SIZE = 50;
        private const int PADDING = 10;
        private const int CHEST_COLS = 5;
        private const int CHEST_ROWS = 4;

        private int? draggedChestSlot = null;
        private int? draggedPlayerSlot = null;
        private MouseState previousMouseState;

        public ChestUI()
        {
            isOpen = false;
            previousMouseState = Mouse.GetState();
        }

        public void OpenChest(Chest chest, Inventory inventory)
        {
            currentChest = chest;
            playerInventory = inventory;
            isOpen = true;
            Logger.Log($"[CHEST UI] Opened {chest.Tier} chest at ({chest.Position.X}, {chest.Position.Y})");
        }

        public void Close()
        {
            isOpen = false;
            currentChest = null;
            draggedChestSlot = null;
            draggedPlayerSlot = null;
        }

        public bool IsOpen => isOpen;

        public void Update()
        {
            if (!isOpen) return;

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();

            // Close on ESC
            if (keyState.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }

            // Handle mouse interactions
            HandleMouseInput(mouseState);

            previousMouseState = mouseState;
        }

        private void HandleMouseInput(MouseState mouseState)
        {
            Point mousePos = new Point(mouseState.X, mouseState.Y);
            KeyboardState keyState = Keyboard.GetState();
            bool shiftPressed = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);

            // Left click - start drag OR Shift+Click quick transfer
            if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                // Check chest slots
                for (int i = 0; i < ChestInventory.CHEST_SLOTS; i++)
                {
                    Rectangle slotRect = GetChestSlotRect(i);
                    if (slotRect.Contains(mousePos))
                    {
                        var slot = currentChest.Inventory.GetSlot(i);
                        if (slot != null && slot.ItemType != ItemType.None)
                        {
                            if (shiftPressed)
                            {
                                // SHIFT+CLICK: Quick move from chest to player inventory
                                playerInventory.AddItem(slot.ItemType, slot.Count);
                                currentChest.Inventory.SetSlot(i, ItemType.None, 0);
                                Logger.Log($"[CHEST UI] Quick-moved {slot.ItemType} from chest to inventory");
                            }
                            else
                            {
                                draggedChestSlot = i;
                                draggedPlayerSlot = null;
                            }
                        }
                        return;
                    }
                }

                // Check player inventory slots
                for (int i = 0; i < playerInventory.GetSlotCount(); i++)
                {
                    Rectangle slotRect = GetPlayerSlotRect(i);
                    if (slotRect.Contains(mousePos))
                    {
                        var slot = playerInventory.GetSlot(i);
                        if (slot != null && slot.ItemType != ItemType.None)
                        {
                            if (shiftPressed)
                            {
                                // SHIFT+CLICK: Quick move from player inventory to chest
                                if (currentChest.Inventory.AddItem(slot.ItemType, slot.Count))
                                {
                                    slot.ItemType = ItemType.None;
                                    slot.Count = 0;
                                    Logger.Log($"[CHEST UI] Quick-moved item from inventory to chest");
                                }
                            }
                            else
                            {
                                draggedPlayerSlot = i;
                                draggedChestSlot = null;
                            }
                        }
                        return;
                    }
                }
            }

            // Release - place item
            if (mouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                if (draggedChestSlot.HasValue)
                {
                    // Dragging from chest
                    var draggedSlot = currentChest.Inventory.GetSlot(draggedChestSlot.Value);
                    
                    // Check if dropping in player inventory
                    for (int i = 0; i < playerInventory.GetSlotCount(); i++)
                    {
                        Rectangle slotRect = GetPlayerSlotRect(i);
                        if (slotRect.Contains(mousePos))
                        {
                            // Transfer to player
                            playerInventory.AddItem(draggedSlot.ItemType, draggedSlot.Count);
                            currentChest.Inventory.SetSlot(draggedChestSlot.Value, ItemType.None, 0);
                            draggedChestSlot = null;
                            return;
                        }
                    }

                    // Check if swapping within chest
                    for (int i = 0; i < ChestInventory.CHEST_SLOTS; i++)
                    {
                        Rectangle slotRect = GetChestSlotRect(i);
                        if (slotRect.Contains(mousePos) && i != draggedChestSlot.Value)
                        {
                            // Swap slots
                            var targetSlot = currentChest.Inventory.GetSlot(i);
                            currentChest.Inventory.SetSlot(i, draggedSlot.ItemType, draggedSlot.Count);
                            currentChest.Inventory.SetSlot(draggedChestSlot.Value, targetSlot.ItemType, targetSlot.Count);
                            draggedChestSlot = null;
                            return;
                        }
                    }
                }
                else if (draggedPlayerSlot.HasValue)
                {
                    // Dragging from player inventory
                    var draggedSlot = playerInventory.GetSlot(draggedPlayerSlot.Value);
                    
                    // Check if dropping in chest
                    for (int i = 0; i < ChestInventory.CHEST_SLOTS; i++)
                    {
                        Rectangle slotRect = GetChestSlotRect(i);
                        if (slotRect.Contains(mousePos))
                        {
                            // Transfer to chest
                            if (currentChest.Inventory.AddItem(draggedSlot.ItemType, draggedSlot.Count))
                            {
                                playerInventory.GetSlot(draggedPlayerSlot.Value).ItemType = ItemType.None;
                                playerInventory.GetSlot(draggedPlayerSlot.Value).Count = 0;
                            }
                            draggedPlayerSlot = null;
                            return;
                        }
                    }

                    // Check if swapping within player inventory
                    for (int i = 0; i < playerInventory.GetSlotCount(); i++)
                    {
                        Rectangle slotRect = GetPlayerSlotRect(i);
                        if (slotRect.Contains(mousePos) && i != draggedPlayerSlot.Value)
                        {
                            // Let inventory handle swap
                            playerInventory.SwapSlots(draggedPlayerSlot.Value, i);
                            draggedPlayerSlot = null;
                            return;
                        }
                    }
                }

                // Cancel drag
                draggedChestSlot = null;
                draggedPlayerSlot = null;
            }
        }

        private Rectangle GetChestSlotRect(int index)
        {
            int col = index % CHEST_COLS;
            int row = index / CHEST_COLS;
            int startX = 760 - (CHEST_COLS * SLOT_SIZE + (CHEST_COLS - 1) * PADDING) / 2; // Centered at 960
            int startY = 230;
            return new Rectangle(
                startX + col * (SLOT_SIZE + PADDING),
                startY + row * (SLOT_SIZE + PADDING),
                SLOT_SIZE,
                SLOT_SIZE
            );
        }

        private Rectangle GetPlayerSlotRect(int index)
        {
            int col = index % 10;
            int row = index / 10;
            int startX = 960 - (10 * SLOT_SIZE + 9 * PADDING) / 2; // Centered at 960
            int startY = 600;
            return new Rectangle(
                startX + col * (SLOT_SIZE + PADDING),
                startY + row * (SLOT_SIZE + PADDING),
                SLOT_SIZE,
                SLOT_SIZE
            );
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
        {
            if (!isOpen) return;

            // Draw semi-transparent background
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, 1920, 1080), Color.Black * 0.7f);

            // CENTERED: Draw chest panel in center of screen
            Rectangle chestPanel = new Rectangle(560, 200, 400, 300);
            spriteBatch.Draw(pixelTexture, chestPanel, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, chestPanel, 2, Color.White);

            // Draw chest title ABOVE the chest panel (centered)
            string chestTitle = $"{currentChest.Tier} Chest";
            Vector2 titleSize = font.MeasureString(chestTitle);
            spriteBatch.DrawString(font, chestTitle, 
                new Vector2(960 - titleSize.X / 2, 170), Color.White);

            // Draw chest slots
            for (int i = 0; i < ChestInventory.CHEST_SLOTS; i++)
            {
                Rectangle slotRect = GetChestSlotRect(i);
                var slot = currentChest.Inventory.GetSlot(i);

                // Slot background
                Color slotColor = (draggedChestSlot.HasValue && draggedChestSlot.Value == i) 
                    ? Color.Gray * 0.5f : Color.Gray;
                spriteBatch.Draw(pixelTexture, slotRect, slotColor);
                DrawBorder(spriteBatch, pixelTexture, slotRect, 1, Color.White);

                // Draw item if present
                if (slot != null && slot.ItemType != ItemType.None)
                {
                    string itemName = slot.ItemType.ToString();
                    if (itemName.Length > 6) itemName = itemName.Substring(0, 6);
                    
                    Vector2 itemNameSize = font.MeasureString(itemName);
                    float scale = Math.Min(1.0f, (SLOT_SIZE - 10) / itemNameSize.X);
                    
                    spriteBatch.DrawString(font, itemName,
                        new Vector2(slotRect.X + 5, slotRect.Y + 5),
                        Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

                    // Draw count
                    string countText = slot.Count.ToString();
                    Vector2 countSize = font.MeasureString(countText);
                    spriteBatch.DrawString(font, countText,
                        new Vector2(slotRect.X + SLOT_SIZE - countSize.X - 5,
                                    slotRect.Y + SLOT_SIZE - countSize.Y - 5),
                        Color.Yellow);
                }
            }

            // Draw player inventory panel (centered)
            Rectangle playerPanel = new Rectangle(660, 570, 600, 150);
            spriteBatch.Draw(pixelTexture, playerPanel, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, playerPanel, 2, Color.White);

            // Draw player inventory title ABOVE the player panel (centered)
            string invTitle = "Your Inventory";
            Vector2 invTitleSize = font.MeasureString(invTitle);
            spriteBatch.DrawString(font, invTitle,
                new Vector2(960 - invTitleSize.X / 2, 540), Color.White);

            // Draw player inventory slots
            for (int i = 0; i < playerInventory.GetSlotCount(); i++)
            {
                Rectangle slotRect = GetPlayerSlotRect(i);
                var slot = playerInventory.GetSlot(i);

                // Slot background
                Color slotColor = (draggedPlayerSlot.HasValue && draggedPlayerSlot.Value == i)
                    ? Color.DarkGray * 0.5f : Color.DarkGray;
                spriteBatch.Draw(pixelTexture, slotRect, slotColor);
                DrawBorder(spriteBatch, pixelTexture, slotRect, 1, Color.White);

                // Draw item if present
                if (slot != null && slot.ItemType != ItemType.None)
                {
                    string itemName = slot.ItemType.ToString();
                    if (itemName.Length > 6) itemName = itemName.Substring(0, 6);

                    Vector2 itemNameSize = font.MeasureString(itemName);
                    float scale = Math.Min(1.0f, (SLOT_SIZE - 10) / itemNameSize.X);

                    spriteBatch.DrawString(font, itemName,
                        new Vector2(slotRect.X + 5, slotRect.Y + 5),
                        Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

                    // Draw count
                    string countText = slot.Count.ToString();
                    Vector2 countSize = font.MeasureString(countText);
                    spriteBatch.DrawString(font, countText,
                        new Vector2(slotRect.X + SLOT_SIZE - countSize.X - 5,
                                    slotRect.Y + SLOT_SIZE - countSize.Y - 5),
                        Color.Yellow);
                }
            }

            // Draw dragged item at mouse cursor
            MouseState mouseState = Mouse.GetState();
            if (draggedChestSlot.HasValue)
            {
                var slot = currentChest.Inventory.GetSlot(draggedChestSlot.Value);
                if (slot != null && slot.ItemType != ItemType.None)
                {
                    string itemName = slot.ItemType.ToString();
                    spriteBatch.DrawString(font, itemName,
                        new Vector2(mouseState.X + 10, mouseState.Y + 10), Color.Yellow);
                }
            }
            else if (draggedPlayerSlot.HasValue)
            {
                var slot = playerInventory.GetSlot(draggedPlayerSlot.Value);
                if (slot != null && slot.ItemType != ItemType.None)
                {
                    string itemName = slot.ItemType.ToString();
                    spriteBatch.DrawString(font, itemName,
                        new Vector2(mouseState.X + 10, mouseState.Y + 10), Color.Yellow);
                }
            }

            // Draw instructions below everything (centered)
            string instructions = "Drag items to move | Shift+Click for quick transfer | ESC to close";
            Vector2 instSize = font.MeasureString(instructions);
            spriteBatch.DrawString(font, instructions,
                new Vector2(960 - instSize.X / 2, 750), Color.LightGray);
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
