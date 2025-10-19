using System.Collections.Generic;
using StarshroudHollows.Enums;

namespace StarshroudHollows.Systems
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

            Recipe hammer = new Recipe(ItemType.Hammer, 1, requiresWoodBench: true);
            hammer.AddIngredient(ItemType.Wood, 8);
            hammer.AddIngredient(ItemType.Stone, 5);
            recipes.Add(hammer);

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

            Recipe ironBar = new Recipe(ItemType.IronBar, 1, requiresWoodBench: true);
            ironBar.AddIngredient(ItemType.Iron, 3);
            recipes.Add(ironBar);

            Recipe silverBar = new Recipe(ItemType.SilverBar, 1, requiresWoodBench: true);
            silverBar.AddIngredient(ItemType.Silver, 4);
            recipes.Add(silverBar);

            Recipe goldBar = new Recipe(ItemType.GoldBar, 1, requiresWoodBench: true);
            goldBar.AddIngredient(ItemType.Gold, 4);
            recipes.Add(goldBar);

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

            Recipe ironPickaxe = new Recipe(ItemType.IronPickaxe, 1, requiresCopperBench: true);
            ironPickaxe.AddIngredient(ItemType.IronBar, 12);
            ironPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(ironPickaxe);

            Recipe silverPickaxe = new Recipe(ItemType.SilverPickaxe, 1, requiresCopperBench: true);
            silverPickaxe.AddIngredient(ItemType.SilverBar, 12);
            silverPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(silverPickaxe);

            Recipe goldPickaxe = new Recipe(ItemType.GoldPickaxe, 1, requiresCopperBench: true);
            goldPickaxe.AddIngredient(ItemType.GoldBar, 12);
            goldPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(goldPickaxe);

            Recipe platinumPickaxe = new Recipe(ItemType.PlatinumPickaxe, 1, requiresCopperBench: true);
            platinumPickaxe.AddIngredient(ItemType.PlatinumBar, 12);
            platinumPickaxe.AddIngredient(ItemType.Stick, 4);
            recipes.Add(platinumPickaxe);

            Recipe silverChest = new Recipe(ItemType.SilverChest, 1, requiresCopperBench: true);
            silverChest.AddIngredient(ItemType.SilverBar, 15);
            recipes.Add(silverChest);

            // NEW: Bed recipe (wood bench)
            Recipe bed = new Recipe(ItemType.Bed, 1, requiresWoodBench: true);
            bed.AddIngredient(ItemType.Wood, 10);
            recipes.Add(bed);

            // NEW: Door recipe (wood bench)
            Recipe door = new Recipe(ItemType.Door, 1, requiresWoodBench: true);
            door.AddIngredient(ItemType.Wood, 6);
            recipes.Add(door);

            // NEW: Bucket recipe (copper bench)
            Recipe bucket = new Recipe(ItemType.EmptyBucket, 1, requiresCopperBench: true);
            bucket.AddIngredient(ItemType.SilverBar, 15);
            recipes.Add(bucket);

            // NEW: Troll Bait - boss summon item
            Recipe trollBait = new Recipe(ItemType.TrollBait, 1);
            trollBait.AddIngredient(ItemType.PieceOfFlesh, 25);
            recipes.Add(trollBait);

            // NEW: Summon Altar - for portal-based boss fights
            Recipe summonAltar = new Recipe(ItemType.SummonAltar, 1, requiresWoodBench: true);
            summonAltar.AddIngredient(ItemType.Stone, 20);
            summonAltar.AddIngredient(ItemType.Wood, 10);
            recipes.Add(summonAltar);

            // WALLS - Convert blocks to walls (4 walls per 1 block)
            Recipe dirtWall = new Recipe(ItemType.DirtWall, 4);
            dirtWall.AddIngredient(ItemType.Dirt, 1);
            recipes.Add(dirtWall);

            Recipe stoneWall = new Recipe(ItemType.StoneWall, 4);
            stoneWall.AddIngredient(ItemType.Stone, 1);
            recipes.Add(stoneWall);

            Recipe woodWall = new Recipe(ItemType.WoodWall, 4);
            woodWall.AddIngredient(ItemType.Wood, 1);
            recipes.Add(woodWall);

            Recipe copperWall = new Recipe(ItemType.CopperWall, 4, requiresWoodBench: true);
            copperWall.AddIngredient(ItemType.CopperBar, 1);
            recipes.Add(copperWall);

            Recipe ironWall = new Recipe(ItemType.IronWall, 4, requiresWoodBench: true);
            ironWall.AddIngredient(ItemType.IronBar, 1);
            recipes.Add(ironWall);

            Recipe silverWall = new Recipe(ItemType.SilverWall, 4, requiresWoodBench: true);
            silverWall.AddIngredient(ItemType.SilverBar, 1);
            recipes.Add(silverWall);

            Recipe goldWall = new Recipe(ItemType.GoldWall, 4, requiresWoodBench: true);
            goldWall.AddIngredient(ItemType.GoldBar, 1);
            recipes.Add(goldWall);

            Recipe platinumWall = new Recipe(ItemType.PlatinumWall, 4, requiresWoodBench: true);
            platinumWall.AddIngredient(ItemType.PlatinumBar, 1);
            recipes.Add(platinumWall);

            // ===== ARMOR RECIPES =====
            
            // WOOD ARMOR (Wood Bench Required)
            Recipe woodHelmet = new Recipe(ItemType.WoodHelmet, 1, requiresWoodBench: true);
            woodHelmet.AddIngredient(ItemType.Wood, 20);
            recipes.Add(woodHelmet);
            
            Recipe woodChestplate = new Recipe(ItemType.WoodChestplate, 1, requiresWoodBench: true);
            woodChestplate.AddIngredient(ItemType.Wood, 30);
            recipes.Add(woodChestplate);
            
            Recipe woodLeggings = new Recipe(ItemType.WoodLeggings, 1, requiresWoodBench: true);
            woodLeggings.AddIngredient(ItemType.Wood, 25);
            recipes.Add(woodLeggings);
            
            // COPPER ARMOR (Copper Bench Required)
            Recipe copperHelmet = new Recipe(ItemType.CopperHelmet, 1, requiresCopperBench: true);
            copperHelmet.AddIngredient(ItemType.CopperBar, 15);
            recipes.Add(copperHelmet);
            
            Recipe copperChestplate = new Recipe(ItemType.CopperChestplate, 1, requiresCopperBench: true);
            copperChestplate.AddIngredient(ItemType.CopperBar, 25);
            recipes.Add(copperChestplate);
            
            Recipe copperLeggings = new Recipe(ItemType.CopperLeggings, 1, requiresCopperBench: true);
            copperLeggings.AddIngredient(ItemType.CopperBar, 20);
            recipes.Add(copperLeggings);
            
            // IRON ARMOR (Copper Bench Required)
            Recipe ironHelmet = new Recipe(ItemType.IronHelmet, 1, requiresCopperBench: true);
            ironHelmet.AddIngredient(ItemType.IronBar, 15);
            recipes.Add(ironHelmet);
            
            Recipe ironChestplate = new Recipe(ItemType.IronChestplate, 1, requiresCopperBench: true);
            ironChestplate.AddIngredient(ItemType.IronBar, 25);
            recipes.Add(ironChestplate);
            
            Recipe ironLeggings = new Recipe(ItemType.IronLeggings, 1, requiresCopperBench: true);
            ironLeggings.AddIngredient(ItemType.IronBar, 20);
            recipes.Add(ironLeggings);
            
            // SILVER ARMOR (Copper Bench Required)
            Recipe silverHelmet = new Recipe(ItemType.SilverHelmet, 1, requiresCopperBench: true);
            silverHelmet.AddIngredient(ItemType.SilverBar, 18);
            recipes.Add(silverHelmet);
            
            Recipe silverChestplate = new Recipe(ItemType.SilverChestplate, 1, requiresCopperBench: true);
            silverChestplate.AddIngredient(ItemType.SilverBar, 30);
            recipes.Add(silverChestplate);
            
            Recipe silverLeggings = new Recipe(ItemType.SilverLeggings, 1, requiresCopperBench: true);
            silverLeggings.AddIngredient(ItemType.SilverBar, 24);
            recipes.Add(silverLeggings);
            
            // GOLD ARMOR (Copper Bench Required)
            Recipe goldHelmet = new Recipe(ItemType.GoldHelmet, 1, requiresCopperBench: true);
            goldHelmet.AddIngredient(ItemType.GoldBar, 18);
            recipes.Add(goldHelmet);
            
            Recipe goldChestplate = new Recipe(ItemType.GoldChestplate, 1, requiresCopperBench: true);
            goldChestplate.AddIngredient(ItemType.GoldBar, 30);
            recipes.Add(goldChestplate);
            
            Recipe goldLeggings = new Recipe(ItemType.GoldLeggings, 1, requiresCopperBench: true);
            goldLeggings.AddIngredient(ItemType.GoldBar, 24);
            recipes.Add(goldLeggings);
            
            // PLATINUM ARMOR (Copper Bench Required) - Best Tier!
            Recipe platinumHelmet = new Recipe(ItemType.PlatinumHelmet, 1, requiresCopperBench: true);
            platinumHelmet.AddIngredient(ItemType.PlatinumBar, 20);
            recipes.Add(platinumHelmet);
            
            Recipe platinumChestplate = new Recipe(ItemType.PlatinumChestplate, 1, requiresCopperBench: true);
            platinumChestplate.AddIngredient(ItemType.PlatinumBar, 35);
            recipes.Add(platinumChestplate);
            
            Recipe platinumLeggings = new Recipe(ItemType.PlatinumLeggings, 1, requiresCopperBench: true);
            platinumLeggings.AddIngredient(ItemType.PlatinumBar, 28);
            recipes.Add(platinumLeggings);

            // Note: Magic chests are NOT craftable - they can only be found
            // Note: Runic pickaxe is NOT craftable - only found in Magic chests
            // Note: Troll Bars and Troll Club are boss drops only
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
