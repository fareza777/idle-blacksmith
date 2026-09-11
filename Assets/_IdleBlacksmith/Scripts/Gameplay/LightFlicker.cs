using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>Warm perlin flicker for the forge light.</summary>
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 1.8f;
        public float amplitude = 0.45f;
        public float speed = 7f;

        Light targetLight;
        float seed;

        void Awake()
        {
            targetLight = GetComponent<Light>();
            seed = Random.Range(0f, 100f);
        }

        void Update()
        {
            if (targetLight == null) return;
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            targetLight.intensity = baseIntensity + (n - 0.5f) * 2f * amplitude;
        }
    }
}
