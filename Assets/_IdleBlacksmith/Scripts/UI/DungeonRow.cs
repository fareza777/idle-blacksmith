using IdleBlacksmith.Core;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>One expedition row: info + GO / live progress / CLAIM states.</summary>
    public class DungeonRow : MonoBehaviour
    {
        public TMP_Text nameLabel;
        public TMP_Text descLabel;
        public TMP_Text rewardLabel;
        public TMP_Text durationLabel;
        public BouncyButton goButton;
        public GameObject goState;      // contains the GO button
        public GameObject runState;     // progress + timer
        public Image progressFill;
        public TMP_Text timerLabel;
        public GameObject claimState;   // CLAIM button
        public BouncyButton claimButton;
        public CanvasGroup content;

        ExpeditionDef def;

        public void Bind(ExpeditionDef d)
        {
            def = d;
            if (nameLabel != null) nameLabel.text = d.displayName;
            if (descLabel != null) descLabel.text = d.description;
            if (durationLabel != null) durationLabel.text = DungeonPanel.FormatDuration(d.durationSeconds);
            if (rewardLabel != null) rewardLabel.text = $"+{d.goldReward} gold   +{d.relicOreReward} relic ore";
            if (goButton != null) goButton.onClick.AddListener(OnGo);
            if (claimButton != null) claimButton.onClick.AddListener(OnClaim);
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || def == null || gm.expeditions == null) return;
            ExpeditionManager ex = gm.expeditions;
            bool isThis = ex.ActiveId == def.id;
            bool ready = isThis && ex.ReadyToClaim;

            if (goState != null) goState.SetActive(!isThis);
            if (runState != null) runState.SetActive(isThis && !ready);
            if (claimState != null) claimState.SetActive(ready);
            if (goButton != null) goButton.interactable = !ex.HasActive;
            if (content != null) content.alpha = !isThis && ex.HasActive ? 0.55f : 1f;

            if (isThis && !ready)
            {
                if (progressFill != null) progressFill.fillAmount = ex.Progress01;
                if (timerLabel != null)
                {
                    float s = ex.RemainingSeconds;
                    timerLabel.text = string.Format("{0}:{1:00}", (int)(s / 60f), (int)(s % 60f));
                }
            }
        }

        void OnGo()
        {
            if (GameManager.Instance != null && GameManager.Instance.StartExpedition(def.id))
            {
                AudioManager.Play("upgrade");
                Tween.PunchScale(transform, Vector3.one * 0.06f, 0.4f);
            }
            else AudioManager.Play("denied");
        }

        void OnClaim()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.TryClaimExpedition(out ExpeditionDef claimed))
            {
                AudioManager.Play("fanfare");
                Tween.PunchScale(transform, Vector3.one * 0.1f, 0.5f);
                if (UIManager.Instance != null)
                    UIManager.Instance.SpawnFloatingText(
                        new Vector3(0f, 2.2f, -1f),
                        $"+{claimed.goldReward} gold  +{claimed.relicOreReward} ore",
                        new Color(0.55f, 0.95f, 1f));
            }
            else AudioManager.Play("denied");
        }
    }
}
