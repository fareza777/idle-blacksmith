using IdleBlacksmith.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One achievement row: icon, name, requirement, progress and its permanent bonus.</summary>
    public class AchRow : MonoBehaviour
    {
        public Image icon;
        public Image frame;
        public TMP_Text nameLabel;
        public TMP_Text descLabel;
        public TMP_Text bonusLabel;
        public TMP_Text progressLabel;
        public GameObject unlockedBadge;
        public CanvasGroup content;

        AchievementDef def;

        static readonly Color Unlocked = new Color(0.36f, 0.62f, 0.35f);
        static readonly Color LockedTint = new Color(0.66f, 0.63f, 0.60f);

        public void Bind(AchievementDef achievementDef)
        {
            def = achievementDef;
            if (icon != null && def.icon != null) icon.sprite = def.icon;
            if (nameLabel != null) nameLabel.text = def.displayName;
            if (descLabel != null) descLabel.text = def.description;
            if (bonusLabel != null) bonusLabel.text = BonusText(def);
            Refresh();
        }

        static string BonusText(AchievementDef a)
        {
            string kind;
            switch (a.bonusKind)
            {
                case AchBonus.Gold: kind = "gold"; break;
                case AchBonus.Ore: kind = "ore mined"; break;
                case AchBonus.Price: kind = "sword prices"; break;
                case AchBonus.Craft: kind = "forge speed"; break;
                case AchBonus.Luck: kind = "luck"; break;
                default: kind = "offline rate"; break;
            }
            float pct = a.bonusKind == AchBonus.Luck ? a.bonus : a.bonus * 100f;
            string amount = a.bonusKind == AchBonus.Luck ? $"+{pct:0.#}" : $"+{pct:0.#}%";
            return $"{amount} {kind}";
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null || gm.achievements == null) return;

            bool unlocked = gm.achievements.IsUnlocked(def.id);
            long progress = Goals.Progress(def.goal, def.targetId);

            if (unlockedBadge != null) unlockedBadge.SetActive(unlocked);
            if (frame != null) frame.color = unlocked ? Unlocked : LockedTint;
            if (content != null) content.alpha = unlocked ? 1f : 0.6f;
            if (progressLabel != null)
                progressLabel.text = unlocked
                    ? "UNLOCKED"
                    : $"{Mathf.Min(progress, def.target)} / {def.target}";
        }
    }
}
