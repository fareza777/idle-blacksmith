using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Pays out what the complex produced while the app was closed. The mine keeps digging
    /// and the Trading Post keeps selling; expeditions resolve on their own wall-clock timers.
    /// </summary>
    public static class OfflineProgress
    {
        public struct Report
        {
            public bool valid;
            public long elapsedSeconds;
            public int oreGained;
            public int goldGained;
            public int expeditionsReady;
        }

        /// <summary>Runs are ignored below this, so a quick app switch never pops a modal.</summary>
        const long MinimumElapsedMs = 60_000;

        /// <summary>Computes and applies the payout. Call once, right after the save is loaded.</summary>
        public static Report Apply()
        {
            var report = new Report();
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null || gm.config == null) return report;
            if (!gm.Data.everSaved || gm.Data.lastSaveUtcMs <= 0) return report;

            long elapsedMs = ExpeditionManager.NowMs() - gm.Data.lastSaveUtcMs;
            if (elapsedMs < MinimumElapsedMs) return report;

            float capSeconds = gm.config.offlineCapHours * 3600f;
            float seconds = Mathf.Min(elapsedMs / 1000f, capSeconds);
            float rate = Mathf.Clamp01(Production.OfflineRate);

            int storehouse = gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Storehouse) : 0;
            var storeDef = gm.config.GetBuilding(BuildingId.Storehouse);
            float stored = 1f + storehouse * (storeDef != null ? storeDef.offlineBonus : 0f);

            // The mine keeps producing; the shop keeps selling; the storehouse keeps the take safe.
            float oreRaw = gm.resources.OrePerSecond * seconds * rate * stored;
            int ore = Mathf.FloorToInt(oreRaw);

            int smithy = gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Smithy) : 0;
            int market = gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Market) : 0;
            float goldRaw = (smithy + market) * gm.config.offlineGoldPerBuildingLevel * seconds * rate * stored;
            int gold = Mathf.FloorToInt(goldRaw);

            if (ore > 0) gm.resources.GrantOffline(seconds);
            if (gold > 0)
            {
                gm.economy.AddGold(gold);
                if (gm.Data.stats != null)
                {
                    gm.Data.stats.goldEarned += gold;
                    gm.Data.runEarned += gold;
                }
            }

            report.valid = ore > 0 || gold > 0 || gm.expeditions.ReadyCount > 0;
            report.elapsedSeconds = (long)seconds;
            report.oreGained = Mathf.Min(ore, gm.resources.Ore);
            report.goldGained = gold;
            report.expeditionsReady = gm.expeditions.ReadyCount;
            return report;
        }

        /// <summary>"3h 12m" / "8m" / "45s" for the welcome-back modal.</summary>
        public static string FormatElapsed(long seconds)
        {
            if (seconds >= 3600) return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
            if (seconds >= 60) return $"{seconds / 60}m";
            return seconds + "s";
        }
    }
}
