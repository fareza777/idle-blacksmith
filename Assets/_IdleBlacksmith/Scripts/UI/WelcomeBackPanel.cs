using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// The "while you were away" sheet. Only appears when the mine and shop actually produced
    /// something, so a quick app switch stays silent.
    /// </summary>
    public class WelcomeBackPanel : MonoBehaviour
    {
        [Header("Refs")]
        public CanvasGroup group;
        public RectTransform card;
        public Image artImage;
        public TMP_Text titleLabel;
        public TMP_Text elapsedLabel;
        public TMP_Text oreLabel;
        public TMP_Text goldLabel;
        public TMP_Text expeditionLabel;
        public BouncyButton collectButton;

        public bool IsOpen { get; private set; }

        public void Init()
        {
            gameObject.SetActive(false);
            if (collectButton != null) collectButton.onClick.AddListener(Close);
        }

        /// <summary>Shows the sheet for a completed offline report. Returns false when there is nothing to show.</summary>
        public bool Show(OfflineProgress.Report report)
        {
            if (!report.valid) return false;

            gameObject.SetActive(true);
            IsOpen = true;

            if (elapsedLabel != null)
                elapsedLabel.text = "The forge kept working for " + OfflineProgress.FormatElapsed(report.elapsedSeconds);
            if (oreLabel != null)
                oreLabel.text = report.oreGained > 0 ? $"+{report.oreGained} ore" : "";
            if (goldLabel != null)
                goldLabel.text = report.goldGained > 0 ? $"+{GoldCounter.Format(report.goldGained)} gold" : "";
            if (expeditionLabel != null)
                expeditionLabel.text = report.expeditionsReady > 0
                    ? $"{report.expeditionsReady} expedition{(report.expeditionsReady == 1 ? "" : "s")} ready to claim"
                    : "";

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = true;
                group.interactable = true;
                Tween.Alpha(group, 1f, 0.35f, Ease.OutQuad);
            }
            if (card != null)
            {
                card.localScale = Vector3.one * 0.85f;
                Tween.Scale(card, Vector3.one, 0.45f, Ease.OutBack);
            }
            AudioManager.Play("market_chime");
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            AudioManager.Play("coin");
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
                Tween.Alpha(group, 0f, 0.28f, Ease.InQuad)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
                gameObject.SetActive(false);
        }

        public void PreviewOpenForScreenshot()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            if (group != null) group.alpha = 1f;
            if (card != null) card.localScale = Vector3.one;
            if (elapsedLabel != null) elapsedLabel.text = "The forge kept working for 4h 12m";
            if (oreLabel != null) oreLabel.text = "+980 ore";
            if (goldLabel != null) goldLabel.text = "+14.2K gold";
            if (expeditionLabel != null) expeditionLabel.text = "2 expeditions ready to claim";
        }

        public void PreviewClose()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }
    }
}
