using IdleBlacksmith.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleBlacksmith.UI
{
    /// <summary>
    /// The always-visible objective banner under the HUD. It exists so a new player always
    /// knows the next step without opening anything.
    /// </summary>
    public class HudTicker : MonoBehaviour
    {
        public TMP_Text objectiveLabel;
        public TMP_Text progressLabel;
        public Image fill;
        public Button openButton;
        public QuestPanel questPanel;
        public CanvasGroup group;

        float pulseTimer;

        public void Init()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.quests != null)
            {
                gm.quests.OnChanged += Refresh;
                gm.quests.OnQuestCompleted += HandleCompleted;
            }
            if (openButton != null)
                openButton.onClick.AddListener(() =>
                {
                    if (questPanel != null) questPanel.Toggle();
                });
            Refresh();
        }

        void HandleCompleted(QuestDef q)
        {
            AudioManager.Play("quest_done");
            if (group != null)
            {
                group.alpha = 1f;
                pulseTimer = 1.2f;
            }
            if (UIManager.Instance != null)
                UIManager.Instance.SpawnFloatingText(
                    new Vector3(0f, 1.6f, 0f), "Quest complete!", new Color(1f, 0.86f, 0.42f));
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.quests == null) return;

            if (objectiveLabel != null) objectiveLabel.text = gm.quests.TickerText;
            if (progressLabel != null)
            {
                QuestDef q = gm.quests.Active;
                progressLabel.text = q == null ? "" : $"{Mathf.Min(gm.quests.Progress, q.target)}/{q.target}";
            }
            if (fill != null) fill.fillAmount = gm.quests.Fill01;
        }

        void Update()
        {
            if (pulseTimer <= 0f) return;
            pulseTimer -= Time.deltaTime;
            // A short highlight when a quest lands, so the reward is noticed.
            if (group != null)
                group.alpha = 1f;
        }
    }
}
