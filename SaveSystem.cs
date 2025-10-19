using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace StarshroudHollows.Systems
{
    public static class SaveSystem
    {
        private static readonly string SaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StarshroudHollows",
            "Saves"
        );

        private const int MAX_SAVE_SLOTS = 3;

        static SaveSystem()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }
        }

        private static string GetSaveFilePath(int slotIndex)
        {
            return Path.Combine(SaveDirectory, $"save_slot_{slotIndex}.json");
        }

        public static bool SaveExists(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
                return false;

            string savePath = GetSaveFilePath(slotIndex);
            return File.Exists(savePath);
        }

        public static bool AnySaveExists()
        {
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                if (SaveExists(i))
                    return true;
            }
            return false;
        }

        // This method is called by UI to check slot status - DON'T log here!
        public static SaveSlotInfo GetSaveSlotInfo(int slotIndex)
        {
            if (!SaveExists(slotIndex))
            {
                return new SaveSlotInfo
                {
                    HasSave = false,
                    SaveName = $"Slot {slotIndex + 1}: Empty"
                };
            }

            try
            {
                string savePath = GetSaveFilePath(slotIndex);
                string json = File.ReadAllText(savePath);
                SaveData data = JsonSerializer.Deserialize<SaveData>(json);

                return new SaveSlotInfo
                {
                    HasSave = true,
                    SaveName = data.SaveName,
                    SaveDate = data.SaveDate,
                    PlayTimeSeconds = data.PlayTimeSeconds
                };
            }
            catch
            {
                return new SaveSlotInfo
                {
                    HasSave = false,
                    SaveName = $"Slot {slotIndex + 1}: Corrupted"
                };
            }
        }

        public static void SaveGame(SaveData data, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
            {
                Logger.Log($"[SAVE] Invalid slot index: {slotIndex}");
                return;
            }

            try
            {
                string savePath = GetSaveFilePath(slotIndex);
                
                Logger.Log($"[SAVE] Serializing data to JSON...");
                Logger.Log($"[SAVE] Save Name: {data.SaveName}");
                Logger.Log($"[SAVE] Player Position: ({data.PlayerPositionX}, {data.PlayerPositionY})");
                Logger.Log($"[SAVE] Inventory Slots: {data.InventorySlots.Count}");
                Logger.Log($"[SAVE] Chests: {data.Chests.Count}");
                
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true  // Make it readable for debugging
                });
                
                Logger.Log($"[SAVE] JSON size: {json.Length} bytes");
                Logger.Log($"[SAVE] Writing to file: {savePath}");
                
                // Write directly without compression for debugging
                File.WriteAllText(savePath, json);
                
                FileInfo fileInfo = new FileInfo(savePath);
                Logger.Log($"[SAVE] File size: {fileInfo.Length} bytes");
                Logger.Log($"[SAVE] ✅ Game saved successfully to slot {slotIndex + 1}");
            }
            catch (Exception ex)
            {
                Logger.Log($"[SAVE] ❌ ERROR saving game: {ex.Message}");
                Logger.Log($"[SAVE] Stack trace: {ex.StackTrace}");
            }
        }

        // This method is for ACTUAL loading attempts - log only here
        public static SaveData LoadGame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
            {
                Logger.Log($"[SAVE] Invalid slot index: {slotIndex}");
                return null;
            }

            try
            {
                string savePath = GetSaveFilePath(slotIndex);

                if (!File.Exists(savePath))
                {
                    Logger.Log($"[SAVE] No save file found in slot {slotIndex + 1}");
                    return null;
                }

                Logger.Log($"[SAVE] Reading save file from: {savePath}");
                
                // Read directly without decompression
                string json = File.ReadAllText(savePath);
                
                Logger.Log($"[SAVE] Deserializing JSON...");
                SaveData data = JsonSerializer.Deserialize<SaveData>(json);

                Logger.Log($"[SAVE] ✅ Game loaded from slot {slotIndex + 1}");
                Logger.Log($"[SAVE] Save Name: {data.SaveName}");
                Logger.Log($"[SAVE] Player Position: ({data.PlayerPositionX}, {data.PlayerPositionY})");
                return data;
            }
            catch (Exception ex)
            {
                Logger.Log($"[SAVE] ❌ ERROR loading game: {ex.Message}");
                Logger.Log($"[SAVE] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public static void DeleteSave(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
                return;

            try
            {
                string savePath = GetSaveFilePath(slotIndex);
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Logger.Log($"[SAVE] Save slot {slotIndex + 1} deleted");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[SAVE] Error deleting save: {ex.Message}");
            }
        }
    }
}