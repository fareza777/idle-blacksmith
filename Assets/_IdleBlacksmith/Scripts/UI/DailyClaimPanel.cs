using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Daily Ember claim card: pops up once per boot while the day's ember is unclaimed
    /// (re-checking until the player claims), queued behind any other modal or the intro.
    /// </summary>
    public class DailyClaimPanel : MonoBehaviour
    {
        public CanvasGroup group;
        public RectTransform card;
        public TMP_Text titleLabel;
        public TMP_Text streakLabel;
        public TMP_Text goldLabel;
        public TMP_Text relicLabel;
        public BouncyButton claimButton;
        public BouncyButton laterButton;

        public bool IsOpen { get; private set; }

        DailyRewardManager mgr;
        float checkAt;

        public void Init(DailyRewardManager manager)
        {
            mgr = manager;
            checkAt = Time.unscaledTime + 4f;
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; group.interactable = false; }
            if (claimButton != null) claimButton.onClick.AddListener(Claim);
            if (laterButton != null) laterButton.onClick.AddListener(Close);
        }

        void Update()
        {
            if (IsOpen || mgr == null || Time.unscaledTime < checkAt) return;
            checkAt = Time.unscaledTime + 20f; // missed this window: re-check quietly
            TryOpen();
        }

        void TryOpen()
        {
            if (!mgr.ClaimAvailable) return;
            var ui = UIManager.Instance;
            if (ui == null || ui.AnyPanelOpen || ui.IntroPlaying) return;

            int day = mgr.NextDay;
            mgr.RewardsFor(day, out int gold, out int relic);
            if (streakLabel != null)
                streakLabel.text = day > 1 ? "Day " + day + " in a row!" : "Your forge thanks you";
            if (goldLabel != null) goldLabel.text = "+" + gold;
            if (relicLabel != null) relicLabel.text = "+" + relic;

            IsOpen = true;
            if (group != null)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
                Tween.Alpha(group, 1f, 0.3f, Ease.OutQuad);
            }
            if (card != null)
            {
                card.localScale = Vector3.one * 0.55f;
                Tween.Scale(card, Vector3.one, 0.45f, Ease.OutBack);
            }
            AudioManager.Play("quest_done", 0.05f, 0.7f);
        }

        void Claim()
        {
            if (mgr != null && mgr.TryClaim(out int gold, out int relic))
            {
                var ui = UIManager.Instance;
                Vector3 at = new Vector3(0f, 2.6f, 0f);
                ui?.SpawnFloatingText(at, "+" + gold + " gold", new Color(1f, 0.84f, 0.3f));
                if (relic > 0)
                    ui?.SpawnFloatingText(at + Vector3.up * 0.5f, "+" + relic + " relic ore", new Color(0.7f, 0.9f, 1f));
                AudioManager.Play("fanfare", 0.04f, 0.85f);
                SettingsPanel.Buzz();
                ui?.FlashScreen(new Color(1f, 0.85f, 0.45f), 0.35f, 0.8f);
            }
            Close();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
                Tween.Alpha(group, 0f, 0.25f, Ease.OutQuad);
            }
        }
    }
}
