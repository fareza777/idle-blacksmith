using System.IO;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>JSON save/load in persistentDataPath. Atomic-ish write via temp file.</summary>
    public static class SaveSystem
    {
        static string SavePath => Path.Combine(Application.persistentDataPath, "idle_blacksmith_save.json");

        public static SaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null) return data;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Load failed, starting fresh: {e.Message}");
            }
            return new SaveData();
        }

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string tmp = SavePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tmp, SavePath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Save failed: {e.Message}");
            }
        }
    }
}
