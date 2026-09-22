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
        public TMP_Text nameLabel;
        public TMP_Text statLabel;
        public TMP_Text forgedLabel;
        public TMP_Text descLabel;
        public BouncyButton selectButton;
        public TMP_Text selectLabel;
        public GameObject lockedBadge;
        public TMP_Text lockedLabel;
        public GameObject activeBadge;
        public CanvasGroup content;

        RecipeDef def;

        static readonly Color Active = new Color(0.36f, 0.62f, 0.35f);
        static readonly Color LockedTint = new Color(0.62f, 0.60f, 0.58f);

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

            if (lockedBadge != null) lockedBadge.SetActive(!unlocked);
            if (activeBadge != null) activeBadge.SetActive(active && unlocked);
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
                StatBlock st = gm.Data != null ? gm.Data.stats : null;
                int forged = st != null ? st.ForgedCount(def.id) : 0;
                int best = st != null ? st.BestRarityOf(def.id) : -1;
                if (forged > 0 && best >= 0)
                {
                    forgedLabel.text = forged + " forged  ·  best " + RarityInfo.NameOf((Rarity)best);
                    forgedLabel.color = RarityInfo.TextColor((Rarity)best);
                }
                else
                {
                    forgedLabel.text = "";
                }
            }

            if (frame != null) frame.color = active ? Active : unlocked ? Color.white : LockedTint;
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
