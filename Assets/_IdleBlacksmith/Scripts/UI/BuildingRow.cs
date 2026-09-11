using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One building in the Complex sheet: icon, level, perk, cost and buy button.</summary>
    public class BuildingRow : MonoBehaviour
    {
        [Header("Refs")]
        public Image icon;
        public TMP_Text nameLabel;
        public TMP_Text levelLabel;
        public TMP_Text perkLabel;
        public TMP_Text costLabel;
        public GameObject costPill;
        public BouncyButton buyButton;
        public GameObject maxBadge;
        public GameObject lockedBadge;
        public CanvasGroup content;
        public Image[] levelPips;

        [Header("Special")]
        public BouncyButton runeButton;
        public TMP_Text runeButtonLabel;

        BuildingDef def;
        ComplexPanel panel;

        static readonly Color CostBad = new Color(1f, 0.5f, 0.45f);
        static readonly Color LevelGood = new Color(0.36f, 0.62f, 0.35f);

        public void Bind(BuildingDef buildingDef, ComplexPanel owner)
        {
            def = buildingDef;
            panel = owner;
            if (icon != null && def.icon != null) icon.sprite = def.icon;
            if (nameLabel != null) nameLabel.text = def.displayName;
            if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
            if (runeButton != null) runeButton.onClick.AddListener(OnRunes);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null || gm.buildings == null) return;

            int level = gm.buildings.GetLevel(def.id);
            bool built = level > 0;
            bool maxed = gm.buildings.IsMaxed(def.id);
            int cost = gm.buildings.GetCost(def.id);
            bool afford = gm.economy != null && gm.economy.Gold >= cost;

            if (levelLabel != null)
                levelLabel.text = built ? $"Lv {level}/{gm.buildings.Def(def.id).maxLevel}" : "Not built";
            if (levelLabel != null) levelLabel.color = built ? LevelGood : Secondary;

            if (perkLabel != null)
                perkLabel.text = maxed ? "Fully upgraded — " + Perk(level) : Perk(built ? level + 1 : 1);

            if (maxBadge != null) maxBadge.SetActive(maxed);
            if (lockedBadge != null) lockedBadge.SetActive(!maxed && !afford);
            if (costPill != null) costPill.SetActive(!maxed);
            if (costLabel != null && !maxed)
            {
                costLabel.text = GoldCounter.Format(cost);
                costLabel.color = afford ? Color.white : CostBad;
            }
            if (buyButton != null)
            {
                buyButton.interactable = !maxed && afford;
                var label = buyButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = built ? "UPGRADE" : "BUILD";
            }
            if (content != null) content.alpha = maxed || afford ? 1f : 0.68f;

            if (levelPips != null)
                for (int i = 0; i < levelPips.Length; i++)
                    if (levelPips[i] != null)
                        levelPips[i].color = i < level ? LevelGood : new Color(0.78f, 0.74f, 0.68f);

            bool isSanctum = def.id == BuildingId.Sanctum;
            if (runeButton != null) runeButton.gameObject.SetActive(isSanctum && built);
            if (runeButtonLabel != null && isSanctum && gm.runes != null)
                runeButtonLabel.text = $"RUNES  {gm.runes.TotalLevels}/{gm.runes.MaxLevel * 4}";
        }

        static readonly Color Secondary = new Color(0.66f, 0.55f, 0.42f);

        string Perk(int level)
        {
            if (def.levelPerks != null && level - 1 >= 0 && level - 1 < def.levelPerks.Length
                && !string.IsNullOrEmpty(def.levelPerks[level - 1]))
                return def.levelPerks[level - 1];
            return def.description;
        }

        void OnBuy()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.buildings == null || gm.economy == null) return;

            int levelBefore = gm.buildings.GetLevel(def.id);
            if (gm.buildings.TryUpgrade(def.id, gm.economy))
            {
                AudioManager.Play(levelBefore == 0 ? "unlock" : "levelup");
                Tween.PunchScale(transform, Vector3.one * 0.07f, 0.4f);

                string verb = levelBefore == 0 ? "built!" : "upgraded!";
                Vector3 pos = PlotPosition(gm, def.id);
                UIManager.Instance?.SpawnFloatingText(pos + Vector3.up * 2.6f,
                    def.displayName + " " + verb, new Color(0.95f, 0.82f, 0.45f));
            }
            else
            {
                AudioManager.Play("denied");
            }
        }

        static Vector3 PlotPosition(GameManager gm, string id)
        {
            if (gm == null || gm.buildings == null) return Vector3.zero;
            var plots = Object.FindObjectsByType<IdleBlacksmith.Gameplay.BuildingVisuals>(FindObjectsSortMode.None);
            foreach (var plot in plots)
                if (plot != null && plot.buildingId == id) return plot.transform.position;
            return Vector3.zero;
        }

        void OnRunes()
        {
            if (panel != null && panel.runePanel != null)
            {
                AudioManager.Play("pop", 0.03f);
                panel.runePanel.Open();
            }
        }
    }
}
