using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Sword recipes and the rarity roll. A recipe becomes available the moment its
    /// building requirements are met — there is nothing extra to buy, so every smithy or
    /// mine upgrade pays off immediately.
    /// </summary>
    public class RecipeManager : MonoBehaviour
    {
        /// <summary>Fired when the available set changes or the active recipe is switched.</summary>
        public event Action OnChanged;

        /// <summary>Fired with each recipe id that just became available.</summary>
        public event Action<string> OnRecipeUnlocked;

        GameConfig config;
        BuildingManager buildings;
        readonly System.Random rng = new System.Random();

        public void Init(GameConfig cfg, BuildingManager buildingManager, SaveData data)
        {
            config = cfg;
            buildings = buildingManager;
            RefreshUnlocks(false);
        }

        public BuildingManager Buildings { set => buildings = value; }

        public IReadOnlyList<RecipeDef> All
            => config != null && config.recipes != null ? config.recipes : Array.Empty<RecipeDef>();

        public RecipeDef Get(string id) => config != null ? config.GetRecipe(id) : null;

        public string ActiveId
        {
            get
            {
                GameManager gm = GameManager.Instance;
                string id = gm != null && gm.Data != null ? gm.Data.activeRecipeId : null;
                return string.IsNullOrEmpty(id) ? RecipeId.Copper : id;
            }
        }

        public RecipeDef Active
        {
            get
            {
                RecipeDef r = Get(ActiveId);
                if (r != null && IsAvailable(r)) return r;
                return Get(RecipeId.Copper) ?? (All.Count > 0 ? All[0] : null);
            }
        }

        public bool IsAvailable(RecipeDef r)
        {
            if (r == null) return false;
            if (buildings == null) return r.requiredSmithyLevel <= 1 && r.requiredMineLevel <= 0;
            return buildings.GetLevel(BuildingId.Smithy) >= r.requiredSmithyLevel
                && buildings.GetLevel(BuildingId.Mine) >= r.requiredMineLevel;
        }

        public int AvailableCount
        {
            get
            {
                int n = 0;
                foreach (RecipeDef r in All)
                    if (IsAvailable(r)) n++;
                return n;
            }
        }

        /// <summary>The recipe that would become available next, or null when all are open.</summary>
        public RecipeDef NextLocked
        {
            get
            {
                foreach (RecipeDef r in All)
                    if (!IsAvailable(r)) return r;
                return null;
            }
        }

        /// <summary>Human-readable progress toward the next recipe, for the ticker and tooltips.</summary>
        public string NextUnlockHint()
        {
            RecipeDef r = NextLocked;
            if (r == null) return "Every recipe researched";
            if (buildings == null) return r.displayName;

            var parts = new System.Collections.Generic.List<string>();
            int smithy = buildings.GetLevel(BuildingId.Smithy);
            int mine = buildings.GetLevel(BuildingId.Mine);
            if (smithy < r.requiredSmithyLevel) parts.Add($"Smithy {smithy}/{r.requiredSmithyLevel}");
            if (mine < r.requiredMineLevel) parts.Add($"Mine {mine}/{r.requiredMineLevel}");
            return $"{r.displayName}: " + string.Join(", ", parts);
        }

        /// <summary>Sets the recipe the smith forges next. Returns false if it is still locked.</summary>
        public bool SetActive(string id)
        {
            RecipeDef r = Get(id);
            if (r == null || !IsAvailable(r)) return false;
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null) return false;
            if (gm.Data.activeRecipeId == id) return false;
            gm.Data.activeRecipeId = id;
            OnChanged?.Invoke();
            gm.Save();
            return true;
        }

        /// <summary>
        /// Re-checks every recipe against the building levels and records the ones that are
        /// now open. <paramref name="announce"/> is false during initial load so a returning
        /// player is not spammed with unlocks they already had.
        /// </summary>
        public void RefreshUnlocks(bool announce = true)
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Data == null || config == null) return;
            if (gm.Data.unlockedRecipes == null) gm.Data.unlockedRecipes = new List<string>();

            bool changed = false;
            foreach (RecipeDef r in All)
            {
                if (r == null || !IsAvailable(r)) continue;
                if (gm.Data.unlockedRecipes.Contains(r.id)) continue;
                gm.Data.unlockedRecipes.Add(r.id);
                changed = true;
                if (announce) OnRecipeUnlocked?.Invoke(r.id);
            }

            int count = gm.Data.unlockedRecipes.Count;
            if (gm.Data.stats != null && count > gm.Data.stats.recipesUnlocked)
                gm.Data.stats.recipesUnlocked = count;

            if (changed || announce) OnChanged?.Invoke();
        }

        /// <summary>Rolls the rarity for a freshly forged sword, weighted by the player's luck.</summary>
        public Rarity RollRarity()
        {
            float luck = 0f;
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.upgrades != null) luck = gm.upgrades.Luck;
            return RarityInfo.Roll(luck, rng);
        }
    }
}
