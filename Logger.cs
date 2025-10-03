using System;
using System.IO;

namespace Claude4_5Terraria.Systems
{
    public static class Logger
    {
        private static string logFilePath;
        private static object lockObject = new object();
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            // Create logs in the game's directory
            string logsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // Create logs folder if it doesn't exist
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }

            // Create log file with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logsFolder, $"game_log_{timestamp}.txt");

            // Write initial header
            WriteToFile("=== CLAUDE'S TERRARIA - GAME LOG ===");
            WriteToFile($"Session Started: {DateTime.Now}");
            WriteToFile("");

            isInitialized = true;
        }

        public static void Log(string message)
        {
            if (!isInitialized) Initialize();

            string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";

            // Write to console
            Console.WriteLine(logMessage);

            // Write to file
            WriteToFile(logMessage);
        }

        public static void LogCrafting(string itemName, int count, bool success)
        {
            string status = success ? "SUCCESS" : "FAILED";
            Log($"[CRAFT] {status}: {itemName} x{count}");
        }

        public static void LogInventory(string action, string itemName, int count)
        {
            Log($"[INVENTORY] {action}: {itemName} x{count}");
        }

        public static void LogWorld(string message)
        {
            Log($"[WORLD] {message}");
        }

        public static void LogError(string message)
        {
            Log($"[ERROR] {message}");
        }

        private static void WriteToFile(string message)
        {
            lock (lockObject)
            {
                try
                {
                    File.AppendAllText(logFilePath, message + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        public static void Close()
        {
            if (!isInitialized) return;

            WriteToFile("");
            WriteToFile($"Session Ended: {DateTime.Now}");
            WriteToFile("=== END OF LOG ===");
        }
    }
}