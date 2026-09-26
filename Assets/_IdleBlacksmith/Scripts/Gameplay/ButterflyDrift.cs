using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Daytime butterflies — a few bright wings fluttering over the front garden
    /// while the sun is up. Runtime-spawned; they fade in as DayCycle.Night falls
    /// toward day and hide at dusk, so they complement the fireflies.
    /// Plain fields + parallel arrays — no nested types (they corrupt level0).
    /// </summary>
    public class ButterflyDrift : MonoBehaviour
    {
        [Tooltip("Butterflies in the garden at once")]
        public int count = 3;
        [Tooltip("Garden center in world space")]
        public Vector3 center = new Vector3(0f, 0.8f, -3.4f);
        [Tooltip("Wander spread around the center")]
        public float radius = 3.4f;

        static readonly Color[] WingColors =
        {
            new Color(0.95f, 0.55f, 0.25f),
            new Color(0.55f, 0.75f, 0.95f),
            new Color(0.95f, 0.60f, 0.80f),
        };

        Transform[] bodyT;
        Transform[] wingL;
        Transform[] wingR;
        Vector3[] home;
        float[] seed;
        Vector3[] lastPos;

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) return;
            int n = Mathf.Max(1, count);
            bodyT = new Transform[n];
            wingL = new Transform[n];
            wingR = new Transform[n];
            home = new Vector3[n];
            seed = new float[n];
            lastPos = new Vector3[n];

            var bodyMat = new Material(sh);
            bodyMat.SetColor("_BaseColor", new Color(0.18f, 0.14f, 0.12f));

            for (int i = 0; i < n; i++)
            {
                var body = Primitives.Create(PrimitiveType.Cube);
                body.transform.SetParent(transform, false);
                body.transform.localScale = new Vector3(0.022f, 0.028f, 0.09f);
                var rb = body.GetComponent<MeshRenderer>();
                rb.material = bodyMat;
                rb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var wingMat = new Material(sh);
                wingMat.SetColor("_BaseColor", WingColors[i % WingColors.Length]);

                var wl = Primitives.Create(PrimitiveType.Cube);
                wl.transform.SetParent(body.transform, false);
                wl.transform.localPosition = new Vector3(-1.6f, 0.35f, 0f);
                wl.transform.localScale = new Vector3(2.6f, 0.3f, 0.85f);
                var rl = wl.GetComponent<MeshRenderer>();
                rl.material = wingMat;
                rl.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var wr = Primitives.Create(PrimitiveType.Cube);
                wr.transform.SetParent(body.transform, false);
                wr.transform.localPosition = new Vector3(1.6f, 0.35f, 0f);
                wr.transform.localScale = new Vector3(2.6f, 0.3f, 0.85f);
                var rr = wr.GetComponent<MeshRenderer>();
                rr.material = wingMat;
                rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                bodyT[i] = body.transform;
                wingL[i] = wl.transform;
                wingR[i] = wr.transform;
                home[i] = new Vector3(Random.Range(-radius, radius),
                                      Random.Range(-0.35f, 0.55f),
                                      Random.Range(-radius, radius));
                seed[i] = Random.value * 100f;
                lastPos[i] = center + home[i];
                body.transform.position = lastPos[i];
            }
        }

        void Update()
        {
            // 1 in daylight, 0 at night — butterflies sleep at dusk.
            float vis = SettingsPanel.ReduceFX ? 0f : 1f - Mathf.InverseLerp(0.1f, 0.4f, DayCycle.Night);
            if (bodyT == null) return;
            if (vis <= 0.001f)
            {
                for (int i = 0; i < bodyT.Length; i++)
                    if (bodyT[i] != null) bodyT[i].localScale = Vector3.zero;
                return;
            }

            float now = Time.time;
            for (int i = 0; i < bodyT.Length; i++)
            {
                if (bodyT[i] == null) continue;
                float t = now * 0.6f + seed[i];
                // Wandering flutter: layered sines so the path never looks mechanical.
                Vector3 pos = center + home[i] + new Vector3(
                    Mathf.Sin(t * 0.9f) * 0.9f + Mathf.Sin(t * 0.31f) * 0.7f,
                    Mathf.Sin(t * 1.4f + 1.2f) * 0.30f + Mathf.Sin(t * 5.2f) * 0.05f,
                    Mathf.Cos(t * 0.7f) * 0.9f + Mathf.Cos(t * 0.43f) * 0.6f);

                // Face the direction of drift.
                Vector3 delta = pos - lastPos[i];
                if (delta.sqrMagnitude > 0.000001f)
                    bodyT[i].rotation = Quaternion.Slerp(bodyT[i].rotation,
                        Quaternion.LookRotation(delta.normalized, Vector3.up), 0.12f);
                bodyT[i].position = pos;
                lastPos[i] = pos;

                float s = vis * (0.9f + 0.1f * Mathf.Sin(t * 3f));
                bodyT[i].localScale = new Vector3(0.022f, 0.028f, 0.09f) * s;

                // Wing flap: fast beat that eases at the top of a dip.
                float flap = Mathf.Abs(Mathf.Sin(now * 13f + seed[i] * 3f));
                float ang = Mathf.Lerp(15f, 75f, flap);
                if (wingL[i] != null) wingL[i].localRotation = Quaternion.Euler(0f, 0f, ang);
                if (wingR[i] != null) wingR[i].localRotation = Quaternion.Euler(0f, 0f, -ang);
            }
        }
    }
}
