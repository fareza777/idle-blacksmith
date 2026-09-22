using System;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Daily Ember: once per calendar day the forge grants a streak-scaled gift of gold
    /// and relic ore. Claiming on consecutive days grows the reward; a missed day drops
    /// the streak back to day one.
    /// </summary>
    public class DailyRewardManager : MonoBehaviour
    {
        static int TodayIndex =>
            (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 86400000L);

        public event Action OnChanged;

        static SaveData Saved => GameManager.Instance != null ? GameManager.Instance.Data : null;

        /// <summary>True while today's ember is still unclaimed.</summary>
        public bool ClaimAvailable => Saved != null && Saved.lastDailyClaimDay < TodayIndex;

        public int Streak => Saved != null ? Saved.dailyStreak : 0;

        /// <summary>The streak day the next claim lands on — one unless yesterday was claimed.</summary>
        public int NextDay
        {
            get
            {
                var data = Saved;
                if (data == null) return 1;
                return data.lastDailyClaimDay == TodayIndex - 1
                    ? Mathf.Clamp(data.dailyStreak + 1, 1, 14)
                    : 1;
            }
        }

        /// <summary>Gold and relic ore granted for a streak of <paramref name="day"/> days.</summary>
        public void RewardsFor(int day, out int gold, out int relic)
        {
            var gm = GameManager.Instance;
            int tier = Mathf.Max(1, gm != null ? gm.ShopTier : 1);
            gold = (120 + 80 * (Mathf.Clamp(day, 1, 14) - 1)) * tier;
            relic = 1 + (day >= 5 ? 1 : 0) + (day >= 7 ? 1 : 0);
        }

        /// <summary>Pays the ember into the save and reports what was granted.</summary>
        public bool TryClaim(out int gold, out int relic)
        {
            gold = 0;
            relic = 0;
            var gm = GameManager.Instance;
            var data = Saved;
            if (gm == null || data == null || !ClaimAvailable) return false;

            data.dailyStreak = NextDay;
            data.lastDailyClaimDay = TodayIndex;
            RewardsFor(data.dailyStreak, out gold, out relic);

            gm.economy.AddGold(gold);
            if (relic > 0) gm.AddRelicOre(relic);
            if (data.stats != null) data.stats.dailyClaims++;
            gm.Save();
            OnChanged?.Invoke();
            return true;
        }
    }
}
