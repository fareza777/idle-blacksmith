using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Night fireflies — a loose swarm of warm motes drifting over the village after
    /// dark. Runtime-spawned (GameManager.SpawnAmbientNpcs); they swell in as
    /// DayCycle.Night climbs and shrink away at dawn.
    /// </summary>
    public class FireflyDrift : MonoBehaviour
    {
        [Tooltip("Number of motes in the swarm")]
        public int count = 14;
        [Tooltip("Drift spread around this transform")]
        public float radius = 7.5f;

        Transform[] moteT;
        Vector3[] moteHome;
        float[] moteSeed;
        float[] moteSpeed;

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            int n = Mathf.Max(1, count);
            moteT = new Transform[n];
            moteHome = new Vector3[n];
            moteSeed = new float[n];
            moteSpeed = new float[n];
            for (int i = 0; i < n; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.zero;
                var r = go.GetComponent<MeshRenderer>();
                if (r != null && sh != null)
                {
                    var m = new Material(sh);
                    m.SetColor("_BaseColor", new Color(0.85f, 1f, 0.42f));
                    r.material = m;
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                moteT[i] = go.transform;
                moteHome[i] = new Vector3(Random.Range(-radius, radius),
                                          Random.Range(0.5f, 2.3f),
                                          Random.Range(-radius, radius));
                moteSeed[i] = Random.value * 100f;
                moteSpeed[i] = Random.Range(0.35f, 0.7f);
            }
        }

        void Update()
        {
            float vis = SettingsPanel.ReduceFX ? 0f : Mathf.InverseLerp(0.35f, 0.75f, DayCycle.Night);
            if (moteT == null) return;
            if (vis <= 0.001f)
            {
                for (int i = 0; i < moteT.Length; i++)
                    moteT[i].localScale = Vector3.zero;
                return;
            }

            for (int i = 0; i < moteT.Length; i++)
            {
                float t = Time.time * moteSpeed[i] + moteSeed[i];
                moteT[i].position = transform.position + moteHome[i] + new Vector3(
                    Mathf.Sin(t) * 0.9f + Mathf.Sin(t * 0.37f) * 0.6f,
                    Mathf.Sin(t * 0.9f + 1.7f) * 0.45f,
                    Mathf.Cos(t * 0.8f) * 0.9f + Mathf.Cos(t * 0.31f) * 0.6f);
                float pulse = 0.55f + 0.45f * Mathf.Sin(t * 2.3f);
                moteT[i].localScale = Vector3.one * ((0.05f + 0.05f * pulse) * vis);
            }
        }
    }
}
