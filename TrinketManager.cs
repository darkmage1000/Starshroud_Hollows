using StarshroudHollows.Enums;
using System;
using System.Collections.Generic;

namespace StarshroudHollows.Systems
{
    public enum TrinketTier
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public enum TrinketType
    {
        None,
        // Generic Trinkets
        Armor_Trinket,          // 15% bonus armor
        Double_Jump_Trinket,    // Allows double jump
        Lava_Shoe_Trinket,      // Walk on lava, immune to lava damage
        Pickaxe_Trinket,        // 15% mining speed
        
        // Biome-Specific Trinkets (for future expansion)
        Forest_Trinket,
        Snow_Trinket,
        Desert_Trinket,
        Jungle_Trinket,
        Swamp_Trinket,
        Volcanic_Trinket
    }

    public class TrinketSlot
    {
        public TrinketType TrinketType { get; set; }
        
        public TrinketSlot()
        {
            TrinketType = TrinketType.None;
        }
        
        public bool IsEmpty()
        {
            return TrinketType == TrinketType.None;
        }
    }

    public class TrinketManager
    {
        private const int MAX_TRINKET_SLOTS = 3;
        private TrinketSlot[] trinketSlots;
        
        // Player reference needed for double jump tracking
        private bool hasUsedDoubleJump = false;
        private bool wasOnGroundLastFrame = false;

        public TrinketManager()
        {
            trinketSlots = new TrinketSlot[MAX_TRINKET_SLOTS];
            for (int i = 0; i < MAX_TRINKET_SLOTS; i++)
            {
                trinketSlots[i] = new TrinketSlot();
            }
        }

        public TrinketSlot GetSlot(int index)
        {
            if (index >= 0 && index < MAX_TRINKET_SLOTS)
                return trinketSlots[index];
            return null;
        }

        public int GetSlotCount() => MAX_TRINKET_SLOTS;

        public bool HasTrinket(TrinketType type)
        {
            for (int i = 0; i < MAX_TRINKET_SLOTS; i++)
            {
                if (trinketSlots[i].TrinketType == type)
                    return true;
            }
            return false;
        }

        public bool TryEquipTrinket(TrinketType trinketType)
        {
            // Check if already equipped
            for (int i = 0; i < MAX_TRINKET_SLOTS; i++)
            {
                if (trinketSlots[i].TrinketType == trinketType)
                {
                    Logger.Log($"[TRINKET] {trinketType} is already equipped");
                    return false;
                }
            }

            // Find empty slot
            for (int i = 0; i < MAX_TRINKET_SLOTS; i++)
            {
                if (trinketSlots[i].IsEmpty())
                {
                    trinketSlots[i].TrinketType = trinketType;
                    Logger.Log($"[TRINKET] Equipped {trinketType} in slot {i}");
                    return true;
                }
            }

            Logger.Log($"[TRINKET] No empty slots available");
            return false;
        }

        public bool TryUnequipTrinket(int slotIndex, Inventory inventory)
        {
            if (slotIndex < 0 || slotIndex >= MAX_TRINKET_SLOTS)
                return false;

            TrinketSlot slot = trinketSlots[slotIndex];
            if (slot.IsEmpty())
                return false;

            // Convert trinket back to item and add to inventory
            ItemType itemType = TrinketTypeToItemType(slot.TrinketType);
            if (inventory.AddItem(itemType, 1))
            {
                Logger.Log($"[TRINKET] Unequipped {slot.TrinketType} from slot {slotIndex}");
                slot.TrinketType = TrinketType.None;
                return true;
            }

            return false;
        }

        // === TRINKET EFFECTS ===

        public float GetArmorBonus()
        {
            return HasTrinket(TrinketType.Armor_Trinket) ? 0.15f : 0f;
        }

        public float GetMiningSpeedBonus()
        {
            return HasTrinket(TrinketType.Pickaxe_Trinket) ? 0.15f : 0f;
        }

        public bool HasLavaImmunity()
        {
            return HasTrinket(TrinketType.Lava_Shoe_Trinket);
        }

        public bool CanDoubleJump()
        {
            return HasTrinket(TrinketType.Double_Jump_Trinket);
        }

        // Call this from Player.Update()
        public void UpdateDoubleJump(bool isOnGround)
        {
            if (isOnGround)
            {
                hasUsedDoubleJump = false;
            }
            wasOnGroundLastFrame = isOnGround;
        }

