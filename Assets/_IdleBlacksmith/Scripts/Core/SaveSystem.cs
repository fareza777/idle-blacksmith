using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>JSON save/load in persistentDataPath. Atomic-ish write via temp file.</summary>
    public static class SaveSystem
    {
        public const int CurrentVersion = 3;

        static string SavePath => Path.Combine(Application.persistentDataPath, "idle_blacksmith_save.json");

        public static string PathForLog => SavePath;

        public static SaveData Load()
        {
            SaveData data = null;
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    data = JsonUtility.FromJson<SaveData>(json);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Load failed, starting fresh: {e.Message}");
            }

            if (data == null) data = new SaveData();
            Migrate(data);
            data.Normalize();
            return data;
        }

        public static void Save(SaveData data)
        {
            try
            {
                data.version = CurrentVersion;
                data.everSaved = true;
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

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Delete failed: {e.Message}");
            }
        }

        /// <summary>
        /// Brings an older save up to the current schema. v1/v2 stored named upgrade fields,
        /// a bare sword count and a single expedition; v3 stores them as lists keyed by id.
        /// </summary>
        static void Migrate(SaveData d)
        {
            if (d.version >= CurrentVersion) return;

            if (d.upgradeList == null || d.upgradeList.Count == 0)
            {
                d.upgradeList = new List<UpgradeState>
                {
                    new UpgradeState { id = UpgradeId.Craft, level = Mathf.Max(0, d.craftLevel) },
                    new UpgradeState { id = UpgradeId.Carry, level = Mathf.Max(0, d.carryLevel) },
                    new UpgradeState { id = UpgradeId.Rack, level = Mathf.Max(0, d.rackLevel) },
                };
            }

            if (d.buildings == null || d.buildings.Count == 0)
            {
                d.buildings = new List<BuildingState>();
                foreach (string id in BuildingId.All)
                {
                    int level = id == BuildingId.Smithy
                        ? Mathf.Clamp(d.shopTier < 1 ? 1 : d.shopTier, 1, 5)
                        : 0;
                    d.buildings.Add(new BuildingState { id = id, level = level });
                }
            }

            if (d.rackItems == null || d.rackItems.Count == 0)
            {
                d.rackItems = new List<SwordItem>();
                for (int i = 0; i < d.rackStoredSwords; i++)
                    d.rackItems.Add(new SwordItem(RecipeId.Copper, Rarity.Common));
            }

            if (d.expeditions == null || d.expeditions.Count == 0)
            {
                d.expeditions = new List<ExpeditionState>();
                if (!string.IsNullOrEmpty(d.expeditionId) && d.expeditionEndUtcMs > 0)
                    d.expeditions.Add(new ExpeditionState { id = d.expeditionId, endUtcMs = d.expeditionEndUtcMs });
            }

            // A migrated save has already been played, so offline progress applies to it.
            if (d.ore <= 0) d.ore = 25;
            d.everSaved = true;
            d.version = CurrentVersion;
            Debug.Log($"[SaveSystem] migrated save to v{CurrentVersion}");
        }
    }
}
