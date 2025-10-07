using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Claude4_5Terraria.Systems
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
        public List<TileChangeData> TileChanges { get; set; }  // NEW: Track tile changes
        public float GameTime { get; set; }
        public string SaveDate { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public int PlayTimeSeconds { get; set; }

        public SaveData()
        {
            InventorySlots = new List<InventorySlotData>();
            TileChanges = new List<TileChangeData>();
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