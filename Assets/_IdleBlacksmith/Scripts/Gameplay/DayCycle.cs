using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Slow ambient day cycle attached to the directional sun. Sun color/intensity,
    /// flat ambient light, and the camera's solid background all drift through
    /// dawn → noon → dusk → night over one long loop, so the village feels alive
    /// without touching gameplay. The forge's emissive parts pop at night.
    /// </summary>
    public class DayCycle : MonoBehaviour
    {
        [Tooltip("Seconds for a full day-night loop.")]
        public float cycleSeconds = 420f;

        [Tooltip("Starting point in the cycle, 0..1 (0.25 = noon).")]
        [Range(0f, 1f)] public float startPhase = 0.12f;

        struct Key
        {
            public Color sun;
            public float intensity;
            public Color ambient;
            public Color sky;
            public Key(Color s, float i, Color a, Color k) { sun = s; intensity = i; ambient = a; sky = k; }
        }

        // dawn, noon, dusk, night — loops back to dawn
        static readonly Key[] keys =
        {
            new Key(new Color(1.00f, 0.76f, 0.55f), 1.05f, new Color(0.62f, 0.56f, 0.50f), new Color(0.88f, 0.74f, 0.62f)),
            new Key(new Color(1.00f, 0.92f, 0.80f), 1.40f, new Color(0.74f, 0.67f, 0.56f), new Color(0.94f, 0.88f, 0.74f)),
            new Key(new Color(1.00f, 0.58f, 0.38f), 0.95f, new Color(0.54f, 0.42f, 0.42f), new Color(0.72f, 0.52f, 0.52f)),
            new Key(new Color(0.52f, 0.62f, 0.95f), 0.50f, new Color(0.30f, 0.34f, 0.50f), new Color(0.14f, 0.19f, 0.36f)),
            new Key(new Color(1.00f, 0.76f, 0.55f), 1.05f, new Color(0.62f, 0.56f, 0.50f), new Color(0.88f, 0.74f, 0.62f)),
        };

        Light sun;
        Camera cam;
        float phase;

        /// <summary>0 in daylight, 1 at deep night — drives window/lamp glow scaling.</summary>
        public static float Night { get; private set; }

        /// <summary>Position in the 0..1 day loop (0 dawn, .25 noon, .5 dusk, .75..1 night).</summary>
        public static float Phase { get; private set; }

        void Start()
        {
            sun = GetComponent<Light>();
            cam = Camera.main;
            phase = startPhase;
            Apply(phase);
        }

        bool nightAmb;

        void Update()
        {
            float prev = phase;
            phase = Mathf.Repeat(phase + Time.deltaTime / cycleSeconds, 1f);
            if (phase < prev)
            {
                // A dawn has come and gone — count it for the Day chip and quests.
                var d = GameManager.Instance != null ? GameManager.Instance.Data : null;
                if (d != null && d.stats != null) d.stats.dayCycles++;
            }
            Apply(phase);

            // Ambience bed follows the sun — forge crackle by day, crickets and the
            // village owl once the dark settles in.
            bool wantNight = Night > 0.55f;
            if (wantNight != nightAmb)
            {
                nightAmb = wantNight;
                AudioManager.PlayAmbience(wantNight ? "amb_night" : "amb_fire", 4f);
            }
        }

        void Apply(float t)
        {
            Phase = t;
            float span = 1f / (keys.Length - 1);
            int i = Mathf.Min(keys.Length - 2, Mathf.FloorToInt(t / span));
            float u = Mathf.SmoothStep(0f, 1f, (t - i * span) / span);
            Key a = keys[i], b = keys[i + 1];

            if (sun != null)
            {
                sun.color = Color.Lerp(a.sun, b.sun, u);
                sun.intensity = Mathf.Lerp(a.intensity, b.intensity, u);
            }
            RenderSettings.ambientLight = Color.Lerp(a.ambient, b.ambient, u);
            if (cam != null) cam.backgroundColor = Color.Lerp(a.sky, b.sky, u);

            float lum = RenderSettings.ambientLight.r * 0.3f + RenderSettings.ambientLight.g * 0.6f
                        + RenderSettings.ambientLight.b * 0.1f;
            Night = 1f - Mathf.InverseLerp(0.37f, 0.68f, lum);
        }
    }
}
