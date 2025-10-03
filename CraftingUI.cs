using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Claude4_5Terraria.Systems;
using Claude4_5Terraria.Enums;
using System.Collections.Generic;

namespace Claude4_5Terraria.UI
{
    public class CraftingUI
    {
        private Inventory inventory;
        private bool nearCraftingBench;

        private const int RECIPE_SLOT_HEIGHT = 60;
        private const int PADDING = 10;
        private const int BOTTOM_MARGIN = 35; // Space for info text at bottom

        private MouseState previousMouseState;
        private List<Recipe> availableRecipes;

        // Store button rectangles for click detection
        private Dictionary<int, Rectangle> craftButtonRects;
        private int panelX, panelY, panelWidth, panelHeight;

        public CraftingUI(Inventory inventory)
        {
            this.inventory = inventory;
            nearCraftingBench = false;
            previousMouseState = Mouse.GetState();
            availableRecipes = new List<Recipe>();
            craftButtonRects = new Dictionary<int, Rectangle>();
        }

        public void SetNearCraftingBench(bool near)
        {
            nearCraftingBench = near;
        }

        public void Update(GameTime gameTime, Vector2 playerPosition, World.World world)
        {
            // Check if player is near a crafting bench
            CheckProximityToCraftingBench(playerPosition, world);

            // Update available recipes based on proximity
            availableRecipes = RecipeDatabase.GetAvailableRecipes(nearCraftingBench);

            // Recalculate button positions if we have valid panel bounds
            if (panelWidth > 0 && panelHeight > 0)
            {
                CalculateButtonPositions();
            }

            // Process clicks on craft buttons
            ProcessCraftingClicks();

            // Update mouse state
            previousMouseState = Mouse.GetState();
        }

        private void ProcessCraftingClicks()
        {
            MouseState currentMouseState = Mouse.GetState();

            // Only process new clicks
            if (currentMouseState.LeftButton != ButtonState.Pressed ||
                previousMouseState.LeftButton == ButtonState.Pressed)
            {
                return;
            }

            Point mousePoint = new Point(currentMouseState.X, currentMouseState.Y);
            Logger.Log($"[UI] Click detected at: {mousePoint}");

            // Check each recipe's craft button
            for (int i = 0; i < availableRecipes.Count; i++)
            {
                if (!craftButtonRects.ContainsKey(i))
                {
                    Logger.Log($"[UI] No button rect stored for recipe index {i}");
                    continue;
                }

                Rectangle buttonRect = craftButtonRects[i];

                if (buttonRect.Contains(mousePoint))
                {
                    Recipe recipe = availableRecipes[i];
                    Logger.Log($"[UI] Clicked craft button for: {GetItemName(recipe.Result)} (Button: {buttonRect})");

                    if (RecipeDatabase.CanCraft(recipe, inventory))
                    {
                        Logger.Log($"[UI] Attempting to craft: {GetItemName(recipe.Result)} x{recipe.ResultCount}");
                        RecipeDatabase.CraftRecipe(recipe, inventory);
                        return; // Only craft one item per click
                    }
                    else
                    {
                        Logger.Log($"[UI] Cannot craft - missing ingredients for {GetItemName(recipe.Result)}");
                    }
                }
            }

            Logger.Log($"[UI] Click at {mousePoint} did not hit any craft buttons. Total buttons: {craftButtonRects.Count}");
        }

