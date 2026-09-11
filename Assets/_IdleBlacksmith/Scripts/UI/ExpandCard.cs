using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>Purchase card for the next Smithy level. Superseded by the full Complex panel.</summary>
    public class ExpandCard : MonoBehaviour
    {
        public TMP_Text stateLabel;
        public TMP_Text perkLabel;
        public TMP_Text costLabel;
        public GameObject costPill;
        public BouncyButton expandButton;
        public GameObject maxBadge;
        public CanvasGroup content;

        static readonly Color CostBad = new Color(1f, 0.5f, 0.45f);

        public void Bind()
        {
            if (expandButton != null) expandButton.onClick.AddListener(OnExpand);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            BuildingDef def = gm.buildings != null ? gm.buildings.Def(BuildingId.Smithy) : null;
            string name = def != null && !string.IsNullOrEmpty(def.displayName) ? def.displayName : "The Smithy";
            int level = gm.ShopTier;
            bool maxed = !gm.CanExpand;

            if (maxBadge != null) maxBadge.SetActive(maxed);
            if (costPill != null) costPill.SetActive(!maxed);

            if (stateLabel != null)
                stateLabel.text = maxed
                    ? $"{name} Lv{level} — fully grown!"
                    : $"{name}  Lv{level}  →  Lv{level + 1}";
            if (perkLabel != null)
                perkLabel.text = maxed ? "The finest forge in the realm" : PerkText(def, level + 1);

            if (maxed)
            {
                if (expandButton != null) expandButton.interactable = false;
                if (content != null) content.alpha = 1f;
                return;
            }

            int cost = gm.ExpandCost;
            bool afford = gm.economy != null && gm.economy.Gold >= cost;
            if (costLabel != null)
            {
                costLabel.text = GoldCounter.Format(cost);
                costLabel.color = afford ? Color.white : CostBad;
            }
            if (expandButton != null) expandButton.interactable = afford;
            if (content != null) content.alpha = afford ? 1f : 0.72f;
        }

        /// <summary>The perk line for a given level, falling back to the building description.</summary>
        static string PerkText(BuildingDef def, int level)
        {
            if (def != null && def.levelPerks != null && level - 1 >= 0 && level - 1 < def.levelPerks.Length
                && !string.IsNullOrEmpty(def.levelPerks[level - 1]))
                return def.levelPerks[level - 1];
            return def != null ? def.description : "";
        }

        void OnExpand()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.TryExpandShop())
            {
                AudioManager.Play("fanfare");
                Tween.PunchScale(transform, Vector3.one * 0.08f, 0.45f);
                if (UIManager.Instance != null && gm.environmentRoot != null)
                    UIManager.Instance.SpawnFloatingText(
                        gm.environmentRoot.position + new Vector3(0f, 3.4f, 0f),
                        "Shop expanded!", new Color(1f, 0.8f, 0.35f));
            }
            else
            {
                AudioManager.Play("denied");
            }
        }
    }
}
