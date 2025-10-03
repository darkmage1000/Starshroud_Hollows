using System.Collections.Generic;
using Claude4_5Terraria.Enums;

namespace Claude4_5Terraria.Systems
{
    public class RecipeIngredient
    {
        public ItemType ItemType { get; set; }
        public int Count { get; set; }

        public RecipeIngredient(ItemType itemType, int count)
        {
            ItemType = itemType;
            Count = count;
        }
    }

    public class Recipe
    {
        public ItemType Result { get; set; }
        public int ResultCount { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; }
        public bool RequiresCraftingBench { get; set; }

        public Recipe(ItemType result, int resultCount, bool requiresBench = false)
        {
            Result = result;
            ResultCount = resultCount;
            Ingredients = new List<RecipeIngredient>();
            RequiresCraftingBench = requiresBench;
        }

        public void AddIngredient(ItemType itemType, int count)
        {
            Ingredients.Add(new RecipeIngredient(itemType, count));
        }
    }

    public static class RecipeDatabase
    {
        private static List<Recipe> recipes;

        static RecipeDatabase()
        {
            recipes = new List<Recipe>();
            InitializeRecipes();
        }

        private static void InitializeRecipes()
        {
            // Sticks (2 wood = 4 sticks) - NO BENCH REQUIRED
            Recipe stickRecipe = new Recipe(ItemType.Stick, 4, false);
            stickRecipe.AddIngredient(ItemType.Wood, 2);
            recipes.Add(stickRecipe);

            // Wood Crafting Bench (10 wood) - NO BENCH REQUIRED
            Recipe benchRecipe = new Recipe(ItemType.WoodCraftingBench, 1, false);
            benchRecipe.AddIngredient(ItemType.Wood, 10);
            recipes.Add(benchRecipe);

            // Torches (1 stick + 1 coal = 3 torches) - NO BENCH REQUIRED
            Recipe torchRecipe = new Recipe(ItemType.Torch, 3, false);
            torchRecipe.AddIngredient(ItemType.Stick, 1);
            torchRecipe.AddIngredient(ItemType.Coal, 1);
            recipes.Add(torchRecipe);

            // Wood Sword (3 sticks + 7 wood) - REQUIRES CRAFTING BENCH
            Recipe swordRecipe = new Recipe(ItemType.WoodSword, 1, true);
            swordRecipe.AddIngredient(ItemType.Stick, 3);
            swordRecipe.AddIngredient(ItemType.Wood, 7);
            recipes.Add(swordRecipe);
        }

        public static List<Recipe> GetAvailableRecipes(bool nearCraftingBench)
        {
            List<Recipe> available = new List<Recipe>();

            Logger.Log($"[RECIPE] Getting recipes. Near bench: {nearCraftingBench}");

            foreach (Recipe recipe in recipes)
            {
                // Include recipe if:
                // - It doesn't require a bench, OR
                // - Player is near a bench
                if (!recipe.RequiresCraftingBench || nearCraftingBench)
                {
                    available.Add(recipe);
                    Logger.Log($"[RECIPE] Added: {recipe.Result} (RequiresBench: {recipe.RequiresCraftingBench})");
                }
                else
                {
                    Logger.Log($"[RECIPE] Skipped: {recipe.Result} (requires bench, not near one)");
                }
            }

            Logger.Log($"[RECIPE] Total available: {available.Count}");
            return available;
        }

        public static bool CanCraft(Recipe recipe, Inventory inventory)
        {
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (!inventory.HasItem(ingredient.ItemType, ingredient.Count))
                {
                    return false;
                }
            }

            return true;
        }

        public static void CraftRecipe(Recipe recipe, Inventory inventory)
        {
            // Verify we can still craft (double-check)
            if (!CanCraft(recipe, inventory))
            {
                System.Console.WriteLine($"Cannot craft {recipe.Result} - missing ingredients");
                return;
            }

            // Remove ingredients
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                bool removed = inventory.RemoveItem(ingredient.ItemType, ingredient.Count);
                if (!removed)
                {
                    System.Console.WriteLine($"ERROR: Failed to remove {ingredient.ItemType} x{ingredient.Count}");
                    return;
                }
            }

            // Add result
            bool added = inventory.AddItem(recipe.Result, recipe.ResultCount);
            if (added)
            {
                System.Console.WriteLine($"Successfully crafted: {recipe.Result} x{recipe.ResultCount}");
            }
            else
            {
                System.Console.WriteLine($"ERROR: Inventory full, could not add {recipe.Result}");
            }
        }
    }
}