using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One-time purchase card: hire the helper blacksmith.</summary>
    public class HelperCard : MonoBehaviour
    {
        public TMP_Text stateLabel;
        public TMP_Text costLabel;
        public GameObject costPill;
        public BouncyButton hireButton;
        public GameObject hiredBadge;
        public CanvasGroup content;

        static readonly Color CostBad = new Color(1f, 0.5f, 0.45f);

        public void Bind()
        {
            if (hireButton != null) hireButton.onClick.AddListener(OnHire);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            bool unlocked = gm.HelperUnlocked;

            if (hiredBadge != null) hiredBadge.SetActive(unlocked);
            if (costPill != null) costPill.SetActive(!unlocked);
            if (stateLabel != null)
                stateLabel.text = unlocked ? "Working hard!" : "Hire a second blacksmith";

            if (unlocked)
            {
                if (hireButton != null) hireButton.interactable = false;
                if (content != null) content.alpha = 1f;
                return;
            }

            int cost = gm.config.helperCost;
            bool afford = gm.economy != null && gm.economy.Gold >= cost;
            if (costLabel != null)
            {
                costLabel.text = GoldCounter.Format(cost);
                costLabel.color = afford ? Color.white : CostBad;
            }
            if (hireButton != null) hireButton.interactable = afford;
            if (content != null) content.alpha = afford ? 1f : 0.72f;
        }

        void OnHire()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.TryUnlockHelper())
            {
                AudioManager.Play("hire");
                Tween.PunchScale(transform, Vector3.one * 0.08f, 0.45f);
                if (gm.helperSpawnPoint != null && UIManager.Instance != null)
                    UIManager.Instance.SpawnFloatingText(
                        gm.helperSpawnPoint.position + Vector3.up * 1.2f,
                        "Helper hired!", new Color(0.45f, 1f, 0.55f));
            }
            else
            {
                AudioManager.Play("denied");
            }
        }
    }
}
