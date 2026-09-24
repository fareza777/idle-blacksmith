using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One recipe card: metal, cost, value, craft time, unlock state and a pick button.</summary>
    public class RecipeCard : MonoBehaviour
    {
        [Header("Refs")]
        public Image icon;
        public Image frame;
        public Image accent;
        public TMP_Text nameLabel;
        public TMP_Text statLabel;
        public TMP_Text forgedLabel;
        public TMP_Text descLabel;
        public BouncyButton selectButton;
        public TMP_Text selectLabel;
        public GameObject lockedBadge;
        public TMP_Text lockedLabel;
        public GameObject activeBadge;
        public GameObject dailyBadge;
        public GameObject newBadge;
        public CanvasGroup content;

        RecipeDef def;

        static readonly Color Active = new Color(0.36f, 0.62f, 0.35f);
        static readonly Color LockedTint = new Color(0.62f, 0.60f, 0.58f);

        // metal-tier accent colors, matching config recipe order
        static readonly Color[] TierAccents =
        {
            new Color(0.80f, 0.45f, 0.28f), // copper
            new Color(0.55f, 0.60f, 0.68f), // iron
            new Color(0.62f, 0.74f, 0.86f), // steel
            new Color(1.00f, 0.58f, 0.26f), // emberaxe
            new Color(0.78f, 0.85f, 0.95f), // silver
            new Color(0.42f, 0.80f, 0.92f), // frostbrand
            new Color(0.42f, 0.86f, 0.80f), // mithril
            new Color(0.90f, 0.40f, 0.34f), // dragonsteel
            new Color(0.62f, 0.42f, 0.90f), // voidreaver
            new Color(0.98f, 0.78f, 0.30f), // starforged
        };

        public void Bind(RecipeDef recipeDef)
        {
            def = recipeDef;
            if (icon != null && def.icon != null) icon.sprite = def.icon;
            if (nameLabel != null) nameLabel.text = def.displayName;
            if (descLabel != null) descLabel.text = def.description;
            if (statLabel != null)
                statLabel.text = $"{def.oreCost} ore   ·   {def.baseValue} base gold   ·   {def.craftDuration:0.#}s";
            if (selectButton != null) selectButton.onClick.AddListener(OnSelect);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null || gm.recipes == null) return;

            bool unlocked = gm.recipes.IsAvailable(def);
            bool active = gm.recipes.ActiveId == def.id;
            bool afford = gm.resources == null || gm.resources.Ore >= def.oreCost;
            StatBlock stats = gm.Data != null ? gm.Data.stats : null;
            int forgedCount = stats != null ? stats.ForgedCount(def.id) : 0;

            if (lockedBadge != null) lockedBadge.SetActive(!unlocked);
            if (activeBadge != null) activeBadge.SetActive(active && unlocked);
            if (dailyBadge != null) dailyBadge.SetActive(gm.RecipeOfTheDay() == def);
            if (newBadge != null) newBadge.SetActive(unlocked && forgedCount == 0);
            if (lockedLabel != null && !unlocked)
            {
                var parts = new System.Collections.Generic.List<string>();
                int smithy = gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Smithy) : 0;
                int mine = gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Mine) : 0;
                if (smithy < def.requiredSmithyLevel) parts.Add($"Smithy {def.requiredSmithyLevel}");
                if (mine < def.requiredMineLevel) parts.Add($"Mine {def.requiredMineLevel}");
                lockedLabel.text = "Needs " + string.Join(" + ", parts);
            }

            if (selectButton != null)
            {
                // The "Needs …" badge owns the slot while locked — the button under it reads
                // as ghosted text, so hide the whole button rather than just the label.
                selectButton.gameObject.SetActive(unlocked);
                selectButton.interactable = unlocked && !active;
                if (selectLabel != null)
                    selectLabel.text = active ? "FORGING" : "SELECT";
            }

            if (forgedLabel != null)
            {
                int best = stats != null ? stats.BestRarityOf(def.id) : -1;
                if (forgedCount > 0 && best >= 0)
                {
                    forgedLabel.text = forgedCount + " forged  ·  best " + RarityInfo.NameOf((Rarity)best);
                    int tier = gm.MasteryTierOf(def.id);
                    if (tier > 0) forgedLabel.text += "  ·  mastery +" + tier * 4 + "%";
                    forgedLabel.color = RarityInfo.TextColor((Rarity)best);
                }
                else
                {
                    forgedLabel.text = "";
                }
            }

            if (frame != null) frame.color = active ? Active : unlocked ? Color.white : LockedTint;
            if (accent != null)
            {
                int idx = gm.config != null && gm.config.recipes != null
                    ? System.Array.IndexOf(gm.config.recipes, def) : -1;
                Color tier = TierAccents[Mathf.Clamp(idx, 0, TierAccents.Length - 1)];
                accent.color = unlocked ? tier : Color.Lerp(tier, LockedTint, 0.55f);
            }
            if (content != null) content.alpha = unlocked ? (afford ? 1f : 0.75f) : 0.55f;
        }

        void OnSelect()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.recipes == null) return;

            if (gm.recipes.SetActive(def.id))
            {
                AudioManager.Play("unlock");
                Tween.PunchScale(transform, Vector3.one * 0.06f, 0.35f);
                UIManager.Instance?.SpawnFloatingText(
                    Vector3.up * 1.8f, "Forging " + def.displayName, new Color(0.95f, 0.85f, 0.5f));
            }
            else
            {
                AudioManager.Play("denied");
            }
        }
    }
}
