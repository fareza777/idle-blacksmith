using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One upgrade line: icon, name, level pips, cost button, MAX state.</summary>
    public class UpgradeRow : MonoBehaviour
    {
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text descLabel;
        public TMP_Text levelLabel;
        public Image[] pips;
        public TMP_Text costLabel;
        public GameObject costPill;
        public BouncyButton buyButton;
        public GameObject maxBadge;
        public CanvasGroup content;

        static readonly Color PipOn = new Color(1f, 0.78f, 0.25f);
        static readonly Color PipOff = new Color(0.87f, 0.79f, 0.68f);
        static readonly Color CostBad = new Color(1f, 0.5f, 0.45f);

        UpgradeDef def;

        public void Bind(UpgradeDef upgradeDef)
        {
            def = upgradeDef;
            if (icon != null) icon.sprite = def.icon;
            if (nameLabel != null) nameLabel.text = def.displayName;
            if (descLabel != null) descLabel.text = def.description;
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null) return;

            int lvl = gm.upgrades.GetLevel(def.id);
            bool maxed = gm.upgrades.IsMaxed(def);

            if (levelLabel != null) levelLabel.text = "Lv " + lvl + "/" + def.maxLevel;
            if (pips != null)
                for (int i = 0; i < pips.Length; i++)
                {
                    if (pips[i] == null) continue;
                    pips[i].gameObject.SetActive(i < def.maxLevel);
                    pips[i].color = i < lvl ? PipOn : PipOff;
                }

            if (maxBadge != null) maxBadge.SetActive(maxed);
            if (costPill != null) costPill.SetActive(!maxed);

            if (maxed)
            {
                if (buyButton != null) buyButton.interactable = false;
                if (content != null) content.alpha = 1f;
                return;
            }

            int cost = gm.upgrades.GetCost(def);
            bool afford = gm.economy != null && gm.economy.Gold >= cost;
            if (costLabel != null)
            {
                costLabel.text = GoldCounter.Format(cost);
                costLabel.color = afford ? Color.white : CostBad;
            }
            if (buyButton != null) buyButton.interactable = afford;
            if (content != null) content.alpha = afford ? 1f : 0.72f;
        }

        void OnBuy()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.upgrades.TryBuy(def, gm.economy))
            {
                Tween.PunchScale(transform, Vector3.one * 0.05f, 0.3f);
                AudioManager.Play("upgrade");
            }
            else
            {
                AudioManager.Play("denied");
            }
        }
    }
}
