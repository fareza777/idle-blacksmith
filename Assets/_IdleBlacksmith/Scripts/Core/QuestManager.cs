using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// The guiding quest chain. Exactly one quest is active at a time, taken in config order,
    /// and it claims itself the moment its goal is met — the player is never asked to go and
    /// collect, they just see the reward land.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public event Action OnChanged;

        /// <summary>(quest, goldReward, oreReward, relicReward, shardReward)</summary>
        public event Action<QuestDef> OnQuestCompleted;

        GameConfig config;
        bool claiming;

        public void Init(GameConfig cfg, SaveData data)
        {
            config = cfg;
            Refresh();
        }

        public QuestDef Active
        {
            get
            {
                if (config == null || config.quests == null) return null;
                GameManager gm = GameManager.Instance;
                List<string> claimed = gm != null && gm.Data != null ? gm.Data.questsClaimed : null;

                foreach (QuestDef q in config.quests)
                {
                    if (q == null) continue;
                    if (claimed != null && claimed.Contains(q.id)) continue;
                    return q;
                }
                return null;
            }
        }

        /// <summary>The quest waiting behind the active one — shown as the road ahead.</summary>
        public QuestDef Next
        {
            get
            {
                if (config == null || config.quests == null) return null;
                GameManager gm = GameManager.Instance;
                List<string> claimed = gm != null && gm.Data != null ? gm.Data.questsClaimed : null;
                bool pastActive = false;

                foreach (QuestDef q in config.quests)
                {
                    if (q == null) continue;
                    if (claimed != null && claimed.Contains(q.id)) continue;
                    if (pastActive) return q;
                    pastActive = true;
                }
                return null;
            }
        }

        public int ClaimedCount
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return gm != null && gm.Data != null && gm.Data.questsClaimed != null
                    ? gm.Data.questsClaimed.Count : 0;
            }
        }

        public int TotalCount => config != null && config.quests != null ? config.quests.Length : 0;

        public long Progress => Active != null ? Goals.Progress(Active.goal, Active.targetId) : 0;

        public bool IsComplete => Active != null && Progress >= Active.target;

        public float Fill01
        {
            get
            {
                QuestDef q = Active;
                if (q == null || q.target <= 0) return 0f;
                return Mathf.Clamp01(Progress / (float)q.target);
            }
        }

        /// <summary>Objective text for the HUD ticker, e.g. "Dig Deeper: Reach Ore Mine level 1  ·  +150g".</summary>
        public string TickerText
        {
            get
            {
                QuestDef q = Active;
                if (q == null) return "All quests complete — the forge is legendary";
                // Count lives on the right-side progress label and the fill bar — the
                // text stays the readable "what + why" so nothing is shown twice.
                string text = $"{q.title}: {Goals.Describe(q.goal, q.targetId, q.target)}";
                string reward = RewardSuffix(q);
                return text + (reward.Length > 0 ? "  ·  " + reward : "");
            }
        }

        /// <summary>"+60g" style compact reward tag for the ticker — answers "why do this?".</summary>
        static string RewardSuffix(QuestDef q)
        {
            var sb = new System.Text.StringBuilder();
            if (q.goldReward > 0) { sb.Append('+').Append(q.goldReward).Append('g'); }
            if (q.oreReward > 0)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append('+').Append(q.oreReward).Append(" ore");
            }
            if (q.relicReward > 0)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append('+').Append(q.relicReward).Append(" relic");
            }
            if (q.shardReward > 0)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append('+').Append(q.shardReward).Append(" shard");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Re-evaluates the active quest and claims it if the goal is met. Safe to call often;
        /// the claim itself is guarded so a reward that changes a stat cannot recurse.
        /// </summary>
        public void Refresh()
        {
            if (claiming) return;

            QuestDef q = Active;
            if (q == null) return;
            if (Goals.Progress(q.goal, q.targetId) < q.target)
            {
                OnChanged?.Invoke();
                return;
            }

            claiming = true;
            try
            {
                GameManager gm = GameManager.Instance;
                if (gm == null || gm.Data == null) return;

                if (gm.Data.questsClaimed == null) gm.Data.questsClaimed = new List<string>();
                gm.Data.questsClaimed.Add(q.id);

                if (q.goldReward > 0) gm.economy.AddGold(q.goldReward);
                if (q.oreReward > 0) gm.resources.Add(q.oreReward);
                if (q.relicReward > 0) gm.AddRelicOre(q.relicReward);
                if (q.shardReward > 0) gm.Data.emberShards += q.shardReward;

                if (gm.Data.stats != null)
                {
                    gm.Data.stats.goldEarned += q.goldReward;
                    gm.Data.runEarned += q.goldReward;
                }

                OnQuestCompleted?.Invoke(q);
                gm.Save();
            }
            finally
            {
                claiming = false;
            }

            OnChanged?.Invoke();
        }
    }
}
