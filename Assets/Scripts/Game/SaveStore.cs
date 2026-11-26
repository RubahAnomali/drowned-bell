using System;
using System.IO;
using UnityEngine;
using DrownedBell.Core;

namespace DrownedBell.Game
{
    /// <summary>
    /// The one save slot on disk (in the browser, Unity keeps persistentDataPath in IndexedDB).
    /// A write goes to a temporary file first and is then moved over the old save, so a crash mid-write leaves
    /// either the old save or the new one. Loading falls back to the temporary file if the main one is missing or damaged.
    /// QA runs (autoplay, screenshot tour) switch saving off so they never touch the player's progress.
    /// </summary>
    public static class SaveStore
    {
        public static bool Enabled = true;

        static string Main => Path.Combine(Application.persistentDataPath, "drowned-bell-save.txt");
        static string Temp => Main + ".tmp";

        public static SaveData Load()
        {
            if (!Enabled) return null;
            return Read(Main) ?? Read(Temp);
        }

        static SaveData Read(string path)
        {
            try { return File.Exists(path) ? SaveData.Parse(File.ReadAllText(path)) : null; }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return null; }
        }

        public static void Write(SaveData data)
        {
            if (!Enabled || data == null) return;
            try
            {
                File.WriteAllText(Temp, data.Serialize());
                if (File.Exists(Main)) File.Delete(Main);
                File.Move(Temp, Main);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Drowned Bell: could not save progress: " + e.Message);
            }
        }

        public static void Clear()
        {
            if (!Enabled) return;
            try
            {
                if (File.Exists(Main)) File.Delete(Main);
                if (File.Exists(Temp)) File.Delete(Temp);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning("Drowned Bell: could not clear the save: " + e.Message);
            }
        }
    }
}
