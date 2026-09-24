using IdleBlacksmith.Gameplay;
using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Core
{
    /// <summary>
    /// Ambient speech: every so often someone in the shop — a browsing customer, the
    /// patron waiting on a contract, or the smith mid-swing — lets out a line above
    /// their head. Pure flavour; no state, no cost.
    /// </summary>
    public class ChatterManager : MonoBehaviour
    {
        [Tooltip("Seconds between lines")]
        public Vector2 interval = new Vector2(14f, 26f);
        [Tooltip("Height of the bubble above the speaker's feet")]
        public float headHeight = 2.1f;

        static readonly string[] CustomerLines =
        {
            "Lovely blades!", "Best steel in the Vale!", "Worth every coin!",
            "I'll be back for more!", "Fine craftsmanship!", "My cousin needs one of these!",
        };

        static readonly string[] PatronLines =
        {
            "My knights await these blades…", "Do hurry, smith!",
            "The contract is generous - do not waste it!", "I am counting on you!",
        };

        static readonly string[] WorkerLines =
        {
            "Strike while it's hot!", "Another beauty!", "Hah! A fine blade!",
            "The forge sings today!",
        };

        static readonly string[] FairLines =
        {
            "Best prices all season!", "What a crowd today!",
            "Saved all month for this!", "Bunting's up — purses out!",
            "Smell that festival air!", "The fair's worth the walk!",
        };

        static readonly string[] RainLines =
        {
            "Rain's good for the steel!", "Stay dry, friend!",
            "The anvil sings louder in a storm!", "Quench-day for the whole valley!",
        };

        static readonly Color Warm = new Color(1f, 0.92f, 0.78f);
        static readonly Color Gold = new Color(1f, 0.84f, 0.4f);
        static readonly Color Mist = new Color(0.8f, 0.88f, 1f);

        WorkerController worker;
        FairCrowd crowd;
        RainWeather rain;
        float nextAt = 8f;

        void Update()
        {
            if (Time.time < nextAt) return;
            nextAt = Time.time + Random.Range(interval.x, interval.y);
            Say();
        }

        void Say()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || UIManager.Instance == null) return;

            // Prefer the waiting patron — their impatience is the fun part.
            PatronController patron = gm.orders != null ? gm.orders.Patron : null;
            if (patron != null && Random.value < 0.5f)
            {
                Speak(patron.transform.position, Pick(PatronLines), Gold);
                return;
            }

            // On fair days the browsing villagers chatter about the festival.
            if (gm.marketFair != null && gm.marketFair.Active)
            {
                if (crowd == null) crowd = FindFirstObjectByType<FairCrowd>();
                Transform stroller = crowd != null ? crowd.RandomStroller : null;
                if (stroller != null && Random.value < 0.55f)
                {
                    Speak(stroller.position, Pick(FairLines), Gold);
                    return;
                }
            }

            // In a shower whoever's about remarks on the weather.
            if (rain == null) rain = FindFirstObjectByType<RainWeather>();
            if (rain != null && rain.IsRaining)
            {
                CustomerController wet = gm.customerSpawner != null ? gm.customerSpawner.Current : null;
                if (wet != null && Random.value < 0.35f)
                {
                    Speak(wet.transform.position, Pick(RainLines), Mist);
                    return;
                }
            }

            CustomerController shopper = gm.customerSpawner != null ? gm.customerSpawner.Current : null;
            if (shopper != null && Random.value < 0.55f)
            {
                Speak(shopper.transform.position, Pick(CustomerLines), Warm);
                return;
            }

            if (worker == null)
                worker = FindFirstObjectByType<WorkerController>();
            if (worker != null)
                Speak(worker.transform.position, Pick(WorkerLines), Warm);
        }

        void Speak(Vector3 at, string line, Color tint)
        {
            UIManager.Instance.SpawnFloatingText(at + Vector3.up * headHeight, line, tint);
        }

        static string Pick(string[] lines) => lines[Random.Range(0, lines.Length)];
    }
}
