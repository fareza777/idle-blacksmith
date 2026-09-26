using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// The quest sheet: shows the current objective in full, how far along it is, what it pays,
    /// and the road already walked.
    /// </summary>
    public class QuestPanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public TMP_Text titleLabel;
        public TMP_Text bodyLabel;
        public TMP_Text progressLabel;
        public TMP_Text rewardLabel;
        public TMP_Text nextLabel;
        public TMP_Text counterLabel;
        public Image fill;
        public Button achievementsButton;
        [Tooltip("Pans the camera to the quest's target building, then closes the sheet")]
        public Button showButton;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        public void Init()
        {
            gameObject.SetActive(false);
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.quests != null) gm.quests.OnChanged += Refresh;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (showButton != null) showButton.onClick.AddListener(OnShow);
        }

        /// <summary>
        /// "Show me": drop the sheet and slide the camera to the building the quest wants.
        /// Quests without a physical target (passive goals) hide the button instead.
        /// </summary>
        void OnShow()
        {
            var marker = FindFirstObjectByType<Gameplay.QuestMarker>();
            Transform target = marker != null ? marker.ResolveTarget() : null;
            var director = FindFirstObjectByType<Gameplay.CameraDirector>();
            if (target != null && director != null)
                director.FocusOn(target.position);
            Close();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.quests == null) return;

            QuestDef q = gm.quests.Active;
            if (counterLabel != null)
                counterLabel.text = $"{gm.quests.ClaimedCount} / {gm.quests.TotalCount} complete";

            if (q == null)
            {
                if (titleLabel != null) titleLabel.text = "The forge is legendary";
                if (bodyLabel != null) bodyLabel.text = "Every quest is done. Rekindle for ember shards, or keep forging for a Legendary sword.";
                if (progressLabel != null) progressLabel.text = "";
                if (rewardLabel != null) rewardLabel.text = "";
                if (nextLabel != null) nextLabel.text = "";
                if (fill != null) fill.fillAmount = 1f;
                if (showButton != null) showButton.gameObject.SetActive(false);
                return;
            }

            // Passive goals have nowhere to point at — the button hides rather than dead-tap.
            if (showButton != null)
            {
                var marker = FindFirstObjectByType<Gameplay.QuestMarker>();
                showButton.gameObject.SetActive(marker != null && marker.ResolveTarget() != null);
            }

            if (nextLabel != null)
            {
                QuestDef next = gm.quests.Next;
                nextLabel.text = next != null ? $"Up next: {next.title}" : "Final quest — the road ends in legend";
            }

            if (titleLabel != null) titleLabel.text = q.title;
            if (bodyLabel != null) bodyLabel.text = q.body;
            if (progressLabel != null)
                progressLabel.text = $"{Mathf.Min(gm.quests.Progress, q.target)} / {q.target}";
            if (rewardLabel != null) rewardLabel.text = RewardText(q);
            if (fill != null) fill.fillAmount = gm.quests.Fill01;
        }

        static string RewardText(QuestDef q)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (q.goldReward > 0) parts.Add($"+{GoldCounter.Format(q.goldReward)} gold");
            if (q.oreReward > 0) parts.Add($"+{q.oreReward} ore");
            if (q.relicReward > 0) parts.Add($"+{q.relicReward} relic ore");
            if (q.shardReward > 0) parts.Add($"+{q.shardReward} ember");
            return parts.Count > 0 ? string.Join("   ", parts) : "Nothing but glory";
        }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            gameObject.SetActive(true);
            Refresh();
            float x = sheet.anchoredPosition.x;
            sheet.anchoredPosition = new Vector2(x, closedY);
            Tween.UIAnchoredPosition(sheet, new Vector2(x, openY), 0.45f, Ease.OutBack);
            if (backdrop != null)
            {
                backdrop.alpha = 0f;
                Tween.Alpha(backdrop, 1f, 0.3f, Ease.OutQuad);
                backdrop.blocksRaycasts = true;
                backdrop.interactable = true;
            }
            AudioManager.Play("pop", 0.03f);
        }

        public void Close()
        {
            if (!IsOpen || sheet == null) return;
            IsOpen = false;
            AudioManager.Play("whoosh", 0.04f, 0.45f);
            if (backdrop != null)
            {
                backdrop.blocksRaycasts = false;
                backdrop.interactable = false;
                Tween.Alpha(backdrop, 0f, 0.25f, Ease.InQuad);
            }
            float x = sheet.anchoredPosition.x;
            Tween.UIAnchoredPosition(sheet, new Vector2(x, closedY), 0.3f, Ease.InBack)
                .OnComplete(() => { if (!IsOpen) gameObject.SetActive(false); });
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void PreviewOpenForScreenshot()
        {
            gameObject.SetActive(true);
            IsOpen = true;
            Refresh();
            if (sheet != null) sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, openY);
            if (backdrop != null) backdrop.alpha = 1f;
        }

        public void PreviewClose()
        {
            IsOpen = false;
            if (sheet != null) sheet.anchoredPosition = new Vector2(sheet.anchoredPosition.x, closedY);
            if (backdrop != null) backdrop.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
