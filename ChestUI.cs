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

        // NEW: Field to hold a reference to the World
        private World.World worldRef;
        
        // NEW: Chest name editing
        private bool isEditingName = false;
        private string editNameText = "";
        private KeyboardState previousKeyState;

        public ChestUI(World.World world)
        {
            isOpen = false;
            previousMouseState = Mouse.GetState();
            previousKeyState = Keyboard.GetState();
            this.worldRef = world;
            isEditingName = false;
            editNameText = "";
        }

        public void OpenChest(Chest chest, Inventory inventory)
        {
            currentChest = chest;
            playerInventory = inventory;
            isOpen = true;
            Logger.Log($"[CHEST UI] Opened {chest.Tier} chest named '{chest.Name}' at ({chest.Position.X}, {chest.Position.Y})");
        }

        public void Close()
        {
            isOpen = false;
            currentChest = null;
            draggedChestSlot = null;
            draggedPlayerSlot = null;
            isEditingName = false;
            editNameText = "";
        }

        public bool IsOpen => isOpen;

        public void Update()
        {
            if (!isOpen) return;

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();

            // Handle name editing text input
            if (isEditingName)
            {
                HandleNameInput(keyState);
            }
            else
            {
                // Close on ESC only when not editing
                if (keyState.IsKeyDown(Keys.Escape))
                {
                    Close();
                    return;
                }
                
                // Handle mouse interactions
                HandleMouseInput(mouseState);
            }

            previousMouseState = mouseState;
            previousKeyState = keyState;
        }

        private void HandleMouseInput(MouseState mouseState)
        {
            Point mousePos = new Point(mouseState.X, mouseState.Y);
            KeyboardState keyState = Keyboard.GetState();
            bool shiftPressed = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);

            // Check if clicking the "Edit Name" button
            Rectangle editNameButton = new Rectangle(820, 220, 80, 30);
            if (editNameButton.Contains(mousePos) && mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                isEditingName = true;
                editNameText = currentChest.Name;
                Logger.Log("[CHEST UI] Started editing chest name");
                return;
            }

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
        
        // NEW: Handle name editing input
        private void HandleNameInput(KeyboardState keyState)
        {
            // Get all pressed keys
            Keys[] pressedKeys = keyState.GetPressedKeys();
            
            foreach (Keys key in pressedKeys)
            {
                // Skip if key was already pressed last frame
                if (previousKeyState.IsKeyDown(key))
                    continue;
                    
                // Enter to confirm
                if (key == Keys.Enter)
                {
                    currentChest.Name = editNameText;
                    isEditingName = false;
                    Logger.Log($"[CHEST UI] Renamed chest to: {currentChest.Name}");
                    return;
                }
                // Escape to cancel
                else if (key == Keys.Escape)
                {
                    isEditingName = false;
                    editNameText = "";
                    Logger.Log("[CHEST UI] Cancelled name editing");
                    return;
                }
                // Backspace to delete
                else if (key == Keys.Back && editNameText.Length > 0)
                {
                    editNameText = editNameText.Substring(0, editNameText.Length - 1);
                }
                // Space
                else if (key == Keys.Space)
                {
                    if (editNameText.Length < 30)
                        editNameText += " ";
                }
                // Letters and numbers
                else
                {
                    string keyString = key.ToString();
                    bool shift = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);
                    
                    // Handle letters (A-Z)
                    if (keyString.Length == 1 && char.IsLetter(keyString[0]))
                    {
                        if (editNameText.Length < 30)
                        {
                            char c = shift ? keyString[0] : char.ToLower(keyString[0]);
                            editNameText += c;
                        }
                    }
                    // Handle numbers (D0-D9)
                    else if (keyString.StartsWith("D") && keyString.Length == 2 && char.IsDigit(keyString[1]))
                    {
                        if (editNameText.Length < 30)
                            editNameText += keyString[1];
                    }
                    // Handle NumPad numbers
                    else if (keyString.StartsWith("NumPad") && char.IsDigit(keyString[keyString.Length - 1]))
                    {
                        if (editNameText.Length < 30)
                            editNameText += keyString[keyString.Length - 1];
                    }
                }
            }
        }

        private Rectangle GetChestSlotRect(int index)
        {
            int col = index % CHEST_COLS;
            int row = index / CHEST_COLS;
            // Chest panel is at (595, 200) with size 330x320
            // Leave 80px for title/name area at top
            // Slots start at Y=280
            // Center the 5-column grid horizontally in the panel
            int panelCenterX = 595 + 330/2; // 760
            int gridWidth = 5 * SLOT_SIZE + 4 * PADDING; // 290
            int startX = panelCenterX - gridWidth/2; // 615
            int startY = 280;
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
            // Player panel is at (610, 600) with size 700x280
            // 10 columns: 10*50 + 9*10 = 590px wide
            // Center horizontally: 610 + 700/2 = 960, then 960 - 590/2 = 665
            int startX = 665;
            // Panel top edge: 600, add 15px margin = 615
            int startY = 615;
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

            // CENTERED: Draw chest panel with proper size for slots
            // Need: title area (70px) + 4 rows of slots (4*(50+10)-10 = 230px) + margins (10px each) = 320px height
            // Width: 5 columns (5*(50+10)-10 = 290px) + margins (20px each) = 330px width
            Rectangle chestPanel = new Rectangle(595, 200, 330, 320);
            spriteBatch.Draw(pixelTexture, chestPanel, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, chestPanel, 2, Color.White);

            // Draw chest name and title inside the panel
            int nameTextX = 605;
            int nameTextY = 210;

            // NEW: Draw Chest Name with Edit Button
            if (currentChest != null)
            {
                string displayText = isEditingName ? editNameText + "_" : currentChest.Name;
                spriteBatch.DrawString(font, displayText, new Vector2(nameTextX, nameTextY), Color.White);

                string chestTitle = $"{currentChest.Tier} Chest";
                spriteBatch.DrawString(font, chestTitle, new Vector2(nameTextX, nameTextY + 25), Color.Gray);
                
                // Draw "Edit Name" button (only if not already editing)
                if (!isEditingName)
                {
                    Rectangle editNameButton = new Rectangle(820, 220, 80, 30);
                    spriteBatch.Draw(pixelTexture, editNameButton, Color.DarkGreen);
                    DrawBorder(spriteBatch, pixelTexture, editNameButton, 1, Color.White);
                    Vector2 buttonTextSize = font.MeasureString("Rename");
                    spriteBatch.DrawString(font, "Rename", 
                        new Vector2(editNameButton.X + (editNameButton.Width - buttonTextSize.X) / 2,
                                    editNameButton.Y + (editNameButton.Height - buttonTextSize.Y) / 2),
                        Color.White);
                }
                else
                {
                    // Show editing instructions
                    spriteBatch.DrawString(font, "Enter=Save ESC=Cancel", 
                        new Vector2(nameTextX, nameTextY + 50), Color.Yellow);
                }
            }

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

            // Draw player inventory panel - ALL 40 SLOTS (4 rows of 10)
            // Height: 4 rows (4*50) + 3 gaps (3*10) + top margin (15) + bottom margin (15) = 260px
            // Width: 10 columns (590px) + margins (55px each side) = 700px  
            Rectangle playerPanel = new Rectangle(610, 600, 700, 280);
            spriteBatch.Draw(pixelTexture, playerPanel, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, playerPanel, 2, Color.White);

            // Draw player inventory title ABOVE the player panel (centered)
            string invTitle = "Player Inventory";
            Vector2 invTitleSize = font.MeasureString(invTitle);
            spriteBatch.DrawString(font, invTitle,
                new Vector2(960 - invTitleSize.X / 2, 570), Color.White);

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

            // MOVED: Draw instructions to the RIGHT of chest panel
            int instructionsX = 940;
            int instructionsY = 220;
            string[] instructionLines = new string[]
            {
                "Instructions:",
                "",
                "Drag items between",
                "chest and inventory",
                "",
                "Shift+Click for",
                "quick transfer",
                "",
                "Click 'Rename' to",
                "change chest name",
                "",
                "ESC to close"
            };
            
            int lineHeight = 25;
            for (int i = 0; i < instructionLines.Length; i++)
            {
                Color lineColor = i == 0 ? Color.White : Color.LightGray;
                spriteBatch.DrawString(font, instructionLines[i],
                    new Vector2(instructionsX, instructionsY + i * lineHeight), lineColor);
            }
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
