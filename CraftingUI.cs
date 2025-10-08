using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.Enums;
using System.Collections.Generic;
using System;

namespace Claude4_5Terraria.UI
{
    public class CraftingUI
    {
        private Inventory inventory;
        private bool nearCraftingBench;
        private bool nearCopperBench;

        private const int RECIPE_SLOT_HEIGHT = 60;
        private const int PADDING = 10;
        private const int BOTTOM_MARGIN = 35;

        private MouseState previousMouseState;
        private List<Recipe> availableRecipes;

        private Dictionary<int, Rectangle> craftButtonRects;
        private int panelX, panelY, panelWidth, panelHeight;
        
        // Item sprites
        private Dictionary<ItemType, Texture2D> itemSprites;

        // Scrolling
        private float scrollOffset = 0f;
        private const float SCROLL_SPEED = 60f;
        private MouseState previousMouseStateForScroll;

        // Scrollbar
        private bool isDraggingScrollbar = false;
        private int scrollbarDragStartY = 0;
        private float scrollOffsetAtDragStart = 0f;
        private Rectangle scrollbarTrack;
        private Rectangle scrollbarHandle;
        private Rectangle scrollUpButton;
        private Rectangle scrollDownButton;
        private const int SCROLLBAR_WIDTH = 20;
        private const int SCROLL_BUTTON_HEIGHT = 20;

        public CraftingUI(Inventory inventory)
        {
            this.inventory = inventory;
            itemSprites = new Dictionary<ItemType, Texture2D>();
            nearCraftingBench = false;
            nearCopperBench = false;
            previousMouseState = Mouse.GetState();
            previousMouseStateForScroll = Mouse.GetState();
            availableRecipes = new List<Recipe>();
            craftButtonRects = new Dictionary<int, Rectangle>();
        }
        
        public void LoadItemSprite(ItemType itemType, Texture2D sprite)
        {
            itemSprites[itemType] = sprite;
        }

        public void Update(GameTime gameTime, Vector2 playerPosition, World.World world)
        {
            CheckProximityToCraftingBench(playerPosition, world);
            availableRecipes = RecipeDatabase.GetAvailableRecipes(nearCraftingBench, nearCopperBench);

            MouseState currentMouseState = Mouse.GetState();

            if (panelWidth > 0 && panelHeight > 0)
            {
                CalculateScrollbarRects();
                HandleScrollbarInput(currentMouseState);
                CalculateButtonPositions();
            }

            ProcessCraftingClicks();

            previousMouseState = currentMouseState;
            previousMouseStateForScroll = currentMouseState;
        }

        private void CalculateScrollbarRects()
        {
            int contentHeight = availableRecipes.Count * (RECIPE_SLOT_HEIGHT + PADDING);
            int viewHeight = panelHeight - 50 - BOTTOM_MARGIN;

            if (contentHeight <= viewHeight)
            {
                scrollbarTrack = Rectangle.Empty;
                scrollbarHandle = Rectangle.Empty;
                scrollUpButton = Rectangle.Empty;
                scrollDownButton = Rectangle.Empty;
                return;
            }

            int trackX = panelX + panelWidth - SCROLLBAR_WIDTH - 5;
            int trackY = panelY + 50 + SCROLL_BUTTON_HEIGHT;
            int trackHeight = viewHeight - (SCROLL_BUTTON_HEIGHT * 2);
            scrollbarTrack = new Rectangle(trackX, trackY, SCROLLBAR_WIDTH, trackHeight);

            scrollUpButton = new Rectangle(trackX, panelY + 50, SCROLLBAR_WIDTH, SCROLL_BUTTON_HEIGHT);
            scrollDownButton = new Rectangle(trackX, trackY + trackHeight, SCROLLBAR_WIDTH, SCROLL_BUTTON_HEIGHT);

            float scrollableHeight = contentHeight - viewHeight;
            float handleHeightRatio = (float)viewHeight / contentHeight;
            int handleHeight = Math.Max(20, (int)(trackHeight * handleHeightRatio));

            float scrollProgress = scrollableHeight > 0 ? scrollOffset / scrollableHeight : 0;
            int handleY = trackY + (int)((trackHeight - handleHeight) * scrollProgress);

            scrollbarHandle = new Rectangle(trackX, handleY, SCROLLBAR_WIDTH, handleHeight);
        }

