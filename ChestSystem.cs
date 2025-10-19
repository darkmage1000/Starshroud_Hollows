using System;
using System.Collections.Generic;
using StarshroudHollows.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StarshroudHollows.Systems
{
    // === NOTE: This file assumes ChestItemData and ChestData are correctly defined in SaveData.cs. ===

    public enum ChestTier
    {
        Wood,
        Silver,
        Magic
    }

    public class ChestInventory
    {
        public const int CHEST_SLOTS = 20;
        public const int MAX_STACK_SIZE = 999;
        private InventorySlot[] slots;

        public ChestInventory()
        {
            slots = new InventorySlot[CHEST_SLOTS];
            for (int i = 0; i < CHEST_SLOTS; i++)
            {
                slots[i] = new InventorySlot();
                // Initialize as truly empty
                slots[i].ItemType = ItemType.None;
                slots[i].Count = 0;
            }
        }

        public InventorySlot GetSlot(int index)
        {
            if (index >= 0 && index < CHEST_SLOTS)
                return slots[index];
            return null;
        }

        public void SetSlot(int index, ItemType itemType, int count)
        {
            if (index >= 0 && index < CHEST_SLOTS)
            {
                slots[index].ItemType = itemType;
                slots[index].Count = count;
            }
        }

        public bool AddItem(ItemType itemType, int count)
        {
            // Try to stack with existing items first
            for (int i = 0; i < CHEST_SLOTS; i++)
            {
                if (slots[i].ItemType == itemType && slots[i].Count < MAX_STACK_SIZE)
                {
                    int spaceLeft = MAX_STACK_SIZE - slots[i].Count;
                    int amountToAdd = Math.Min(count, spaceLeft);
                    slots[i].Count += amountToAdd;
                    count -= amountToAdd;

                    if (count <= 0)
                        return true;
                }
            }

            // Find empty slots for remaining items
            while (count > 0)
            {
                int emptySlot = -1;
                for (int i = 0; i < CHEST_SLOTS; i++)
                {
                    if (slots[i].ItemType == ItemType.None)
                    {
                        emptySlot = i;
                        break;
                    }
                }

                if (emptySlot == -1)
                    return false; // Chest is full

                int amountToAdd = Math.Min(count, MAX_STACK_SIZE);
                slots[emptySlot].ItemType = itemType;
                slots[emptySlot].Count = amountToAdd;
                count -= amountToAdd;
            }

            return true;
        }

        public bool HasSpace()
        {
            for (int i = 0; i < CHEST_SLOTS; i++)
            {
                if (slots[i].ItemType == ItemType.None)
                    return true;
            }
            return false;
        }

        public List<ChestItemData> GetSaveData()
        {
            List<ChestItemData> items = new List<ChestItemData>();
            for (int i = 0; i < CHEST_SLOTS; i++)
            {
                if (slots[i].ItemType != ItemType.None && slots[i].Count > 0)
                {
                    items.Add(new ChestItemData
                    {
                        SlotIndex = i,
                        ItemType = (int)slots[i].ItemType,
                        Count = slots[i].Count
                    });
                }
            }
            return items;
        }

        public void LoadFromData(List<ChestItemData> items)
        {
            // Clear all slots first
            for (int i = 0; i < CHEST_SLOTS; i++)
            {
                slots[i].ItemType = ItemType.None;
                slots[i].Count = 0;
            }

            // Load saved items
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.SlotIndex >= 0 && item.SlotIndex < CHEST_SLOTS)
                    {
                        slots[item.SlotIndex].ItemType = (ItemType)item.ItemType;
                        slots[item.SlotIndex].Count = item.Count;
                    }
                }
            }
        }
    }

    public class Chest
    {
        public Point Position { get; set; }
        public ChestTier Tier { get; set; }
        public ChestInventory Inventory { get; set; }
        public bool IsNaturallyGenerated { get; set; }
        // NEW: Chest Name field
        public string Name { get; set; }

        public Chest(Point position, ChestTier tier, bool naturallyGenerated = false)
        {
            Position = position;
            Tier = tier;
            Inventory = new ChestInventory();
            IsNaturallyGenerated = naturallyGenerated;
            // Default name if not explicitly set
            this.Name = $"{tier} Chest";
        }

        public TileType GetTileType()
        {
            switch (Tier)
            {
                case ChestTier.Wood: return TileType.WoodChest;
                case ChestTier.Silver: return TileType.SilverChest;
                case ChestTier.Magic: return TileType.MagicChest;
                default: return TileType.WoodChest;
            }
        }

        public ItemType GetItemType()
        {
            switch (Tier)
            {
                case ChestTier.Wood: return ItemType.WoodChest;
                case ChestTier.Silver: return ItemType.SilverChest;
                case ChestTier.Magic: return ItemType.MagicChest;
                default: return ItemType.WoodChest;
            }
        }
    }

    public class ChestSystem
    {
        private Dictionary<Point, Chest> chests;

        public ChestSystem()
        {
            chests = new Dictionary<Point, Chest>();
        }

        // UPDATED: Added customName parameter
        public void PlaceChest(Point position, ChestTier tier, bool naturallyGenerated = false, string customName = null)
        {
            if (!chests.ContainsKey(position))
            {
                Chest chest = new Chest(position, tier, naturallyGenerated);

                // Apply custom name if provided (used by player placement)
                if (!string.IsNullOrEmpty(customName))
                {
                    chest.Name = customName;
                }

                chests[position] = chest;
                Logger.Log($"[CHEST] Placed {tier} chest at ({position.X}, {position.Y})");
            }
        }

        public Chest GetChest(Point position)
        {
            if (chests.ContainsKey(position))
                return chests[position];
            return null;
        }

        public bool RemoveChest(Point position, Inventory playerInventory)
        {
            if (chests.ContainsKey(position))
            {
                Chest chest = chests[position];

                // Transfer all items to player inventory
                for (int i = 0; i < ChestInventory.CHEST_SLOTS; i++)
                {
                    var slot = chest.Inventory.GetSlot(i);
                    if (slot != null && slot.ItemType != ItemType.None && slot.Count > 0)
                    {
                        playerInventory.AddItem(slot.ItemType, slot.Count);
                    }
                }

                // Give the chest item back to player
                playerInventory.AddItem(chest.GetItemType(), 1);

                chests.Remove(position);
                Logger.Log($"[CHEST] Removed {chest.Tier} chest at ({position.X}, {position.Y})");
                return true;
            }
            return false;
        }

        public void GenerateChestLoot(Chest chest, Random random)
        {
            if (!chest.IsNaturallyGenerated)
                return; // Only generate loot for naturally spawned chests

            switch (chest.Tier)
            {
                case ChestTier.Wood:
                    // Torches (5-10), Copper Bars (3-7), 1 Recall Potion
                    chest.Inventory.AddItem(ItemType.Torch, random.Next(5, 11));
                    chest.Inventory.AddItem(ItemType.CopperBar, random.Next(3, 8));
                    chest.Inventory.AddItem(ItemType.RecallPotion, 1);
                    break;

                case ChestTier.Silver:
                    // 3 Platinum Ore, Silver Bars (5-10), Torches (10-15), 3 Recall Potions, 30% chance Empty Bucket
                    chest.Inventory.AddItem(ItemType.Platinum, 3);
                    chest.Inventory.AddItem(ItemType.SilverBar, random.Next(5, 11));
                    chest.Inventory.AddItem(ItemType.Torch, random.Next(10, 16));
                    chest.Inventory.AddItem(ItemType.RecallPotion, 3);
                    // 30% chance for bucket
                    if (random.NextDouble() < 0.3)
                    {
                        chest.Inventory.AddItem(ItemType.EmptyBucket, 1);
                    }
                    break;

                case ChestTier.Magic:
                    // Runic Pickaxe (best tier), 10 Recall Potions
                    chest.Inventory.AddItem(ItemType.RunicPickaxe, 1);
                    chest.Inventory.AddItem(ItemType.RecallPotion, 10);
                    break;
            }

            Logger.Log($"[CHEST] Generated loot for {chest.Tier} chest at ({chest.Position.X}, {chest.Position.Y})");
        }

        // UPDATED: Save the chest name
        public List<ChestData> GetSaveData()
        {
            List<ChestData> data = new List<ChestData>();
            foreach (var kvp in chests)
            {
                data.Add(new ChestData
                {
                    X = kvp.Key.X,
                    Y = kvp.Key.Y,
                    Tier = (int)kvp.Value.Tier,
                    IsNaturallyGenerated = kvp.Value.IsNaturallyGenerated,
                    Items = kvp.Value.Inventory.GetSaveData(),
                    // FIX: This line now requires the Name property to exist in the external ChestData
                    Name = kvp.Value.Name
                });
            }
            return data;
        }

        // UPDATED: Load the chest name
        public void LoadFromData(List<ChestData> data)
        {
            chests.Clear();
            if (data != null)
            {
                foreach (var chestData in data)
                {
                    Point position = new Point(chestData.X, chestData.Y);
                    Chest chest = new Chest(position, (ChestTier)chestData.Tier, chestData.IsNaturallyGenerated);
                    chest.Inventory.LoadFromData(chestData.Items);
                    // FIX: This line now requires the Name property to exist in the external ChestData
                    chest.Name = chestData.Name ?? $"{chest.Tier} Chest";
                    chests[position] = chest;
                }
            }
            Logger.Log($"[CHEST] Loaded {chests.Count} chests from save data");
        }

        public int GetChestCount()
        {
            return chests.Count;
        }
    }
}