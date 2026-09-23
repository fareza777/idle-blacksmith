using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Turns a QuestGoal into a number the UI and the trackers can compare against a target.
    /// Every goal reads a monotonic lifetime stat or a current value, so progress needs no
    /// per-quest baseline bookkeeping.
    /// </summary>
    public static class Goals
    {
        public static long Progress(QuestGoal goal, string targetId)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null) return 0;
            StatBlock st = gm.Data.stats;

            switch (goal)
            {
                case QuestGoal.ForgeSwords: return st != null ? st.swordsForged : 0;
                case QuestGoal.SellSwords: return st != null ? st.swordsSold : 0;
                case QuestGoal.EarnGoldRun: return gm.Data.runEarned;
                case QuestGoal.ReachGold: return gm.economy != null ? gm.economy.Gold : 0;
                case QuestGoal.ReachOre: return gm.resources != null ? gm.resources.Ore : 0;
                case QuestGoal.UpgradeBuilding: return gm.buildings != null ? gm.buildings.GetLevel(targetId) : 0;
                case QuestGoal.UnlockRecipe: return gm.recipes != null ? gm.recipes.AvailableCount : 0;
                case QuestGoal.ClaimExpedition: return st != null ? st.expeditionsClaimed : 0;
                case QuestGoal.HireHelper: return gm.HelperUnlocked ? 1 : 0;
                case QuestGoal.Prestige: return st != null ? st.prestiges : 0;
                case QuestGoal.BuildRunes: return gm.runes != null ? gm.runes.TotalLevels : 0;
                case QuestGoal.OwnRarity: return st != null ? st.bestRarity : 0;
                case QuestGoal.PlayMinutes: return st != null ? (long)(st.playSeconds / 60f) : 0;
                case QuestGoal.ServeOrders: return st != null ? st.ordersServed : 0;
                case QuestGoal.RushOrders: return st != null ? st.rushOrdersDone : 0;
                case QuestGoal.ClaimDailies: return st != null ? st.dailyClaims : 0;
                case QuestGoal.UseTools: return st != null ? st.toolUses : 0;
                case QuestGoal.PetCat: return st != null ? st.catPets : 0;
            }
            return 0;
        }

        /// <summary>Short objective text, e.g. "Forge 25 swords" or "Own a Rare sword".</summary>
        public static string Describe(QuestGoal goal, string targetId, int target)
        {
            switch (goal)
            {
                case QuestGoal.ForgeSwords: return $"Forge {target} swords";
                case QuestGoal.SellSwords: return $"Sell {target} swords";
                case QuestGoal.EarnGoldRun: return $"Earn {target} gold this run";
                case QuestGoal.ReachGold: return $"Hold {target} gold";
                case QuestGoal.ReachOre: return $"Stockpile {target} ore";
                case QuestGoal.UpgradeBuilding: return $"Reach {BuildingName(targetId)} level {target}";
                case QuestGoal.UnlockRecipe: return $"Unlock {target} recipes";
                case QuestGoal.ClaimExpedition: return $"Complete {target} expeditions";
                case QuestGoal.HireHelper: return "Hire the apprentice";
                case QuestGoal.Prestige: return "Rekindle the forge";
                case QuestGoal.BuildRunes: return $"Raise {target} rune levels";
                case QuestGoal.OwnRarity: return $"Forge a {RarityInfo.NameOf((Rarity)target)} sword";
                case QuestGoal.PlayMinutes: return $"Play for {target} minutes";
                case QuestGoal.ServeOrders: return $"Complete {target} merchant orders";
                case QuestGoal.RushOrders: return $"Complete {target} orders during Rush Hour";
                case QuestGoal.ClaimDailies: return $"Claim {target} daily embers";
                case QuestGoal.UseTools: return $"Use workbench tools {target} times";
                case QuestGoal.PetCat: return $"Pet the forge cat {target} times";
            }
            return "Progress";
        }

        public static string BuildingName(string id)
        {
            GameManager gm = GameManager.Instance;
            BuildingDef def = gm != null && gm.config != null ? gm.config.GetBuilding(id) : null;
            return def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : "building";
        }
    }
}
