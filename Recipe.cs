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
        public bool RequiresWoodBench { get; set; }
        public bool RequiresCopperBench { get; set; }

        public Recipe(ItemType result, int resultCount = 1, bool requiresWoodBench = false, bool requiresCopperBench = false)
        {
            Result = result;
            ResultCount = resultCount;
            Ingredients = new List<RecipeIngredient>();
            RequiresWoodBench = requiresWoodBench;
            RequiresCopperBench = requiresCopperBench;
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
            // Basic recipes (no bench required)
            Recipe stick = new Recipe(ItemType.Stick, 4);
            stick.AddIngredient(ItemType.Wood, 1);
            recipes.Add(stick);

            Recipe torch = new Recipe(ItemType.Torch, 3);
            torch.AddIngredient(ItemType.Stick, 1);
            torch.AddIngredient(ItemType.Coal, 1);
            recipes.Add(torch);

            Recipe woodCraftingBench = new Recipe(ItemType.WoodCraftingBench, 1);
            woodCraftingBench.AddIngredient(ItemType.Wood, 10);
            recipes.Add(woodCraftingBench);

            // Wood bench recipes
            Recipe woodSword = new Recipe(ItemType.WoodSword, 1, requiresWoodBench: true);
            woodSword.AddIngredient(ItemType.Wood, 7);
            recipes.Add(woodSword);

            Recipe woodPickaxe = new Recipe(ItemType.WoodPickaxe, 1, requiresWoodBench: true);
            woodPickaxe.AddIngredient(ItemType.Wood, 12);
            woodPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(woodPickaxe);

            Recipe stonePickaxe = new Recipe(ItemType.StonePickaxe, 1, requiresWoodBench: true);
            stonePickaxe.AddIngredient(ItemType.Stone, 15);
            stonePickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(stonePickaxe);

            // Chest recipes
            Recipe woodChest = new Recipe(ItemType.WoodChest, 1, requiresWoodBench: true);
            woodChest.AddIngredient(ItemType.Wood, 15);
            recipes.Add(woodChest);

            // Smelting recipes (wood bench)
            Recipe copperBar = new Recipe(ItemType.CopperBar, 1, requiresWoodBench: true);
            copperBar.AddIngredient(ItemType.Copper, 3);
            recipes.Add(copperBar);

            Recipe silverBar = new Recipe(ItemType.SilverBar, 1, requiresWoodBench: true);
            silverBar.AddIngredient(ItemType.Silver, 4);
            recipes.Add(silverBar);

            Recipe platinumBar = new Recipe(ItemType.PlatinumBar, 1, requiresWoodBench: true);
            platinumBar.AddIngredient(ItemType.Platinum, 5);
            recipes.Add(platinumBar);

            // Copper bench
            Recipe copperCraftingBench = new Recipe(ItemType.CopperCraftingBench, 1, requiresWoodBench: true);
            copperCraftingBench.AddIngredient(ItemType.CopperBar, 10);
            copperCraftingBench.AddIngredient(ItemType.Wood, 5);
            recipes.Add(copperCraftingBench);

            // Copper bench recipes
            Recipe copperPickaxe = new Recipe(ItemType.CopperPickaxe, 1, requiresCopperBench: true);
            copperPickaxe.AddIngredient(ItemType.CopperBar, 12);
            copperPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(copperPickaxe);

            Recipe silverPickaxe = new Recipe(ItemType.SilverPickaxe, 1, requiresCopperBench: true);
            silverPickaxe.AddIngredient(ItemType.SilverBar, 12);
            silverPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(silverPickaxe);

            Recipe platinumPickaxe = new Recipe(ItemType.PlatinumPickaxe, 1, requiresCopperBench: true);
            platinumPickaxe.AddIngredient(ItemType.PlatinumBar, 12);
            platinumPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(platinumPickaxe);

            Recipe silverChest = new Recipe(ItemType.SilverChest, 1, requiresCopperBench: true);
            silverChest.AddIngredient(ItemType.SilverBar, 15);
            recipes.Add(silverChest);

            // Note: Magic chests are NOT craftable - they can only be found
            // Note: Runic pickaxe is NOT craftable - only found in Magic chests
        }

        public static List<Recipe> GetAvailableRecipes(bool nearWoodBench, bool nearCopperBench)
        {
            List<Recipe> availableRecipes = new List<Recipe>();

            foreach (Recipe recipe in recipes)
            {
                if (recipe.RequiresCopperBench && !nearCopperBench)
                    continue;

                if (recipe.RequiresWoodBench && !nearWoodBench && !nearCopperBench)
                    continue;

                availableRecipes.Add(recipe);
            }

            return availableRecipes;
        }

        public static bool CanCraft(Recipe recipe, Inventory inventory)
        {
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                int totalCount = 0;

                // Count items across all inventory slots
                for (int i = 0; i < inventory.GetSlotCount(); i++)
                {
                    InventorySlot slot = inventory.GetSlot(i);
                    if (slot != null && slot.ItemType == ingredient.ItemType)
                    {
                        totalCount += slot.Count;
                    }
                }

                if (totalCount < ingredient.Count)
                {
                    return false;
                }
            }
            return true;
        }

        public static void CraftRecipe(Recipe recipe, Inventory inventory)
        {
            if (!CanCraft(recipe, inventory))
            {
                Logger.Log($"[CRAFT] Cannot craft {recipe.Result} - missing ingredients");
                return;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                inventory.RemoveItem(ingredient.ItemType, ingredient.Count);
            }

            inventory.AddItem(recipe.Result, recipe.ResultCount);
            Logger.Log($"[CRAFT] Crafted {recipe.Result} x{recipe.ResultCount}");
        }
    }
}
