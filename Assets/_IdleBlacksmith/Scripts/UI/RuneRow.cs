using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One rune row: icon, level out of the current cap, effect and buy button.</summary>
    public class RuneRow : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descLabel;
        public TMP_Text levelLabel;
        public TMP_Text costLabel;
        public GameObject costPill;
        public BouncyButton buyButton;
        public GameObject maxBadge;
        public CanvasGroup content;

        RuneDef def;

        static readonly Color CostBad = new Color(1f, 0.5f, 0.45f);

        public void Bind(RuneDef runeDef)
        {
            def = runeDef;
            if (icon != null && def.icon != null) icon.sprite = def.icon;
            if (nameLabel != null) nameLabel.text = def.displayName;
            if (descLabel != null) descLabel.text = def.description;
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null || gm.runes == null) return;

            int level = gm.runes.GetLevel(def.id);
            int cap = gm.runes.MaxLevel;
            bool maxed = gm.runes.IsMaxed(def);
            int cost = gm.runes.GetCost(def);
            bool afford = gm.RelicOre >= cost;

            if (levelLabel != null) levelLabel.text = $"Lv {level}/{cap}";
            if (maxBadge != null) maxBadge.SetActive(maxed);
            if (costPill != null) costPill.SetActive(!maxed);
            if (costLabel != null && !maxed)
            {
                costLabel.text = cost.ToString();
                costLabel.color = afford ? Color.white : CostBad;
            }
            if (buyButton != null) buyButton.interactable = !maxed && afford;
            if (content != null) content.alpha = maxed || afford ? 1f : 0.68f;
        }

        void OnBuy()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.runes == null) return;

            if (gm.runes.TryBuy(def, gm))
            {
                AudioManager.Play("enchant");
                Tween.PunchScale(transform, Vector3.one * 0.07f, 0.4f);
                UIManager.Instance?.SpawnFloatingText(
                    Vector3.up * 2.2f, def.displayName + " +1", new Color(0.72f, 0.86f, 1f));
            }
            else
            {
                AudioManager.Play("denied");
            }
        }
    }
}