        private void HandleScrollbarInput(MouseState mouseState)
        {
            Point mousePoint = new Point(mouseState.X, mouseState.Y);

            int contentHeight = availableRecipes.Count * (RECIPE_SLOT_HEIGHT + PADDING);
            int viewHeight = panelHeight - 50 - BOTTOM_MARGIN;
            float maxScroll = Math.Max(0, contentHeight - viewHeight);

            if (maxScroll <= 0)
            {
                scrollOffset = 0;
                return;
            }

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                if (!isDraggingScrollbar && scrollbarHandle.Contains(mousePoint))
                {
                    isDraggingScrollbar = true;
                    scrollbarDragStartY = mouseState.Y;
                    scrollOffsetAtDragStart = scrollOffset;
                }

                if (isDraggingScrollbar)
                {
                    int dragDelta = mouseState.Y - scrollbarDragStartY;
                    int trackHeight = scrollbarTrack.Height - scrollbarHandle.Height;

                    if (trackHeight > 0)
                    {
                        float scrollDelta = ((float)dragDelta / trackHeight) * maxScroll;
                        scrollOffset = scrollOffsetAtDragStart + scrollDelta;
                        scrollOffset = Math.Max(0, Math.Min(scrollOffset, maxScroll));
                    }
                }
            }
            else
            {
                isDraggingScrollbar = false;
            }

            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseStateForScroll.LeftButton == ButtonState.Released &&
                scrollUpButton.Contains(mousePoint))
            {
                scrollOffset = Math.Max(0, scrollOffset - SCROLL_SPEED);
            }

            if (mouseState.LeftButton == ButtonState.Pressed &&
                previousMouseStateForScroll.LeftButton == ButtonState.Released &&
                scrollDownButton.Contains(mousePoint))
            {
                scrollOffset = Math.Min(maxScroll, scrollOffset + SCROLL_SPEED);
            }

