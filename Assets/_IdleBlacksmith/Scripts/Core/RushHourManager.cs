using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Rush hour: a short periodic surge where the queue out the door triples and every
    /// buyer pays a premium. Announcements float up, the HUD banner counts the seconds,
    /// and both multipliers read back through PaceMult / PriceMult.
    /// </summary>
    public class RushHourManager : MonoBehaviour
    {
        [Header("Cadence")]
        [Tooltip("Delay between one rush ending and the next")]
        public Vector2 idleDelay = new Vector2(240f, 420f);
        [Tooltip("How long the rush lasts")]
        public float duration = 20f;
        [Tooltip("First rush arrives this many seconds into the session")]
        public float firstDelay = 150f;

        [Header("Effect")]
        [Tooltip("Customer gap multiplier while rushing - 0.33 means roughly 3x the foot traffic")]
        public float customerPaceMult = 0.33f;
        [Tooltip("Sale price multiplier while rushing")]
        public float priceBonus = 1.25f;

        public bool Active { get; private set; }
        public float PaceMult => Active ? customerPaceMult : 1f;
        public float PriceMult => Active ? priceBonus : 1f;
        public float TimeLeft => Active ? Mathf.Max(0f, endsAt - Time.time) : 0f;
        public event System.Action OnChanged;

        float nextAt;
        float endsAt;

        void Start()
        {
            nextAt = Time.time + firstDelay;
        }

        void Update()
        {
            if (!Active)
            {
                if (Time.time >= nextAt)
                {
                    Active = true;
                    endsAt = Time.time + duration;
                    UIManager.Instance?.SpawnFloatingText(
                        new Vector3(0f, 2.5f, 0f), "RUSH HOUR!", new Color(1f, 0.62f, 0.25f));
                    UIManager.Instance?.FlashScreen(new Color(1f, 0.55f, 0.2f), 0.28f, 0.9f);
                    UI.SettingsPanel.Buzz();
                    AudioManager.Play("ember_whoosh", 0.04f, 0.8f);
                    OnChanged?.Invoke();
                }
                return;
            }

            if (Time.time >= endsAt)
            {
                Active = false;
                nextAt = Time.time + Random.Range(idleDelay.x, idleDelay.y);
                UIManager.Instance?.FlashScreen(new Color(0.45f, 0.6f, 1f), 0.18f, 0.8f);
                AudioManager.Play("blip", 0.06f, 0.6f);
                OnChanged?.Invoke();
            }
        }

        /// <summary>Kicks a rush in immediately - used by the smoke test and QA builds.</summary>
        public void ForceStart()
        {
            if (!Active) nextAt = Time.time;
        }
    }
}
