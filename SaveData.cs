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
        public Vector2 PlayerPosition { get; set; }
        public List<InventorySlotData> InventorySlots { get; set; }
        public float GameTime { get; set; }
        public string SaveDate { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public int PlayTimeSeconds { get; set; }

        public SaveData()
        {
            InventorySlots = new List<InventorySlotData>();
            SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveName = "Unnamed Save";
        }
    }

    [Serializable]
    public class InventorySlotData
    {
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