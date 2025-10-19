using StarshroudHollows.Enums;
using System.Collections.Generic;

namespace StarshroudHollows.Systems
{
    public class ArmorSlot
    {
        public ItemType ItemType { get; set; }
        public ArmorSlot()
        {
            ItemType = ItemType.None;
        }

        public bool IsEmpty() => ItemType == ItemType.None;
    }

    public enum ArmorType
    {
        Helmet,
        Chestplate,
        Leggings
    }

    public struct ArmorStats
    {
        public int Defense { get; set; }
        public float SpeedBonus { get; set; }  // Percentage (0.1f = 10% faster)
        public float MiningSpeedBonus { get; set; }

        public ArmorStats(int defense, float speedBonus = 0f, float miningSpeedBonus = 0f)
        {
            Defense = defense;
            SpeedBonus = speedBonus;
            MiningSpeedBonus = miningSpeedBonus;
        }
    }

    public struct SetBonus
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int BonusDefense { get; set; }
        public float BonusDamage { get; set; }  // Percentage (0.1f = 10% more damage)

        public SetBonus(string name, string description, int bonusDefense = 0, float bonusDamage = 0f)
        {
            Name = name;
            Description = description;
            BonusDefense = bonusDefense;
            BonusDamage = bonusDamage;
        }
    }

    public class ArmorSystem
    {
        private ArmorSlot helmetSlot;
        private ArmorSlot chestplateSlot;
        private ArmorSlot leggingsSlot;

        private Inventory inventory;

        // Armor stats database
        private static readonly Dictionary<ItemType, ArmorStats> armorStatsTable = new Dictionary<ItemType, ArmorStats>
        {
            // Wood Armor (Tier 1)
            { ItemType.WoodHelmet, new ArmorStats(1) },
            { ItemType.WoodChestplate, new ArmorStats(2) },
            { ItemType.WoodLeggings, new ArmorStats(1) },

            // Copper Armor (Tier 2)
            { ItemType.CopperHelmet, new ArmorStats(2) },
            { ItemType.CopperChestplate, new ArmorStats(3) },
            { ItemType.CopperLeggings, new ArmorStats(2) },

            // Iron Armor (Tier 3)
            { ItemType.IronHelmet, new ArmorStats(3) },
            { ItemType.IronChestplate, new ArmorStats(5) },
            { ItemType.IronLeggings, new ArmorStats(3) },

            // Silver Armor (Tier 4)
            { ItemType.SilverHelmet, new ArmorStats(4, 0.05f) },
            { ItemType.SilverChestplate, new ArmorStats(6, 0.05f) },
            { ItemType.SilverLeggings, new ArmorStats(4, 0.05f) },

            // Gold Armor (Tier 5)
            { ItemType.GoldHelmet, new ArmorStats(3, 0.1f, 0.1f) },
            { ItemType.GoldChestplate, new ArmorStats(5, 0.1f, 0.1f) },
            { ItemType.GoldLeggings, new ArmorStats(3, 0.1f, 0.1f) },

            // Platinum Armor (Tier 6 - Best)
            { ItemType.PlatinumHelmet, new ArmorStats(5, 0.08f) },
            { ItemType.PlatinumChestplate, new ArmorStats(8, 0.08f) },
            { ItemType.PlatinumLeggings, new ArmorStats(5, 0.08f) }
        };

        // Set bonuses database
        private static readonly Dictionary<string, SetBonus> setBonusTable = new Dictionary<string, SetBonus>
        {
            { "Wood", new SetBonus("Wood Set", "+1 Defense", 1, 0f) },
            { "Copper", new SetBonus("Copper Set", "+2 Defense", 2, 0f) },
            { "Iron", new SetBonus("Iron Guard", "+3 Defense, +5% Damage", 3, 0.05f) },
            { "Silver", new SetBonus("Swift Silver", "+10% Speed, +5% Damage", 0, 0.05f) },
            { "Gold", new SetBonus("Golden Fortune", "+15% Mining Speed, +8% Damage", 0, 0.08f) },
            { "Platinum", new SetBonus("Platinum Aegis", "+5 Defense, +10% Damage", 5, 0.10f) }
        };

        public ArmorSystem(Inventory playerInventory)
        {
            helmetSlot = new ArmorSlot();
            chestplateSlot = new ArmorSlot();
            leggingsSlot = new ArmorSlot();
            inventory = playerInventory;
        }

        public ArmorSlot GetHelmetSlot() => helmetSlot;
        public ArmorSlot GetChestplateSlot() => chestplateSlot;
        public ArmorSlot GetLeggingsSlot() => leggingsSlot;

        public ArmorSlot GetSlotByType(ArmorType type)
        {
            switch (type)
            {
                case ArmorType.Helmet: return helmetSlot;
                case ArmorType.Chestplate: return chestplateSlot;
                case ArmorType.Leggings: return leggingsSlot;
                default: return helmetSlot;
            }
        }

        public bool TryEquipArmor(ItemType armorItem)
        {
            if (!IsArmorItem(armorItem))
            {
                Logger.Log($"[ARMOR] {armorItem} is not an armor item!");
                return false;
            }

            // Check if player has the item
            if (!inventory.HasItem(armorItem, 1))
            {
                Logger.Log($"[ARMOR] Player doesn't have {armorItem} to equip!");
                return false;
            }

            ArmorType armorType = GetArmorType(armorItem);
            ArmorSlot targetSlot = GetSlotByType(armorType);

            // If slot already has armor, unequip it first
            if (!targetSlot.IsEmpty())
            {
                UnequipArmor(armorType);
            }

            // Remove from inventory and equip
            inventory.RemoveItem(armorItem, 1);
            targetSlot.ItemType = armorItem;

            Logger.Log($"[ARMOR] Equipped {armorItem} to {armorType} slot");
            return true;
        }

        public bool UnequipArmor(ArmorType armorType)
        {
            ArmorSlot targetSlot = GetSlotByType(armorType);

            if (targetSlot.IsEmpty())
            {
                Logger.Log($"[ARMOR] No armor equipped in {armorType} slot!");
                return false;
            }

            // Add back to inventory
            ItemType unequippedItem = targetSlot.ItemType;
            if (!inventory.CanAddItem(unequippedItem, 1))
            {
                Logger.Log($"[ARMOR] Inventory full! Cannot unequip {unequippedItem}");
                return false;
            }

            inventory.AddItem(unequippedItem, 1);
            targetSlot.ItemType = ItemType.None;

            Logger.Log($"[ARMOR] Unequipped {unequippedItem} from {armorType} slot");
            return true;
        }

        public int GetTotalDefense()
        {
            int defense = 0;

            if (!helmetSlot.IsEmpty() && armorStatsTable.ContainsKey(helmetSlot.ItemType))
                defense += armorStatsTable[helmetSlot.ItemType].Defense;

            if (!chestplateSlot.IsEmpty() && armorStatsTable.ContainsKey(chestplateSlot.ItemType))
                defense += armorStatsTable[chestplateSlot.ItemType].Defense;

            if (!leggingsSlot.IsEmpty() && armorStatsTable.ContainsKey(leggingsSlot.ItemType))
                defense += armorStatsTable[leggingsSlot.ItemType].Defense;

            // Add set bonus defense
            SetBonus? setBonus = GetCurrentSetBonus();
            if (setBonus.HasValue)
                defense += setBonus.Value.BonusDefense;

            return defense;
        }

        public float GetTotalSpeedBonus()
        {
            float bonus = 0f;

            if (!helmetSlot.IsEmpty() && armorStatsTable.ContainsKey(helmetSlot.ItemType))
                bonus += armorStatsTable[helmetSlot.ItemType].SpeedBonus;

            if (!chestplateSlot.IsEmpty() && armorStatsTable.ContainsKey(chestplateSlot.ItemType))
                bonus += armorStatsTable[chestplateSlot.ItemType].SpeedBonus;

            if (!leggingsSlot.IsEmpty() && armorStatsTable.ContainsKey(leggingsSlot.ItemType))
                bonus += armorStatsTable[leggingsSlot.ItemType].SpeedBonus;

            return bonus;
        }

        public float GetTotalMiningSpeedBonus()
        {
            float bonus = 0f;

            if (!helmetSlot.IsEmpty() && armorStatsTable.ContainsKey(helmetSlot.ItemType))
                bonus += armorStatsTable[helmetSlot.ItemType].MiningSpeedBonus;

            if (!chestplateSlot.IsEmpty() && armorStatsTable.ContainsKey(chestplateSlot.ItemType))
                bonus += armorStatsTable[chestplateSlot.ItemType].MiningSpeedBonus;

            if (!leggingsSlot.IsEmpty() && armorStatsTable.ContainsKey(leggingsSlot.ItemType))
                bonus += armorStatsTable[leggingsSlot.ItemType].MiningSpeedBonus;

            return bonus;
        }

        public float GetTotalDamageBonus()
        {
            SetBonus? setBonus = GetCurrentSetBonus();
            return setBonus.HasValue ? setBonus.Value.BonusDamage : 0f;
        }

        public SetBonus? GetCurrentSetBonus()
        {
            // Check if wearing a full set
            if (helmetSlot.IsEmpty() || chestplateSlot.IsEmpty() || leggingsSlot.IsEmpty())
                return null;

            string helmetSet = GetArmorSetName(helmetSlot.ItemType);
            string chestSet = GetArmorSetName(chestplateSlot.ItemType);
            string legsSet = GetArmorSetName(leggingsSlot.ItemType);

            // All three must be from the same set
            if (helmetSet == chestSet && chestSet == legsSet && setBonusTable.ContainsKey(helmetSet))
            {
                return setBonusTable[helmetSet];
            }

            return null;
        }

        private string GetArmorSetName(ItemType armorItem)
        {
            string itemName = armorItem.ToString();
            if (itemName.Contains("Wood")) return "Wood";
            if (itemName.Contains("Copper")) return "Copper";
            if (itemName.Contains("Iron")) return "Iron";
            if (itemName.Contains("Silver")) return "Silver";
            if (itemName.Contains("Gold")) return "Gold";
            if (itemName.Contains("Platinum")) return "Platinum";
            return "";
        }

        public static bool IsArmorItem(ItemType itemType)
        {
            return armorStatsTable.ContainsKey(itemType);
        }

        public static ArmorType GetArmorType(ItemType itemType)
        {
            string itemName = itemType.ToString();
            if (itemName.Contains("Helmet")) return ArmorType.Helmet;
            if (itemName.Contains("Chestplate")) return ArmorType.Chestplate;
            if (itemName.Contains("Leggings")) return ArmorType.Leggings;
            return ArmorType.Helmet;
        }

        public static ArmorStats GetArmorStats(ItemType armorItem)
        {
            if (armorStatsTable.ContainsKey(armorItem))
                return armorStatsTable[armorItem];
            return new ArmorStats(0);
        }

        public static string GetArmorStatDescription(ItemType armorItem)
        {
            if (!armorStatsTable.ContainsKey(armorItem))
                return "";

            ArmorStats stats = armorStatsTable[armorItem];
            string desc = $"{stats.Defense} Defense";

            if (stats.SpeedBonus > 0)
                desc += $"\n+{(stats.SpeedBonus * 100):F0}% Speed";

            if (stats.MiningSpeedBonus > 0)
                desc += $"\n+{(stats.MiningSpeedBonus * 100):F0}% Mining Speed";

            return desc;
        }
    }
}