            int wheelDelta = mouseState.ScrollWheelValue - previousMouseStateForScroll.ScrollWheelValue;
            if (wheelDelta != 0)
            {
                scrollOffset -= wheelDelta / 120f * 30f;
                scrollOffset = Math.Max(0, Math.Min(scrollOffset, maxScroll));
            }
        }

        private void CalculateButtonPositions()
        {
            craftButtonRects.Clear();

            int listStartY = panelY + 50;
            int maxY = panelY + panelHeight - BOTTOM_MARGIN;
            int contentWidth = panelWidth - PADDING * 2 - SCROLLBAR_WIDTH - 10;

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                Recipe recipe = availableRecipes[i];

                int recipeY = listStartY + i * (RECIPE_SLOT_HEIGHT + PADDING) - (int)scrollOffset;
                int recipeBottom = recipeY + RECIPE_SLOT_HEIGHT;

                if (recipeBottom < listStartY || recipeY > maxY)
                {
                    continue;
                }

                int x = panelX + PADDING;

                Rectangle craftButtonRect = new Rectangle(
                    x + contentWidth - 80,
                    recipeY + 15,
                    70,
                    30
                );

                craftButtonRects[i] = craftButtonRect;
            }
        }

        private void ProcessCraftingClicks()
        {
            MouseState currentMouseState = Mouse.GetState();

            if (currentMouseState.LeftButton != ButtonState.Pressed ||
                previousMouseState.LeftButton == ButtonState.Pressed)
            {
                return;
            }

            Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                if (!craftButtonRects.ContainsKey(i))
                {
                    continue;
                }

                Rectangle buttonRect = craftButtonRects[i];

                if (buttonRect.Contains(mousePoint))
                {
                    Recipe recipe = availableRecipes[i];

                    if (RecipeDatabase.CanCraft(recipe, inventory))
                    {
                        RecipeDatabase.CraftRecipe(recipe, inventory);
                        Logger.Log($"[CRAFT] Crafted {recipe.Result} x{recipe.ResultCount}");
                        return;
                    }
                }
            }
        }

        private void CheckProximityToCraftingBench(Vector2 playerPosition, World.World world)
        {
            if (world == null)
            {
                nearCraftingBench = false;
                nearCopperBench = false;
                return;
            }

            int playerTileX = (int)(playerPosition.X / World.World.TILE_SIZE);
            int playerTileY = (int)(playerPosition.Y / World.World.TILE_SIZE);

            bool foundWoodBench = false;
            bool foundCopperBench = false;

            for (int dx = -5; dx <= 5; dx++)
            {
                for (int dy = -5; dy <= 5; dy++)
                {
                    int checkX = playerTileX + dx;
                    int checkY = playerTileY + dy;

                    World.Tile tile = world.GetTile(checkX, checkY);
                    if (tile != null && tile.IsActive)
                    {
                        if (tile.Type == TileType.WoodCraftingBench)
                        {
                            foundWoodBench = true;
                        }
                        else if (tile.Type == TileType.CopperCraftingBench)
                        {
                            foundCopperBench = true;
                        }
                    }
                }
            }

            nearCraftingBench = foundWoodBench;
            nearCopperBench = foundCopperBench;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int x, int y, int width, int height)
        {
            if (font == null || pixelTexture == null) return;

            panelX = x;
            panelY = y;
            panelWidth = width;
            panelHeight = height;

            string title = "CRAFTING";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 10);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            Rectangle scissorRect = new Rectangle(
                panelX + PADDING,
                panelY + 50,
                panelWidth - PADDING * 2 - SCROLLBAR_WIDTH - 10,
                panelHeight - 50 - BOTTOM_MARGIN
            );

            Rectangle oldScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
            RasterizerState rasterizerState = new RasterizerState { ScissorTestEnable = true };

            spriteBatch.End();
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                rasterizerState: rasterizerState
            );

            spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

            int listStartY = panelY + 50;
            int maxY = panelY + panelHeight - BOTTOM_MARGIN;
            int contentWidth = panelWidth - PADDING * 2 - SCROLLBAR_WIDTH - 10;

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                Recipe recipe = availableRecipes[i];
                int recipeY = listStartY + i * (RECIPE_SLOT_HEIGHT + PADDING) - (int)scrollOffset;

                if (recipeY + RECIPE_SLOT_HEIGHT < panelY + 50 || recipeY > maxY)
                    continue;

                DrawRecipe(spriteBatch, pixelTexture, font, panelX + PADDING, recipeY, contentWidth, recipe, i);
            }

            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = oldScissor;
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp
            );

            // Draw button text AFTER scissor clipping (so it's not clipped)
            for (int i = 0; i < availableRecipes.Count; i++)
            {
                if (!craftButtonRects.ContainsKey(i)) continue;

                Recipe recipe = availableRecipes[i];
                int recipeY = listStartY + i * (RECIPE_SLOT_HEIGHT + PADDING) - (int)scrollOffset;

                if (recipeY + RECIPE_SLOT_HEIGHT < panelY + 50 || recipeY > maxY)
                    continue;

                Rectangle craftButtonRect = craftButtonRects[i];
                bool canCraft = RecipeDatabase.CanCraft(recipe, inventory);

                string buttonText = "CRAFT";
                Vector2 buttonTextSize = font.MeasureString(buttonText);
                Vector2 buttonTextPos = new Vector2(
                    craftButtonRect.X + (craftButtonRect.Width - buttonTextSize.X) / 2,
                    craftButtonRect.Y + (craftButtonRect.Height - buttonTextSize.Y) / 2
                );
                Color textColor = canCraft ? Color.White : Color.LightGray;
                spriteBatch.DrawString(font, buttonText, buttonTextPos, textColor);
            }

            DrawScrollbar(spriteBatch, pixelTexture, font);

            string infoText = nearCopperBench ? "Copper Crafting Bench" :
                              nearCraftingBench ? "Wood Crafting Bench" :
                              "Basic Crafting";
            Color infoColor = nearCopperBench ? Color.Orange :
                              nearCraftingBench ? Color.LightGreen :
                              Color.Gray;
            Vector2 infoSize = font.MeasureString(infoText);
            Vector2 infoPos = new Vector2(panelX + (panelWidth - infoSize.X) / 2, panelY + panelHeight - 25);
            spriteBatch.DrawString(font, infoText, infoPos, infoColor);
        }

        private void DrawRecipe(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int x, int y, int width, Recipe recipe, int index)
        {
            bool canCraft = RecipeDatabase.CanCraft(recipe, inventory);
            Color bgColor = canCraft ? new Color(20, 60, 20) : new Color(60, 20, 20);
            Color borderColor = canCraft ? Color.Green : Color.Red;

            Rectangle recipeRect = new Rectangle(x, y, width, RECIPE_SLOT_HEIGHT);
            spriteBatch.Draw(pixelTexture, recipeRect, bgColor * 0.8f);
            DrawBorder(spriteBatch, pixelTexture, recipeRect, 2, borderColor);

            int iconSize = 40;
            Rectangle iconRect = new Rectangle(x + 10, y + 10, iconSize, iconSize);
            
            // Draw sprite if available, otherwise use colored square
            if (itemSprites.ContainsKey(recipe.Result))
            {
                spriteBatch.Draw(itemSprites[recipe.Result], iconRect, Color.White);
            }
            else
            {
                Color itemColor = GetItemColor(recipe.Result);
                spriteBatch.Draw(pixelTexture, iconRect, itemColor);
            }
            DrawBorder(spriteBatch, pixelTexture, iconRect, 1, Color.White * 0.5f);

            string resultText = $"{GetItemName(recipe.Result)} x{recipe.ResultCount}";
            Vector2 resultPos = new Vector2(x + iconSize + 20, y + 10);
            spriteBatch.DrawString(font, resultText, resultPos, Color.White);

            int ingredientY = y + 35;
            string ingredientsText = "Needs: ";

            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                RecipeIngredient ingredient = recipe.Ingredients[i];

                if (i > 0)
                    ingredientsText += ", ";

                ingredientsText += $"{GetItemName(ingredient.ItemType)} x{ingredient.Count}";
            }

            Vector2 ingredientPos = new Vector2(x + iconSize + 20, ingredientY);
            Color ingredientColor = canCraft ? Color.LightGreen : Color.LightCoral;
            spriteBatch.DrawString(font, ingredientsText, ingredientPos, ingredientColor);

            // Draw button background (text drawn separately outside scissor clip)
            if (craftButtonRects.ContainsKey(index))
            {
                Rectangle craftButtonRect = craftButtonRects[index];
                Color buttonColor = canCraft ? Color.DarkGreen : Color.DarkGray;
                spriteBatch.Draw(pixelTexture, craftButtonRect, buttonColor);
                DrawBorder(spriteBatch, pixelTexture, craftButtonRect, 2, canCraft ? Color.Green : Color.Gray);
            }
        }

        private void DrawScrollbar(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
        {
            if (scrollbarTrack.IsEmpty) return;

            spriteBatch.Draw(pixelTexture, scrollbarTrack, Color.DarkGray * 0.5f);
            DrawBorder(spriteBatch, pixelTexture, scrollbarTrack, 1, Color.Gray);

            spriteBatch.Draw(pixelTexture, scrollUpButton, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, scrollUpButton, 2, Color.White * 0.7f);

            string upArrow = "^";
            Vector2 upSize = font.MeasureString(upArrow);
            Vector2 upPos = new Vector2(
                scrollUpButton.X + (scrollUpButton.Width - upSize.X) / 2,
                scrollUpButton.Y + (scrollUpButton.Height - upSize.Y) / 2
            );
            spriteBatch.DrawString(font, upArrow, upPos, Color.White);

            spriteBatch.Draw(pixelTexture, scrollDownButton, Color.DarkSlateGray);
            DrawBorder(spriteBatch, pixelTexture, scrollDownButton, 2, Color.White * 0.7f);

            string downArrow = "v";
            Vector2 downSize = font.MeasureString(downArrow);
            Vector2 downPos = new Vector2(
                scrollDownButton.X + (scrollDownButton.Width - downSize.X) / 2,
                scrollDownButton.Y + (scrollDownButton.Height - downSize.Y) / 2
            );
            spriteBatch.DrawString(font, downArrow, downPos, Color.White);

            Color handleColor = isDraggingScrollbar ? Color.LightGray : Color.Gray;
            spriteBatch.Draw(pixelTexture, scrollbarHandle, handleColor);
            DrawBorder(spriteBatch, pixelTexture, scrollbarHandle, 2, Color.White);
        }

        private string GetItemName(ItemType type)
        {
            switch (type)
            {
                case ItemType.Dirt: return "Dirt";
                case ItemType.Grass: return "Grass";
                case ItemType.Stone: return "Stone";
                case ItemType.Wood: return "Wood";
                case ItemType.Coal: return "Coal";
                case ItemType.Copper: return "Copper Ore";
                case ItemType.Silver: return "Silver Ore";
                case ItemType.Platinum: return "Platinum Ore";
                case ItemType.Stick: return "Stick";
                case ItemType.Torch: return "Torch";
                case ItemType.CopperBar: return "Copper Bar";
                case ItemType.SilverBar: return "Silver Bar";
                case ItemType.PlatinumBar: return "Platinum Bar";
                case ItemType.WoodCraftingBench: return "Wood Bench";
                case ItemType.CopperCraftingBench: return "Copper Bench";
                case ItemType.WoodSword: return "Wood Sword";
                case ItemType.WoodPickaxe: return "Wood Pickaxe";
                case ItemType.StonePickaxe: return "Stone Pickaxe";
                case ItemType.CopperPickaxe: return "Copper Pickaxe";
                case ItemType.SilverPickaxe: return "Silver Pickaxe";
                case ItemType.PlatinumPickaxe: return "Platinum Pickaxe";
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
                case ItemType.CopperBar: return new Color(255, 140, 0);
                case ItemType.SilverBar: return new Color(192, 192, 192);
                case ItemType.PlatinumBar: return new Color(229, 228, 226);
                case ItemType.WoodCraftingBench: return new Color(120, 80, 40);
                case ItemType.CopperCraftingBench: return new Color(200, 100, 20);
                case ItemType.WoodSword: return new Color(180, 140, 100);
                case ItemType.WoodPickaxe: return new Color(139, 90, 43);
                case ItemType.StonePickaxe: return new Color(128, 128, 128);
                case ItemType.CopperPickaxe: return new Color(255, 140, 0);
                case ItemType.SilverPickaxe: return new Color(192, 192, 192);
                case ItemType.PlatinumPickaxe: return new Color(144, 238, 144);
                default: return Color.White;
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
