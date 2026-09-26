using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Makes a building feel alive: parts bob, the local light breathes, and particle bursts
    /// fire on a timer. Used by the mine, market, gate and sanctum prefabs.
    /// </summary>
    public class BuildingFlourish : MonoBehaviour
    {
        [Tooltip("Parts that drift up and down (floating crystals, lanterns)")]
        public Transform[] floaters;
        public float floatAmplitude = 0.10f;
        public float floatSpeed = 1.3f;

        [Tooltip("One-shot particle systems fired every burstInterval seconds")]
        public ParticleSystem[] bursts;
        public float burstInterval = 5f;

        public Light glow;
        public float glowBase = 1.2f;
        public float glowSwing = 0.2f;
        public float glowSpeed = 1.1f;

        Vector3[] restPositions;
        float burstTimer;
        float phase;
        float interval;

        void Awake()
        {
            if (floaters != null)
            {
                restPositions = new Vector3[floaters.Length];
                for (int i = 0; i < floaters.Length; i++)
                    if (floaters[i] != null) restPositions[i] = floaters[i].localPosition;
            }

            phase = Random.value * 6.283f;
            if (glow != null) glowBase = glow.intensity;
            interval = burstInterval > 0.1f ? burstInterval : 5f;
            burstTimer = Random.Range(0.4f, interval);
        }

        void Update()
        {
            float t = Time.time;

            if (floaters != null && restPositions != null)
                for (int i = 0; i < floaters.Length; i++)
                {
                    if (floaters[i] == null) continue;
                    float offset = Mathf.Sin(t * floatSpeed + phase + i * 0.8f) * floatAmplitude;
                    floaters[i].localPosition = restPositions[i] + new Vector3(0f, offset, 0f);
                }

            if (glow != null)
            {
                // Lamps breathe; at night they burn brighter so the village reads as lit.
                float nightBoost = Mathf.Lerp(0.55f, 2.0f, DayCycle.Night);
                glow.intensity = (glowBase + Mathf.Sin(t * glowSpeed + phase) * glowSwing) * nightBoost;
            }

            if (bursts == null || bursts.Length == 0) return;
            burstTimer -= Time.deltaTime;
            if (burstTimer > 0f) return;
            burstTimer = interval;
            foreach (ParticleSystem ps in bursts)
                if (ps != null) ps.Play();
        }
    }
}
