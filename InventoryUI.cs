using Claude4_5Terraria.Enums;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Claude4_5Terraria.UI
{
    public class InventoryUI
    {
        private Inventory inventory;
        private MiningSystem miningSystem;
        private CraftingUI craftingUI;

        private const int SLOT_SIZE = 48;
        private const int SLOTS_PER_ROW = 10;
        private const int HOTBAR_SLOTS = 10;
        private const int PADDING = 4;
        private const int HOTBAR_BOTTOM_PADDING = 30;

        private KeyboardState previousKeyState;
        private MouseState previousMouseState;
        private bool isInventoryOpen;

        private Texture2D pixelTexture;
        private SpriteFont font;

        // Item sprites
        private System.Collections.Generic.Dictionary<ItemType, Texture2D> itemSprites;

        // Item dragging
        private int? draggedSlotIndex;
        private InventorySlot draggedItem;

        // Tooltip
        private int? hoveredSlotIndex;
        private Rectangle[] slotRectangles;

        // Cached screen dimensions
        private int cachedScreenHeight;

        public InventoryUI(Inventory inventory, MiningSystem miningSystem)
        {
            this.inventory = inventory;
            this.miningSystem = miningSystem;
            this.craftingUI = new CraftingUI(inventory);
            previousKeyState = Keyboard.GetState();
            previousMouseState = Mouse.GetState();
            isInventoryOpen = false;
            draggedSlotIndex = null;
            draggedItem = null;
            hoveredSlotIndex = null;
            slotRectangles = new Rectangle[40];
            cachedScreenHeight = 1080;
            itemSprites = new System.Collections.Generic.Dictionary<ItemType, Texture2D>();
        }

        public bool IsInventoryOpen => isInventoryOpen;

        public CraftingUI GetCraftingUI() => craftingUI;

        public void Initialize(Texture2D pixel, SpriteFont spriteFont)
        {
            pixelTexture = pixel;
            font = spriteFont;
        }

        public void LoadItemSprite(ItemType itemType, Texture2D sprite)
        {
            itemSprites[itemType] = sprite;
        }

        public void Update(GameTime gameTime, Vector2 playerPosition, World.World world, int screenHeight)
        {
            cachedScreenHeight = screenHeight;

            KeyboardState currentKeyState = Keyboard.GetState();
            MouseState currentMouseState = Mouse.GetState();

            // Toggle inventory
            if (currentKeyState.IsKeyDown(Keys.I) && previousKeyState.IsKeyUp(Keys.I))
            {
                isInventoryOpen = !isInventoryOpen;

                // Cancel dragging when closing inventory
                if (!isInventoryOpen && draggedSlotIndex.HasValue)
                {
                    CancelDrag();
                }
            }

            // Hotbar selection (only when inventory closed)
            if (!isInventoryOpen)
            {
                if (currentKeyState.IsKeyDown(Keys.D1) && previousKeyState.IsKeyUp(Keys.D1))
                    miningSystem.SetSelectedHotbarSlot(0);
                if (currentKeyState.IsKeyDown(Keys.D2) && previousKeyState.IsKeyUp(Keys.D2))
                    miningSystem.SetSelectedHotbarSlot(1);
                if (currentKeyState.IsKeyDown(Keys.D3) && previousKeyState.IsKeyUp(Keys.D3))
                    miningSystem.SetSelectedHotbarSlot(2);
                if (currentKeyState.IsKeyDown(Keys.D4) && previousKeyState.IsKeyUp(Keys.D4))
                    miningSystem.SetSelectedHotbarSlot(3);
                if (currentKeyState.IsKeyDown(Keys.D5) && previousKeyState.IsKeyUp(Keys.D5))
                    miningSystem.SetSelectedHotbarSlot(4);
                if (currentKeyState.IsKeyDown(Keys.D6) && previousKeyState.IsKeyUp(Keys.D6))
                    miningSystem.SetSelectedHotbarSlot(5);
                if (currentKeyState.IsKeyDown(Keys.D7) && previousKeyState.IsKeyUp(Keys.D7))
                    miningSystem.SetSelectedHotbarSlot(6);
                if (currentKeyState.IsKeyDown(Keys.D8) && previousKeyState.IsKeyUp(Keys.D8))
                    miningSystem.SetSelectedHotbarSlot(7);
                if (currentKeyState.IsKeyDown(Keys.D9) && previousKeyState.IsKeyUp(Keys.D9))
                    miningSystem.SetSelectedHotbarSlot(8);
                if (currentKeyState.IsKeyDown(Keys.D0) && previousKeyState.IsKeyUp(Keys.D0))
                    miningSystem.SetSelectedHotbarSlot(9);
            }

            // Handle inventory interactions
            if (isInventoryOpen)
            {
                HandleInventoryClicks(currentMouseState);
                UpdateHoveredSlot(currentMouseState);
                craftingUI.Update(gameTime, playerPosition, world);
            }
            else
            {
                // Check hotbar hover even when closed
                UpdateHotbarHover(currentMouseState);
            }

            previousKeyState = currentKeyState;
            previousMouseState = currentMouseState;
        }

        private void HandleInventoryClicks(MouseState currentMouseState)
        {
            Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
            KeyboardState keyState = Keyboard.GetState();
            bool shiftPressed = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);

            // Start dragging OR Shift+Click quick transfer
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released &&
                !draggedSlotIndex.HasValue)
            {
                for (int i = 0; i < slotRectangles.Length; i++)
                {
                    if (slotRectangles[i].Contains(mousePoint))
                    {
                        InventorySlot slot = inventory.GetSlot(i);
                        if (slot != null && !slot.IsEmpty())
                        {
                            if (shiftPressed)
                            {
                                // SHIFT+CLICK: Quick transfer between hotbar and storage
                                if (i < 10)
                                {
                                    // From hotbar to storage
                                    QuickMoveToStorage(i);
                                }
                                else
                                {
                                    // From storage to hotbar
                                    QuickMoveToHotbar(i);
                                }
                                Logger.Log($"[INVENTORY] Quick-moved {slot.ItemType} from slot {i}");
                            }
                            else
                            {
                                StartDrag(i);
                                Logger.Log($"[INVENTORY] Started dragging from slot {i}: {slot.ItemType} x{slot.Count}");
                            }
                            break;
                        }
                    }
                }
            }

            // Release drag
            if (currentMouseState.LeftButton == ButtonState.Released && draggedSlotIndex.HasValue)
            {
                // Find slot under mouse
                int? targetSlot = null;
                for (int i = 0; i < slotRectangles.Length; i++)
                {
                    if (slotRectangles[i].Contains(mousePoint))
                    {
                        targetSlot = i;
                        break;
                    }
                }

                if (targetSlot.HasValue)
                {
                    CompleteDrag(targetSlot.Value);
                }
                else
                {
                    CancelDrag();
                }
            }
        }

        private void StartDrag(int slotIndex)
        {
            InventorySlot slot = inventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty()) return;

            draggedSlotIndex = slotIndex;
            draggedItem = new InventorySlot
            {
                ItemType = slot.ItemType,
                Count = slot.Count
            };

            // Clear the source slot
            slot.ItemType = ItemType.None;
            slot.Count = 0;
        }

        private void CompleteDrag(int targetSlotIndex)
        {
            if (!draggedSlotIndex.HasValue || draggedItem == null) return;

            InventorySlot targetSlot = inventory.GetSlot(targetSlotIndex);

            if (targetSlot == null)
            {
                CancelDrag();
                return;
            }

            // Same slot - put it back
            if (targetSlotIndex == draggedSlotIndex.Value)
            {
                targetSlot.ItemType = draggedItem.ItemType;
                targetSlot.Count = draggedItem.Count;
                Logger.Log($"[INVENTORY] Returned item to same slot {targetSlotIndex}");
            }
            // Empty target slot - simple move
            else if (targetSlot.IsEmpty())
            {
                targetSlot.ItemType = draggedItem.ItemType;
                targetSlot.Count = draggedItem.Count;
                Logger.Log($"[INVENTORY] Moved {draggedItem.ItemType} from slot {draggedSlotIndex.Value} to {targetSlotIndex}");
            }
            // Same item type - stack
            else if (targetSlot.ItemType == draggedItem.ItemType)
            {
                targetSlot.Count += draggedItem.Count;
                Logger.Log($"[INVENTORY] Stacked {draggedItem.ItemType} x{draggedItem.Count} into slot {targetSlotIndex}");
            }
            // Different item - swap
            else
            {
                InventorySlot temp = new InventorySlot
                {
                    ItemType = targetSlot.ItemType,
                    Count = targetSlot.Count
                };
                targetSlot.ItemType = draggedItem.ItemType;
                targetSlot.Count = draggedItem.Count;
                inventory.GetSlot(draggedSlotIndex.Value).ItemType = temp.ItemType;
                inventory.GetSlot(draggedSlotIndex.Value).Count = temp.Count;
                Logger.Log($"[INVENTORY] Swapped {draggedItem.ItemType} with {temp.ItemType} between slots {draggedSlotIndex.Value} and {targetSlotIndex}");
            }

            draggedSlotIndex = null;
            draggedItem = null;
        }

        private void CancelDrag()
        {
            if (draggedSlotIndex.HasValue)
            {
                InventorySlot sourceSlot = inventory.GetSlot(draggedSlotIndex.Value);
                sourceSlot.ItemType = draggedItem.ItemType;
                sourceSlot.Count = draggedItem.Count;
                Logger.Log($"[INVENTORY] Cancelled drag - returned {draggedItem.ItemType} to slot {draggedSlotIndex.Value}");
            }
            draggedSlotIndex = null;
            draggedItem = null;
        }

        // NEW: Quick move from hotbar to storage
        private void QuickMoveToStorage(int hotbarIndex)
        {
            var sourceSlot = inventory.GetSlot(hotbarIndex);
            if (sourceSlot == null || sourceSlot.IsEmpty()) return;

            // Try to stack with existing items in storage first
            for (int i = 10; i < 40; i++)
            {
                var targetSlot = inventory.GetSlot(i);
                if (targetSlot.ItemType == sourceSlot.ItemType && !targetSlot.IsEmpty())
                {
                    targetSlot.Count += sourceSlot.Count;
                    sourceSlot.ItemType = ItemType.None;
                    sourceSlot.Count = 0;
                    return;
                }
            }

            // Find first empty slot in storage
            for (int i = 10; i < 40; i++)
            {
                var targetSlot = inventory.GetSlot(i);
                if (targetSlot.IsEmpty())
                {
                    targetSlot.ItemType = sourceSlot.ItemType;
                    targetSlot.Count = sourceSlot.Count;
                    sourceSlot.ItemType = ItemType.None;
                    sourceSlot.Count = 0;
                    return;
                }
            }
        }

        // NEW: Quick move from storage to hotbar
        private void QuickMoveToHotbar(int storageIndex)
        {
            var sourceSlot = inventory.GetSlot(storageIndex);
            if (sourceSlot == null || sourceSlot.IsEmpty()) return;

            // Try to stack with existing items in hotbar first
            for (int i = 0; i < 10; i++)
            {
                var targetSlot = inventory.GetSlot(i);
                if (targetSlot.ItemType == sourceSlot.ItemType && !targetSlot.IsEmpty())
                {
                    targetSlot.Count += sourceSlot.Count;
                    sourceSlot.ItemType = ItemType.None;
                    sourceSlot.Count = 0;
                    return;
                }
            }

            // Find first empty slot in hotbar
            for (int i = 0; i < 10; i++)
            {
                var targetSlot = inventory.GetSlot(i);
                if (targetSlot.IsEmpty())
                {
                    targetSlot.ItemType = sourceSlot.ItemType;
                    targetSlot.Count = sourceSlot.Count;
                    sourceSlot.ItemType = ItemType.None;
                    sourceSlot.Count = 0;
                    return;
                }
            }
        }

        // NEW: Helper method to retrieve weapon stats
        private (float Damage, float Recovery) GetWeaponStats(ItemType type)
        {
            switch (type)
            {
                case ItemType.WoodSword: return (2f, 0.5f); // 2 Damage, 0.5s Recovery
                // Add future weapon stats here
                default: return (0f, 0f);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont spriteFont, int screenWidth, int screenHeight)
        {
            MouseState mouseState = Mouse.GetState();

            int inventoryWidth = SLOTS_PER_ROW * (SLOT_SIZE + PADDING) + 20;
            int craftingWidth = 400;
            int totalWidth = inventoryWidth + craftingWidth + 20;
            int inventoryHeight = 4 * (SLOT_SIZE + PADDING) + 80;
            int craftingHeight = 500;
            int panelHeight = Math.Max(inventoryHeight, craftingHeight);
            int startX = (screenWidth - totalWidth) / 2;
            int panelY = (screenHeight - panelHeight) / 2;

            if (isInventoryOpen)
            {
                // Draw full inventory + crafting panels
                DrawInventoryPanel(spriteBatch, startX, panelY, inventoryWidth, panelHeight);
                int craftingX = startX + inventoryWidth + 20;
                DrawCraftingPanel(spriteBatch, craftingX, panelY, craftingWidth, panelHeight);
            }
            else
            {
                // Draw just hotbar when closed
                DrawHotbar(spriteBatch, screenWidth, screenHeight);
            }

            // Always draw dragged item and tooltip
            DrawDraggedItem(spriteBatch, mouseState);
            DrawTooltip(spriteBatch, mouseState);
        }

        private void DrawInventoryPanel(SpriteBatch spriteBatch, int panelX, int panelY, int panelWidth, int panelHeight)
        {
            // Draw panel background
            Rectangle panelBg = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            spriteBatch.Draw(pixelTexture, panelBg, Color.DarkSlateGray * 0.8f);
            DrawBorder(spriteBatch, panelBg, 3, Color.White);

            // Title
            if (font != null)
            {
                string title = "Inventory";
                Vector2 titleSize = font.MeasureString(title);
                Vector2 titlePos = new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 10);
                spriteBatch.DrawString(font, title, titlePos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, title, titlePos, Color.White);
            }

            int startX = panelX + 10;
            int startY = panelY + 50;

            // Clear slot rectangles first
            for (int i = 0; i < slotRectangles.Length; i++)
                slotRectangles[i] = Rectangle.Empty;

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < SLOTS_PER_ROW; col++)
                {
                    int slotIndex = row * SLOTS_PER_ROW + col;
                    int slotX = startX + col * (SLOT_SIZE + PADDING);
                    int slotY = startY + row * (SLOT_SIZE + PADDING);

                    Rectangle slotRect = new Rectangle(slotX, slotY, SLOT_SIZE, SLOT_SIZE);
                    slotRectangles[slotIndex] = slotRect;

                    if (slotIndex < HOTBAR_SLOTS)
                    {
                        spriteBatch.Draw(pixelTexture, slotRect, Color.DarkGoldenrod * 0.5f);
                    }
                    else
                    {
                        spriteBatch.Draw(pixelTexture, slotRect, Color.DarkGray);
                    }

                    DrawBorder(spriteBatch, slotRect, 2, Color.Gray);

                    // Don't draw item if it's being dragged
                    if (draggedSlotIndex.HasValue && draggedSlotIndex.Value == slotIndex)
                        continue;

                    InventorySlot slot = inventory.GetSlot(slotIndex);
                    if (slot != null && !slot.IsEmpty())
                    {
                        DrawItemInSlot(spriteBatch, slotX, slotY, slot);
                    }
                }
            }
        }

        private void DrawHotbar(SpriteBatch spriteBatch, int screenWidth, int screenHeight)
        {
            int startX = 20;
            int startY = screenHeight - SLOT_SIZE - HOTBAR_BOTTOM_PADDING;

            int selected = miningSystem.GetSelectedHotbarSlot();
            for (int i = 0; i < HOTBAR_SLOTS; i++)
            {
                int slotX = startX + i * (SLOT_SIZE + PADDING);
                Rectangle slotRect = new Rectangle(slotX, startY, SLOT_SIZE, SLOT_SIZE);

                Color bgColor = (i == selected) ? new Color(255, 255, 0) * 0.8f : Color.DarkGoldenrod * 0.5f;
                spriteBatch.Draw(pixelTexture, slotRect, bgColor);

                int borderThickness = (i == selected) ? 3 : 2;
                Color borderColor = (i == selected) ? Color.Yellow : Color.Gray;
                DrawBorder(spriteBatch, slotRect, borderThickness, borderColor);

                InventorySlot slot = inventory.GetSlot(i);
                if (slot != null && !slot.IsEmpty())
                {
                    // FIX: Only draw the item icon if the inventory is open,
                    // OR if this is NOT the currently selected hotbar item.
                    // This prevents the hotbar icon and the held item from drawing at the same time.
                    if (isInventoryOpen || i != selected)
                    {
                        DrawItemInSlot(spriteBatch, slotX, startY, slot);
                    }
                }
            }
        }

        private void DrawCraftingPanel(SpriteBatch spriteBatch, int panelX, int panelY, int panelWidth, int panelHeight)
        {
            Rectangle panelBg = new Rectangle(panelX, panelY, panelWidth, panelHeight);
            spriteBatch.Draw(pixelTexture, panelBg, Color.DarkSlateGray);
            DrawBorder(spriteBatch, panelBg, 3, Color.White);

            craftingUI.Draw(spriteBatch, pixelTexture, font, panelX, panelY, panelWidth, panelHeight);
        }

        private void DrawDraggedItem(SpriteBatch spriteBatch, MouseState mouseState)
        {
            if (draggedItem == null || pixelTexture == null) return;

            int iconSize = 32;
            int x = mouseState.X - iconSize / 2;
            int y = mouseState.Y - iconSize / 2;
            Rectangle iconRect = new Rectangle(x, y, iconSize, iconSize);

            // Draw sprite if available, otherwise use colored square
            if (itemSprites.ContainsKey(draggedItem.ItemType))
            {
                Texture2D itemTexture = itemSprites[draggedItem.ItemType];
                Rectangle sourceRect;

                // FIXED: Special handling for spritesheets when dragging
                if (draggedItem.ItemType == ItemType.RunicPickaxe)
                {
                    // Only show first frame when dragging
                    int frameWidth = itemTexture.Width / 2;
                    int frameHeight = itemTexture.Height / 2;
                    sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                }
                else if (draggedItem.ItemType == ItemType.RunicSword)
                {
                    // Runic Sword - CORRECTED: 5x2 grid, show first frame (top-left)
                    int frameWidth = itemTexture.Width / 4;
                    int frameHeight = itemTexture.Height / 2;
                    sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                }
                else if (draggedItem.ItemType == ItemType.RunicLaserWand)
                {
                    // Runic Laser Wand - 4x3 grid, show first frame (top-left)
                    int frameWidth = itemTexture.Width / 4; // CORRECTED
                    int frameHeight = itemTexture.Height / 3;
                    sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                }
                else
                {
                    // Regular items use full texture
                    sourceRect = new Rectangle(0, 0, itemTexture.Width, itemTexture.Height);
                }

                spriteBatch.Draw(itemTexture, iconRect, sourceRect, Color.White * 0.8f);
            }
            else
            {
                Color itemColor = GetItemColor(draggedItem.ItemType);
                spriteBatch.Draw(pixelTexture, iconRect, itemColor * 0.8f);
            }

            DrawBorder(spriteBatch, iconRect, 2, Color.White);

            if (font != null && draggedItem.Count > 1)
            {
                string countText = draggedItem.Count.ToString();
                Vector2 textSize = font.MeasureString(countText);
                Vector2 textPos = new Vector2(x + iconSize - textSize.X, y + iconSize - textSize.Y);
                spriteBatch.DrawString(font, countText, textPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, countText, textPos, Color.White);
            }
        }

        private void DrawTooltip(SpriteBatch spriteBatch, MouseState mouseState)
        {
            if (!hoveredSlotIndex.HasValue || font == null) return;

            InventorySlot slot = inventory.GetSlot(hoveredSlotIndex.Value);
            if (slot == null || slot.IsEmpty()) return;

            string itemName = GetItemName(slot.ItemType);

            // Start tooltip with Name and Count
            string tooltipText = $"{itemName} x{slot.Count}\n";

            // Add Weapon Stats if applicable
            if (slot.ItemType == ItemType.WoodSword)
            {
                var stats = GetWeaponStats(slot.ItemType);
                float totalCycleTime = stats.Recovery + 0.3f; // 0.3s is ATTACK_DURATION
                tooltipText += $"Damage: {stats.Damage}\n";
                tooltipText += $"Speed: {totalCycleTime:F1}s cycle (Slow)";
            }
            // else if (slot.ItemType == ItemType.CopperSword) { ... }

            Vector2 textSize = font.MeasureString(tooltipText);
            int tooltipX = mouseState.X + 15;
            int tooltipY = mouseState.Y + 15;

            Rectangle tooltipBg = new Rectangle(
                tooltipX - 5,
                tooltipY - 5,
                (int)textSize.X + 10,
                (int)textSize.Y + 10
            );

            spriteBatch.Draw(pixelTexture, tooltipBg, Color.Black * 0.9f);
            DrawBorder(spriteBatch, tooltipBg, 2, GetItemColor(slot.ItemType));

            spriteBatch.DrawString(font, tooltipText, new Vector2(tooltipX, tooltipY), Color.White);
        }

        private void UpdateHoveredSlot(MouseState mouseState)
        {
            Point mousePoint = new Point(mouseState.X, mouseState.Y);
            hoveredSlotIndex = null;

            for (int i = 0; i < slotRectangles.Length; i++)
            {
                if (slotRectangles[i].Contains(mousePoint))
                {
                    hoveredSlotIndex = i;
                    break;
                }
            }
        }

        private void UpdateHotbarHover(MouseState mouseState)
        {
            Point mousePoint = new Point(mouseState.X, mouseState.Y);
            hoveredSlotIndex = null;

            int hotbarY = cachedScreenHeight - SLOT_SIZE - HOTBAR_BOTTOM_PADDING;
            int startX = 20;
            for (int i = 0; i < HOTBAR_SLOTS; i++)
            {
                int slotX = startX + i * (SLOT_SIZE + PADDING);
                Rectangle hotbarRect = new Rectangle(slotX, hotbarY, SLOT_SIZE, SLOT_SIZE);
                if (hotbarRect.Contains(mousePoint))
                {
                    hoveredSlotIndex = i;
                    break;
                }
            }
        }

        private void DrawItemInSlot(SpriteBatch spriteBatch, int slotX, int slotY, InventorySlot slot)
        {
            if (pixelTexture == null) return;

            int iconSize = 32;
            int iconX = slotX + (SLOT_SIZE - iconSize) / 2;
            int iconY = slotY + (SLOT_SIZE - iconSize) / 2;
            Rectangle iconRect = new Rectangle(iconX, iconY, iconSize, iconSize);

            // Draw sprite if available, otherwise use colored square
            if (itemSprites.ContainsKey(slot.ItemType))
            {
                Texture2D itemTexture = itemSprites[slot.ItemType];
                Rectangle sourceRect;

                // FIXED: Special handling for spritesheets (only show first frame in inventory)
                if (slot.ItemType == ItemType.RunicPickaxe)
                {
                    // Runic pickaxe is a 2x2 grid (3 frames total)
                    // Frame 0 is at (0,0) - top-left corner
                    int frameWidth = itemTexture.Width / 2;
                    int frameHeight = itemTexture.Height / 2;
                    sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                }
                else if (slot.ItemType == ItemType.RunicSword)
                {
                    // Runic Sword - CORRECTED: 5x2 grid, show first frame (top-left)
                    int frameWidth = itemTexture.Width / 5;
                    int frameHeight = itemTexture.Height / 2;
                    sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                }
                else if (slot.ItemType == ItemType.RunicLaserWand)
                {
                    // Runic Laser Wand - 4x3 grid, show first frame (top-left)
                    int frameWidth = itemTexture.Width / 4; // CORRECTED
                    int frameHeight = itemTexture.Height / 3;
                    sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                }
                else
                {
                    // Regular items use full texture
                    sourceRect = new Rectangle(0, 0, itemTexture.Width, itemTexture.Height);
                }

                spriteBatch.Draw(itemTexture, iconRect, sourceRect, Color.White);
            }
            else
            {
                Color itemColor = GetItemColor(slot.ItemType);
                spriteBatch.Draw(pixelTexture, iconRect, itemColor);
            }

            DrawBorder(spriteBatch, iconRect, 1, Color.White * 0.5f);

            if (slot.Count > 1 && font != null)
            {
                string countText = slot.Count.ToString();
                Vector2 textSize = font.MeasureString(countText);
                Vector2 textPos = new Vector2(
                    slotX + SLOT_SIZE - textSize.X - 4,
                    slotY + SLOT_SIZE - textSize.Y - 2
                );

                spriteBatch.DrawString(font, countText, textPos + new Vector2(1, 1), Color.Black);
                spriteBatch.DrawString(font, countText, textPos, Color.White);
            }
        }

        private string GetItemName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Dirt: return "Dirt";
                case ItemType.Grass: return "Grass";
                case ItemType.Stone: return "Stone";
                case ItemType.Copper: return "Copper Ore";
                case ItemType.Silver: return "Silver Ore";
                case ItemType.Platinum: return "Platinum Ore";
                case ItemType.Wood: return "Wood";
                case ItemType.Coal: return "Coal";
                case ItemType.Stick: return "Stick";
                case ItemType.Torch: return "Torch";
                case ItemType.WoodCraftingBench: return "Crafting Bench";
                case ItemType.WoodSword: return "Wood Sword";
                case ItemType.WoodPickaxe: return "Wood Pickaxe";
                case ItemType.StonePickaxe: return "Stone Pickaxe";
                case ItemType.CopperPickaxe: return "Copper Pickaxe";
                case ItemType.SilverPickaxe: return "Silver Pickaxe";
                case ItemType.PlatinumPickaxe: return "Platinum Pickaxe";
                case ItemType.CopperBar: return "Copper Bar";
                case ItemType.SilverBar: return "Silver Bar";
                case ItemType.PlatinumBar: return "Platinum Bar";
                case ItemType.Acorn: return "Acorn";
                case ItemType.RunicPickaxe: return "Runic Pickaxe";
                case ItemType.RecallPotion: return "Recall Potion";
                case ItemType.WoodChest: return "Wood Chest";
                case ItemType.SilverChest: return "Silver Chest";
                case ItemType.MagicChest: return "Magic Chest";
                case ItemType.CopperCraftingBench: return "Copper Crafting Bench";
                default: return type.ToString();
            }
        }

        private Color GetItemColor(ItemType type)
        {
            switch (type)
            {
                case ItemType.Dirt: return new Color(150, 75, 0);
                case ItemType.Grass: return new Color(34, 139, 34);
                case ItemType.Stone: return new Color(128, 128, 128);
                case ItemType.Copper: return new Color(255, 140, 0);
                case ItemType.Silver: return new Color(192, 192, 192);
                case ItemType.Platinum: return new Color(144, 238, 144);
                case ItemType.Wood: return new Color(101, 67, 33);
                case ItemType.Coal: return new Color(40, 40, 40);
                case ItemType.Stick: return new Color(139, 90, 43);
                case ItemType.Torch: return new Color(255, 200, 100);
                case ItemType.WoodCraftingBench: return new Color(120, 80, 40);
                case ItemType.WoodSword: return new Color(180, 140, 100);
                case ItemType.WoodPickaxe: return new Color(80, 50, 30);
                case ItemType.StonePickaxe: return new Color(100, 100, 100);
                case ItemType.CopperPickaxe: return new Color(200, 100, 0);
                case ItemType.SilverPickaxe: return new Color(160, 160, 160);
                case ItemType.PlatinumPickaxe: return new Color(120, 200, 120);
                case ItemType.CopperBar: return new Color(255, 140, 0);
                case ItemType.SilverBar: return new Color(192, 192, 192);
                case ItemType.PlatinumBar: return new Color(229, 228, 226);
                case ItemType.Acorn: return new Color(139, 90, 43);
                case ItemType.RunicPickaxe: return new Color(100, 200, 255);
                case ItemType.RecallPotion: return new Color(100, 100, 255);
                case ItemType.WoodChest: return new Color(139, 90, 43);
                case ItemType.SilverChest: return new Color(192, 192, 192);
                case ItemType.MagicChest: return new Color(200, 100, 255);
                case ItemType.CopperCraftingBench: return new Color(200, 100, 0);
                default: return Color.White;
            }
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
        {
            if (pixelTexture == null) return;

            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}