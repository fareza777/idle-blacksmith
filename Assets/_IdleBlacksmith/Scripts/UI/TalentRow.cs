using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One permanent talent: what it does at this level and what the next costs in shards.</summary>
    public class TalentRow : MonoBehaviour
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

        TalentDef def;

        static readonly Color CostBad = new Color(1f, 0.5f, 0.45f);
        static readonly Color Ember = new Color(1f, 0.72f, 0.32f);

        public void Bind(TalentDef talentDef)
        {
            def = talentDef;
            if (icon != null && def.icon != null) icon.sprite = def.icon;
            if (nameLabel != null) nameLabel.text = def.displayName;
            if (descLabel != null) descLabel.text = def.description;
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null || gm.talents == null || gm.prestige == null) return;

            int level = gm.talents.GetLevel(def.id);
            bool maxed = gm.talents.IsMaxed(def);
            int cost = gm.talents.GetCost(def);
            bool afford = gm.prestige.Shards >= cost;

            if (levelLabel != null) levelLabel.text = $"Lv {level}/{def.maxLevel}";
            if (maxBadge != null) maxBadge.SetActive(maxed);
            if (costPill != null) costPill.SetActive(!maxed);
            if (costLabel != null && !maxed)
            {
                costLabel.text = cost.ToString();
                costLabel.color = afford ? Ember : CostBad;
            }
            if (buyButton != null) buyButton.interactable = !maxed && afford;
            if (content != null) content.alpha = maxed || afford ? 1f : 0.68f;
        }

        void OnBuy()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.talents == null) return;

            if (gm.talents.TryBuy(def, gm))
            {
                AudioManager.Play("levelup");
                Tween.PunchScale(transform, Vector3.one * 0.07f, 0.4f);
                UIManager.Instance?.SpawnFloatingText(
                    Vector3.up * 2f, def.displayName + " +1", Ember);
            }
            else
            {
                AudioManager.Play("denied");
            }
        }
    }
}
