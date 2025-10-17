using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StarshroudHollows.Systems
{
    [Serializable]
    public class SaveData
    {
        public string SaveName { get; set; }
        public int WorldSeed { get; set; }

        public float PlayerPositionX { get; set; }
        public float PlayerPositionY { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Vector2 PlayerPosition
        {
            get => new Vector2(PlayerPositionX, PlayerPositionY);
            set
            {
                PlayerPositionX = value.X;
                PlayerPositionY = value.Y;
            }
        }

        public List<InventorySlotData> InventorySlots { get; set; }
        public List<TileChangeData> TileChanges { get; set; }
        public List<ChestData> Chests { get; set; }  // NEW: Store chest data
        public float GameTime { get; set; }
        public string SaveDate { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public int PlayTimeSeconds { get; set; }

        public SaveData()
        {
            InventorySlots = new List<InventorySlotData>();
            TileChanges = new List<TileChangeData>();
            Chests = new List<ChestData>();  // Initialize chest list
            SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveName = "Unnamed Save";
            PlayerPositionX = 0;
            PlayerPositionY = 0;
        }
    }

    [Serializable]
    public class InventorySlotData
    {
        public int ItemType { get; set; }
        public int Count { get; set; }
    }

    [Serializable]
    public class TileChangeData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int TileType { get; set; }
        public bool IsActive { get; set; }
        public float LiquidVolume { get; set; } // NEW: Save liquid volume for proper flow restoration
        public int WallType { get; set; } // NEW: Save wall type for background walls
    }

    [Serializable]
    public class ChestData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Tier { get; set; }  // 0 = Wood, 1 = Silver, 2 = Magic
        public bool IsNaturallyGenerated { get; set; }
        public List<ChestItemData> Items { get; set; }
        // FIX: ADDED MISSING PROPERTY
        public string Name { get; set; } // - Added to fix CS0117/CS1061

        public ChestData()
        {
            Items = new List<ChestItemData>();
        }
    }

    [Serializable]
    public class ChestItemData
    {
        public int SlotIndex { get; set; }
        public int ItemType { get; set; }
        public int Count { get; set; }
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public bool HasSave { get; set; }
        public string SaveName { get; set; }
        public string SaveDate { get; set; }
        public int PlayTimeSeconds { get; set; }

        public SaveSlotInfo()
        {
            HasSave = false;
            SaveName = "Empty Slot";
            SaveDate = "";
            PlayTimeSeconds = 0;
        }
    }
}