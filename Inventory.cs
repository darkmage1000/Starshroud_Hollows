using System.Collections.Generic;
using Claude4_5Terraria.Enums;

namespace Claude4_5Terraria.Systems
{
    public class InventorySlot
    {
        public ItemType ItemType { get; set; }
        public int Count { get; set; }

        public InventorySlot()
        {
            ItemType = ItemType.None;
            Count = 0;
        }

        public bool IsEmpty()
        {
            return Count == 0 || ItemType == ItemType.None;
        }
    }

    public class Inventory
    {
        private const int INVENTORY_SIZE = 40;
        private InventorySlot[] slots;

        public Inventory()
        {
            slots = new InventorySlot[INVENTORY_SIZE];
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                slots[i] = new InventorySlot();
            }

            // Give starter items
            AddItem(ItemType.RunicPickaxe, 1);
            AddItem(ItemType.Torch, 50);
            AddItem(ItemType.RecallPotion, 5);  // NEW: 5 starter Recall Potions
        }

        public bool AddItem(ItemType itemType, int amount = 1)
        {
            // Try to stack with existing item
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (slots[i].ItemType == itemType && !slots[i].IsEmpty())
                {
                    slots[i].Count += amount;
                    return true;
                }
            }

            // Find empty slot
            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (slots[i].IsEmpty())
                {
                    slots[i].ItemType = itemType;
                    slots[i].Count = amount;
                    return true;
                }
            }

            return false;
        }

        public bool HasItem(ItemType itemType, int amount)
        {
            int totalCount = 0;

            for (int i = 0; i < INVENTORY_SIZE; i++)
            {
                if (slots[i].ItemType == itemType && !slots[i].IsEmpty())
                {
                    totalCount += slots[i].Count;
                }
            }

            return totalCount >= amount;
        }

        public bool RemoveItem(ItemType itemType, int amount)
        {
            if (!HasItem(itemType, amount))
                return false;

            int remaining = amount;

            for (int i = 0; i < INVENTORY_SIZE && remaining > 0; i++)
            {
                if (slots[i].ItemType == itemType && !slots[i].IsEmpty())
                {
                    int removeCount = System.Math.Min(slots[i].Count, remaining);
                    slots[i].Count -= removeCount;
                    remaining -= removeCount;

                    if (slots[i].Count <= 0)
                    {
                        slots[i].ItemType = ItemType.None;
                        slots[i].Count = 0;
                    }
                }
            }

            return true;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= INVENTORY_SIZE)
                return null;
            return slots[index];
        }

        public int GetSlotCount()
        {
            return INVENTORY_SIZE;
        }

        // NEW: Swap two inventory slots
        public void SwapSlots(int slot1Index, int slot2Index)
        {
            if (slot1Index < 0 || slot1Index >= INVENTORY_SIZE || 
                slot2Index < 0 || slot2Index >= INVENTORY_SIZE)
            {
                return;
            }

            // Swap the contents
            ItemType tempType = slots[slot1Index].ItemType;
            int tempCount = slots[slot1Index].Count;

            slots[slot1Index].ItemType = slots[slot2Index].ItemType;
            slots[slot1Index].Count = slots[slot2Index].Count;

            slots[slot2Index].ItemType = tempType;
            slots[slot2Index].Count = tempCount;
        }
    }
}