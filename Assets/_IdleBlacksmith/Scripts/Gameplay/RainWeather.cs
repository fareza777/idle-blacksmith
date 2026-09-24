using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Occasional rain showers over the village: a burst of stretched unlit quads that fall
    /// and recycle, easing in and out over a short window. Runtime-spawned with plain fields
    /// and parallel arrays only — no particle modules, no nested types (both corrupt level0).
    /// </summary>
    public class RainWeather : MonoBehaviour
    {
        public int streaks = 48;
        public Vector2 area = new Vector2(13f, 9f);
        public float fallSpeed = 9f;
        public float topY = 11f;
        [Tooltip("Seconds between shower rolls; each shower lasts a fraction of the gap")]
        public Vector2 gap = new Vector2(80f, 160f);
        public Vector2 showerLength = new Vector2(20f, 35f);

        Transform[] drops;
        float[] dropSeed;
        Material rainMat;
        AudioSource rainSrc;
        float rainClipGain = 1f;
        float nextShower;
        float showerEnd;
        float intensity; // 0..1 ease

        /// <summary>True once a shower is meaningfully underway — lets chatter react to weather.</summary>
        public bool IsRaining => intensity > 0.5f;

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            rainMat = new Material(sh);
            rainMat.SetColor("_BaseColor", new Color(0.62f, 0.7f, 0.85f));

            drops = new Transform[streaks];
            dropSeed = new float[streaks];
            for (int i = 0; i < streaks; i++)
            {
                var c = Primitives.Create(PrimitiveType.Cube);
                c.transform.SetParent(transform, false);
                var r = c.GetComponent<MeshRenderer>();
                r.material = rainMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                c.transform.localScale = new Vector3(0.018f, 0.34f, 0.018f);
                c.transform.rotation = Quaternion.Euler(0f, 0f, 9f); // light slant
                dropSeed[i] = Random.value;
                drops[i] = c.transform;
                drops[i].localScale = Vector3.zero;
                ResetDrop(i, true);
            }
            nextShower = Time.time + Random.Range(gap.x, gap.y) * 0.4f; // first one comes early
            showerEnd = 0f;

            // Own channel for the patter — the day/night bed keeps its source to itself.
            if (AudioManager.Instance != null && AudioManager.Instance.clips != null)
            {
                foreach (var nc in AudioManager.Instance.clips)
                {
                    if (nc == null || nc.id != "amb_rain" || nc.clip == null) continue;
                    rainSrc = gameObject.AddComponent<AudioSource>();
                    rainSrc.clip = nc.clip;
                    rainSrc.loop = true;
                    rainSrc.playOnAwake = false;
                    rainSrc.spatialBlend = 0f;
                    rainSrc.volume = 0f;
                    rainClipGain = nc.volume;
                    rainSrc.Play();
                    break;
                }
            }
        }

        void ResetDrop(int i, bool randomY)
        {
            float x = Random.Range(-area.x, area.x);
            float z = Random.Range(-area.y, area.y) + 1f;
            float y = randomY ? Random.Range(0.5f, topY) : topY;
            drops[i].position = new Vector3(x, y, z);
        }

        void Update()
        {
            float now = Time.time;
            if (showerEnd <= now && now >= nextShower)
                showerEnd = now + Random.Range(showerLength.x, showerLength.y);

            // Ease the shower in and out so it never pops. Lite mode skips the shower
            // entirely — the drop pool just stays empty.
            float target = (showerEnd > now && !SettingsPanel.ReduceFX) ? 1f : 0f;
            intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime / 3f);
            if (rainSrc != null)
                rainSrc.volume = intensity * rainClipGain;
            if (target == 0f && intensity <= 0.001f)
            {
                nextShower = now + Random.Range(gap.x, gap.y);
                if (drops[0].localScale.x != 0f)
                    for (int i = 0; i < streaks; i++)
                        drops[i].localScale = Vector3.zero;
                return;
            }

            float fade = Mathf.Min(1f, intensity * 1.4f);
            for (int i = 0; i < streaks; i++)
            {
                if (drops[i] == null) continue;
                Vector3 p = drops[i].position;
                p.y -= fallSpeed * Time.deltaTime * (0.85f + dropSeed[i] * 0.3f);
                if (p.y < 0.1f) { ResetDrop(i, false); p = drops[i].position; }
                drops[i].position = p;
                drops[i].localScale = new Vector3(0.018f, 0.34f, 0.018f) * fade;
            }
        }
    }
}
