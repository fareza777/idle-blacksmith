using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Market fair: every fifth day over the forge the village holds a fair — bunting goes
    /// up in the yard, customers walk in faster and every sale pays a premium until the
    /// next dawn. The day counter (dayCycles) decides the day; the manager only watches it.
    /// </summary>
    public class MarketFairManager : MonoBehaviour
    {
        [Tooltip("One fair every N displayed days — Day 5, 10, 15...")]
        public int fairIntervalDays = 5;
        [Tooltip("Customer gap multiplier during a fair — 0.65 means about a third shorter waits")]
        public float customerPaceMult = 0.65f;
        [Tooltip("Sale price multiplier during a fair")]
        public float priceBonus = 1.5f;

        public bool Active { get; private set; }
        public float PaceMult => Active ? customerPaceMult : 1f;
        public float PriceMult => Active ? priceBonus : 1f;
        public event System.Action OnChanged;

        int announcedDay = -1;

        void Update()
        {
            var d = GameManager.Instance != null ? GameManager.Instance.Data : null;
            int dawns = d != null && d.stats != null ? d.stats.dayCycles : 0;
            // dawns==0 is still Day 1 — the forge's first fair lands on the fifth dawn.
            bool fair = fairIntervalDays > 0 && dawns > 0
                        && dawns % fairIntervalDays == fairIntervalDays - 1;
            if (fair == Active) return;
            Active = fair;

            if (Active && dawns != announcedDay)
            {
                announcedDay = dawns;
                UIManager.Instance?.SpawnFloatingText(
                    new Vector3(0f, 2.6f, 0f), "MARKET FAIR!", new Color(1f, 0.78f, 0.25f));
                UIManager.Instance?.FlashScreen(new Color(1f, 0.66f, 0.28f), 0.26f, 0.85f);
                UI.SettingsPanel.Buzz();
                AudioManager.Play("ember_whoosh", 0.045f, 0.85f);
            }
            OnChanged?.Invoke();
        }
    }
}
