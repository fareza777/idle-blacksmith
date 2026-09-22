using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// Rekindle the forge: burns the run for ember shards and buys permanent talents.
    /// This is the game's meta layer and its endgame loop.
    /// </summary>
    public class PrestigePanel : MonoBehaviour
    {
        [Header("Refs")]
        public RectTransform sheet;
        public CanvasGroup backdrop;
        public Button backdropButton;
        public Button closeButton;
        public Image artImage;

        [Header("Summary")]
        public TMP_Text shardLabel;
        public TMP_Text pendingLabel;
        public TMP_Text runLabel;
        public TMP_Text countLabel;
        public Image progressFill;
        public BouncyButton prestigeButton;
        public TMP_Text prestigeButtonLabel;

        [Header("Talents")]
        public TalentRow talentRowPrefab;
        public Transform talentsParent;

        [Header("Confirm modal")]
        public GameObject confirmRoot;
        public TMP_Text confirmBody;
        public BouncyButton confirmYes;
        public BouncyButton confirmNo;

        [Header("Layout")]
        public float openY = 26f;
        public float closedY = -1900f;

        public bool IsOpen { get; private set; }

        readonly System.Collections.Generic.List<TalentRow> rows = new System.Collections.Generic.List<TalentRow>();
        bool built;

        public void Init()
        {
            BuildRows();
            gameObject.SetActive(false);
            if (confirmRoot != null) confirmRoot.SetActive(false);

            GameManager gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.prestige != null) gm.prestige.OnChanged += RefreshAll;
                if (gm.talents != null) gm.talents.OnChanged += RefreshAll;
                if (gm.economy != null) gm.economy.OnGoldChanged += (_, __) => RefreshSummary();
            }
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (prestigeButton != null) prestigeButton.onClick.AddListener(AskConfirm);
            if (confirmNo != null) confirmNo.onClick.AddListener(CloseConfirm);
            if (confirmYes != null) confirmYes.onClick.AddListener(DoPrestige);
        }

        void BuildRows()
        {
            if (built) return;
            built = true;
            GameManager gm = GameManager.Instance;
            GameConfig config = gm != null ? gm.config : null;
            if (config == null || config.talents == null || talentRowPrefab == null || talentsParent == null) return;

            foreach (TalentDef def in config.talents)
            {
                if (def == null) continue;
                TalentRow row = Instantiate(talentRowPrefab, talentsParent);
                row.Bind(def);
                rows.Add(row);
            }
        }

        public void RefreshAll()
        {
            RefreshSummary();
            foreach (TalentRow r in rows)
                if (r != null) r.Refresh();
        }

        void RefreshSummary()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.prestige == null) return;

            if (shardLabel != null) shardLabel.text = gm.prestige.Shards.ToString();
            if (countLabel != null)
                countLabel.text = gm.prestige.Count == 0
                    ? "The forge has never been rekindled"
                    : $"Rekindled {gm.prestige.Count} time{(gm.prestige.Count == 1 ? "" : "s")}";

            int pending = gm.prestige.PendingShards;
            if (pendingLabel != null)
                pendingLabel.text = pending > 0
                    ? $"Rekindling now grants {pending} ember shard{(pending == 1 ? "" : "s")}"
                    : $"Earn {GoldCounter.Format(gm.prestige.GoldToNextShard)} more gold this run for your first shard";

            if (runLabel != null)
                runLabel.text = $"This run: {GoldCounter.Format(gm.prestige.RunEarned)} gold earned";

            if (progressFill != null)
            {
                float divisor = gm.config != null ? gm.config.prestigeGoldDivisor : 5000f;
                float ratio = divisor > 0f ? Mathf.Clamp01(gm.prestige.RunEarned / divisor) : 0f;
                progressFill.fillAmount = gm.prestige.RunEarned >= divisor ? 1f : ratio;
            }

            if (prestigeButton != null) prestigeButton.interactable = pending > 0;
            if (prestigeButtonLabel != null)
                prestigeButtonLabel.text = pending > 0 ? "REKINDLE" : "NOT READY";
        }

        void AskConfirm()
        {
            if (confirmRoot == null) { DoPrestige(); return; }
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.prestige == null) return;

            int pending = gm.prestige.PendingShards;
            if (confirmBody != null)
                confirmBody.text =
                    $"Rekindle the forge for {pending} ember shard{(pending == 1 ? "" : "s")}?\n\n" +
                    "You lose gold, ore, buildings, upgrades and runes.\n" +
                    "You keep talents, shards, quests, achievements and every stat.";
            confirmRoot.SetActive(true);
            AudioManager.Play("pop", 0.03f);
        }

        void CloseConfirm()
        {
            if (confirmRoot != null) confirmRoot.SetActive(false);
        }

        void DoPrestige()
        {
            CloseConfirm();
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.prestige == null) return;

            int gained = gm.prestige.DoPrestige();
            if (gained <= 0) { AudioManager.Play("denied"); return; }

            AudioManager.Play("prestige");
            UIManager.Instance?.FlashScreen(new Color(1f, 0.72f, 0.35f), 0.85f, 1.2f);
            Tween.PunchScale(transform, Vector3.one * 0.09f, 0.6f);
            UIManager.Instance?.SpawnFloatingText(
                new Vector3(0f, 2.2f, -0.6f), $"+{gained} ember shards!", new Color(1f, 0.62f, 0.3f));
            RefreshAll();
        }

        public void Open()
        {
            if (IsOpen || sheet == null) return;
            IsOpen = true;
            gameObject.SetActive(true);
            RefreshAll();
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
            CloseConfirm();
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
            BuildRows();
            gameObject.SetActive(true);
            IsOpen = true;
            CloseConfirm();
            RefreshAll();
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