        private void CheckProximityToCraftingBench(Vector2 playerPosition, World.World world)
        {
            if (world == null)
            {
                nearCraftingBench = false;
                Logger.Log("[CRAFTING] World is null, cannot check proximity");
                return;
            }

            int playerTileX = (int)(playerPosition.X / World.World.TILE_SIZE);
            int playerTileY = (int)(playerPosition.Y / World.World.TILE_SIZE);

            Logger.Log($"[CRAFTING] Checking proximity around player at tile ({playerTileX}, {playerTileY})");

            bool foundBench = false;

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
                            Logger.Log($"[CRAFTING] Found Crafting Bench at tile ({checkX}, {checkY})");
                            foundBench = true;
                            break;
                        }
                    }
                }
                if (foundBench) break;
            }

            bool wasNearBench = nearCraftingBench;
            nearCraftingBench = foundBench;

            if (wasNearBench != nearCraftingBench)
            {
                Logger.Log($"[CRAFTING] Proximity changed: {nearCraftingBench}");
                Logger.Log($"[CRAFTING] Available recipes: {RecipeDatabase.GetAvailableRecipes(nearCraftingBench).Count}");
            }
        }

        public void SetPanelBounds(int x, int y, int width, int height)
        {
            panelX = x;
            panelY = y;
            panelWidth = width;
            panelHeight = height;

            // Recalculate button positions
            CalculateButtonPositions();
        }

        private void CalculateButtonPositions()
        {
            craftButtonRects.Clear();

            int listStartY = panelY + 50;
            int maxY = panelY + panelHeight - BOTTOM_MARGIN;

            Logger.Log($"========== BUTTON POSITION CALCULATION ==========");
            Logger.Log($"[UI] Available recipes: {availableRecipes.Count}");
            Logger.Log($"[UI] Panel X={panelX}, Y={panelY}, W={panelWidth}, H={panelHeight}");
            Logger.Log($"[UI] List starts at Y={listStartY}");
            Logger.Log($"[UI] Max allowed Y={maxY}");
            Logger.Log($"[UI] Recipe slot height={RECIPE_SLOT_HEIGHT}, padding={PADDING}");

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                Recipe recipe = availableRecipes[i];
                int recipeY = listStartY + i * (RECIPE_SLOT_HEIGHT + PADDING);
                int recipeBottom = recipeY + RECIPE_SLOT_HEIGHT;

                Logger.Log($"--- Recipe {i}: {GetItemName(recipe.Result)} ---");
                Logger.Log($"    Position: Y={recipeY}, Bottom={recipeBottom}");
                Logger.Log($"    MaxY={maxY}, WouldFit={(recipeBottom <= maxY)}");

                // Check if recipe fits in panel
                if (recipeBottom > maxY)
                {
                    Logger.Log($"    SKIPPED: Recipe bottom ({recipeBottom}) exceeds maxY ({maxY})");
                    Logger.Log($"    Overflow by: {recipeBottom - maxY} pixels");
                    break;
                }

                int x = panelX + PADDING;
                int width = panelWidth - PADDING * 2;

                // Calculate craft button rectangle
                Rectangle craftButtonRect = new Rectangle(
                    x + width - 80,
                    recipeY + 15,
                    70,
                    30
                );

                craftButtonRects[i] = craftButtonRect;
                Logger.Log($"    Button stored: {craftButtonRect}");
            }

            Logger.Log($"[UI] TOTAL BUTTONS CREATED: {craftButtonRects.Count} out of {availableRecipes.Count} recipes");
            Logger.Log($"==================================================");
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int x, int y, int width, int height)
        {
            if (font == null || pixelTexture == null) return;

            // Store panel bounds for next update
            panelX = x;
            panelY = y;
            panelWidth = width;
            panelHeight = height;


            // Title
            string title = "CRAFTING";
            Vector2 titleSize = font.MeasureString(title);
            Vector2 titlePos = new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 10);
            spriteBatch.DrawString(font, title, titlePos, Color.White);

            // Recipe list
            int listStartY = panelY + 50;
            int maxY = panelY + panelHeight - BOTTOM_MARGIN;

            for (int i = 0; i < availableRecipes.Count; i++)
            {
                Recipe recipe = availableRecipes[i];
                int recipeY = listStartY + i * (RECIPE_SLOT_HEIGHT + PADDING);

                // Check if recipe fits in visible area
                if (recipeY + RECIPE_SLOT_HEIGHT > maxY)
                    break;

                DrawRecipe(spriteBatch, pixelTexture, font, panelX + PADDING, recipeY, panelWidth - PADDING * 2, recipe, i);
            }

            // Info text at bottom
            string infoText = nearCraftingBench ? "Near Crafting Bench" : "Basic Crafting";
            Vector2 infoSize = font.MeasureString(infoText);
            Vector2 infoPos = new Vector2(panelX + (panelWidth - infoSize.X) / 2, panelY + panelHeight - 25);
            spriteBatch.DrawString(font, infoText, infoPos, nearCraftingBench ? Color.LightGreen : Color.Gray);
        }

        private void DrawRecipe(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int x, int y, int width, Recipe recipe, int index)
        {
            bool canCraft = RecipeDatabase.CanCraft(recipe, inventory);
            Color bgColor = canCraft ? new Color(20, 60, 20) : new Color(60, 20, 20);
            Color borderColor = canCraft ? Color.Green : Color.Red;

            // Background
            Rectangle recipeRect = new Rectangle(x, y, width, RECIPE_SLOT_HEIGHT);
            spriteBatch.Draw(pixelTexture, recipeRect, bgColor * 0.8f);
            DrawBorder(spriteBatch, pixelTexture, recipeRect, 2, borderColor);

            // Result item icon
            int iconSize = 40;
            Rectangle iconRect = new Rectangle(x + 10, y + 10, iconSize, iconSize);
            Color itemColor = GetItemColor(recipe.Result);
            spriteBatch.Draw(pixelTexture, iconRect, itemColor);
            DrawBorder(spriteBatch, pixelTexture, iconRect, 1, Color.White * 0.5f);

            // Result name and count
            string resultText = $"{GetItemName(recipe.Result)} x{recipe.ResultCount}";
            Vector2 resultPos = new Vector2(x + iconSize + 20, y + 10);
            spriteBatch.DrawString(font, resultText, resultPos, Color.White);

            // Ingredients
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

            // Craft button (use stored rectangle)
            if (craftButtonRects.ContainsKey(index))
            {
                Rectangle craftButtonRect = craftButtonRects[index];
                Color buttonColor = canCraft ? Color.DarkGreen : Color.DarkGray;
                spriteBatch.Draw(pixelTexture, craftButtonRect, buttonColor);
                DrawBorder(spriteBatch, pixelTexture, craftButtonRect, 2, canCraft ? Color.Green : Color.Gray);

                string buttonText = "CRAFT";
                Vector2 buttonTextSize = font.MeasureString(buttonText);
                Vector2 buttonTextPos = new Vector2(
                    craftButtonRect.X + (craftButtonRect.Width - buttonTextSize.X) / 2,
                    craftButtonRect.Y + (craftButtonRect.Height - buttonTextSize.Y) / 2
                );
                Color textColor = canCraft ? Color.White : Color.DarkGray;
                spriteBatch.DrawString(font, buttonText, buttonTextPos, textColor);
            }
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
                case ItemType.Copper: return "Copper";
                case ItemType.Silver: return "Silver";
                case ItemType.Platinum: return "Platinum";
                case ItemType.Stick: return "Stick";
                case ItemType.Torch: return "Torch";
                case ItemType.WoodCraftingBench: return "Crafting Bench";
                case ItemType.WoodSword: return "Wood Sword";
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