        // Call this when jump is pressed
        public bool TryUseDoubleJump(bool isOnGround)
        {
            if (!CanDoubleJump())
                return false;

            // If on ground, allow normal jump
            if (isOnGround)
                return true;

            // If in air and haven't used double jump yet
            if (!hasUsedDoubleJump && !wasOnGroundLastFrame)
            {
                hasUsedDoubleJump = true;
                Logger.Log("[TRINKET] Double jump activated!");
                return true;
            }

            return false;
        }

        // === TRINKET CONVERSION ===

        public static TrinketType ItemTypeToTrinketType(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Armor_Trinket: return TrinketType.Armor_Trinket;
                case ItemType.Double_Jump_Trinket: return TrinketType.Double_Jump_Trinket;
                case ItemType.Lava_Shoe_Trinket: return TrinketType.Lava_Shoe_Trinket;
                case ItemType.Pickaxe_Trinket: return TrinketType.Pickaxe_Trinket;
                default: return TrinketType.None;
            }
        }

        public static ItemType TrinketTypeToItemType(TrinketType trinketType)
        {
            switch (trinketType)
            {
                case TrinketType.Armor_Trinket: return ItemType.Armor_Trinket;
                case TrinketType.Double_Jump_Trinket: return ItemType.Double_Jump_Trinket;
                case TrinketType.Lava_Shoe_Trinket: return ItemType.Lava_Shoe_Trinket;
                case TrinketType.Pickaxe_Trinket: return ItemType.Pickaxe_Trinket;
                default: return ItemType.None;
            }
        }

        public static bool IsTrinketItem(ItemType itemType)
        {
            return itemType == ItemType.Armor_Trinket ||
                   itemType == ItemType.Double_Jump_Trinket ||
                   itemType == ItemType.Lava_Shoe_Trinket ||
                   itemType == ItemType.Pickaxe_Trinket;
        }

        public static string GetTrinketName(TrinketType type)
        {
            switch (type)
            {
                case TrinketType.Armor_Trinket: return "Armor Trinket";
                case TrinketType.Double_Jump_Trinket: return "Double Jump Trinket";
                case TrinketType.Lava_Shoe_Trinket: return "Lava Shoes";
                case TrinketType.Pickaxe_Trinket: return "Pickaxe Trinket";
                default: return "Unknown Trinket";
            }
        }

        public static string GetTrinketDescription(TrinketType type)
        {
            switch (type)
            {
                case TrinketType.Armor_Trinket: return "+15% Defense";
                case TrinketType.Double_Jump_Trinket: return "Allows double jumping";
                case TrinketType.Lava_Shoe_Trinket: return "Walk on lava, immunity to lava damage";
                case TrinketType.Pickaxe_Trinket: return "+15% Mining Speed";
                default: return "";
            }
        }

        public static TrinketTier GetTrinketTier(TrinketType type)
        {
            switch (type)
            {
                case TrinketType.Armor_Trinket:
                case TrinketType.Pickaxe_Trinket:
                    return TrinketTier.Common;
                case TrinketType.Double_Jump_Trinket:
                    return TrinketTier.Uncommon;
                case TrinketType.Lava_Shoe_Trinket:
                    return TrinketTier.Rare;
                default:
                    return TrinketTier.Common;
            }
        }

        // === SAVE/LOAD ===

        public List<TrinketSlotData> GetSaveData()
        {
            List<TrinketSlotData> data = new List<TrinketSlotData>();
            for (int i = 0; i < MAX_TRINKET_SLOTS; i++)
            {
                if (!trinketSlots[i].IsEmpty())
                {
                    data.Add(new TrinketSlotData
                    {
                        SlotIndex = i,
                        TrinketType = (int)trinketSlots[i].TrinketType
                    });
                }
            }
            return data;
        }

        public void LoadFromData(List<TrinketSlotData> data)
        {
            // Clear all slots
            for (int i = 0; i < MAX_TRINKET_SLOTS; i++)
            {
                trinketSlots[i].TrinketType = TrinketType.None;
            }

            // Load saved trinkets
            if (data != null)
            {
                foreach (var trinket in data)
                {
                    if (trinket.SlotIndex >= 0 && trinket.SlotIndex < MAX_TRINKET_SLOTS)
                    {
                        trinketSlots[trinket.SlotIndex].TrinketType = (TrinketType)trinket.TrinketType;
                    }
                }
            }
        }
    }

    [Serializable]
    public class TrinketSlotData
    {
        public int SlotIndex { get; set; }
        public int TrinketType { get; set; }
    }
}
