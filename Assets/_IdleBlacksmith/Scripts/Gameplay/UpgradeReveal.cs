using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Celebrates a smithy upgrade: a ring of golden embers rises around the shop while the new
    /// tier model swaps in, so the building reads as having transformed rather than popped.
    /// Spawned at runtime by GameManager — never serialized on a scene object.
    /// </summary>
    public class UpgradeReveal : MonoBehaviour
    {
        const int Count = 26;
        const float Duration = 2.2f;

        static Material emberMat;
        static Material sparkMat;

        Transform[] embers;
        Vector3[] origins;
        float[] sizes;
        float[] speeds;
        float[] spins;
        float t;
        float radius;
        float riseTop;

        public void Begin(float radius, float top)
        {
            this.radius = radius;
            riseTop = top;
            if (emberMat == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (sh != null)
                {
                    emberMat = new Material(sh);
                    emberMat.color = new Color(1f, 0.78f, 0.30f);
                    sparkMat = new Material(sh);
                    sparkMat.color = new Color(1f, 0.92f, 0.62f);
                }
            }
            embers = new Transform[Count];
            origins = new Vector3[Count];
            sizes = new float[Count];
            speeds = new float[Count];
            spins = new float[Count];
            var rng = new System.Random();
            for (int i = 0; i < Count; i++)
            {
                float angle = (float)i / Count * 6.283f + (float)rng.NextDouble() * 0.4f;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "RevealEmber" + i;
                go.transform.SetParent(transform, false);
                origins[i] = new Vector3(Mathf.Cos(angle) * radius, 0.15f, Mathf.Sin(angle) * radius * 0.8f);
                go.transform.localPosition = origins[i];
                go.transform.localRotation = Quaternion.Euler(45f, angle * 57.3f, 45f);
                float s = 0.10f + (float)rng.NextDouble() * 0.08f;
                go.transform.localScale = new Vector3(s, s, s);
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = i % 3 == 0 && sparkMat != null ? sparkMat : emberMat;
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                sizes[i] = s;
                speeds[i] = 0.8f + (float)rng.NextDouble() * 0.9f;
                spins[i] = 90f + (float)rng.NextDouble() * 160f;
                embers[i] = go.transform;
            }
        }

        void Update()
        {
            if (embers == null) { Destroy(gameObject); return; }
            t += Time.deltaTime;
            float fade = 1f;
            if (t > Duration * 0.55f) fade = Mathf.Clamp01(1f - (t - Duration * 0.55f) / (Duration * 0.45f));
            for (int i = 0; i < embers.Length; i++)
            {
                if (embers[i] == null) continue;
                float k = Mathf.Min(1f, t * speeds[i] * 0.45f);
                float y = origins[i].y + k * riseTop;
                embers[i].localPosition = new Vector3(origins[i].x, y, origins[i].z);
                embers[i].localRotation = Quaternion.Euler(45f + spins[i] * t, spins[i] * t * 0.6f, 45f);
                float s = sizes[i] * fade;
                embers[i].localScale = new Vector3(s, s, s);
            }
            if (t >= Duration) Destroy(gameObject);
        }
    }
}
