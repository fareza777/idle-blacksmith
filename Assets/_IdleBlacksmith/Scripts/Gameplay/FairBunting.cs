using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Festival bunting over the smithy forecourt: two short posts strung with a drooping
    /// line of colored pennants, up only while a market fair day is on. Runtime-built with
    /// plain fields and parallel arrays — no nested types (they corrupt level0).
    /// </summary>
    public class FairBunting : MonoBehaviour
    {
        [Tooltip("Pennants per string")]
        public int pennants = 12;
        [Tooltip("How far the string sags below the pole tops")]
        public float sag = 0.65f;
        [Tooltip("Pole tops above ground")]
        public float poleHeight = 2.1f;
        [Tooltip("String endpoints in local space — across the smithy forecourt")]
        public Vector3 leftPole = new Vector3(-2.9f, 0f, -0.55f);
        public Vector3 rightPole = new Vector3(2.9f, 0f, -0.55f);

        static readonly Color[] FlagColors =
        {
            new Color(0.95f, 0.45f, 0.25f),
            new Color(0.95f, 0.78f, 0.30f),
            new Color(0.40f, 0.72f, 0.60f),
            new Color(0.85f, 0.55f, 0.75f),
        };

        Transform[] flags;
        float[] flagSeed;
        Material poleMat, stringMat;
        Material[] flagMats;
        Transform root;

        /// <summary>Editor preview hook: build the bunting and force it on for screenshots.</summary>
        public void PreviewBuild()
        {
            Start();
            if (root != null) root.gameObject.SetActive(true);
        }

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            poleMat = new Material(sh);
            poleMat.SetColor("_BaseColor", new Color(0.38f, 0.26f, 0.16f));
            stringMat = new Material(sh);
            stringMat.SetColor("_BaseColor", new Color(0.30f, 0.22f, 0.15f));
            flagMats = new Material[FlagColors.Length];
            for (int i = 0; i < FlagColors.Length; i++)
            {
                flagMats[i] = new Material(sh);
                flagMats[i].SetColor("_BaseColor", FlagColors[i]);
            }

            root = new GameObject("BuntingRoot").transform;
            root.SetParent(transform, false);

            Vector3 la = leftPole + Vector3.up * poleHeight;
            Vector3 ra = rightPole + Vector3.up * poleHeight;

            // Two slim posts.
            Post(leftPole);
            Post(rightPole);

            // The rope: small stretched cubes along the catenary so the line reads curved.
            int ropeSegs = 26;
            for (int i = 0; i < ropeSegs; i++)
            {
                float t0 = i / (float)ropeSegs;
                float t1 = (i + 1) / (float)ropeSegs;
                Vector3 a = Catenary(la, ra, t0);
                Vector3 b = Catenary(la, ra, t1);
                var seg = Primitives.Create(PrimitiveType.Cube);
                seg.transform.SetParent(root, false);
                seg.transform.position = (a + b) * 0.5f;
                seg.transform.rotation = Quaternion.LookRotation(b - a);
                seg.transform.localScale = new Vector3(0.02f, 0.02f, (b - a).magnitude + 0.02f);
                var r = seg.GetComponent<MeshRenderer>();
                r.material = stringMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            // Pennants: flattened cubes under the rope that flutter on their own rhythm.
            flags = new Transform[pennants];
            flagSeed = new float[pennants];
            for (int i = 0; i < pennants; i++)
            {
                float t = (i + 0.5f) / pennants;
                Vector3 p = Catenary(la, ra, t);
                var f = Primitives.Create(PrimitiveType.Cube);
                f.transform.SetParent(root, false);
                f.transform.position = p + Vector3.down * 0.09f;
                f.transform.localScale = new Vector3(0.14f, 0.17f, 0.02f);
                var r = f.GetComponent<MeshRenderer>();
                r.material = flagMats[i % flagMats.Length];
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                flagSeed[i] = Random.value * 10f;
                flags[i] = f.transform;
            }

            root.gameObject.SetActive(false);
        }

        void Post(Vector3 basePos)
        {
            var p = Primitives.Create(PrimitiveType.Cylinder);
            p.transform.SetParent(root, false);
            p.transform.position = basePos + Vector3.up * (poleHeight * 0.5f);
            p.transform.localScale = new Vector3(0.10f, poleHeight * 0.5f, 0.10f);
            var r = p.GetComponent<MeshRenderer>();
            r.material = poleMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Little cap so the rope visibly ties off.
            var cap = Primitives.Create(PrimitiveType.Sphere);
            cap.transform.SetParent(root, false);
            cap.transform.position = basePos + Vector3.up * poleHeight;
            cap.transform.localScale = Vector3.one * 0.12f;
            var rc = cap.GetComponent<MeshRenderer>();
            rc.material = poleMat;
            rc.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        Vector3 Catenary(Vector3 a, Vector3 b, float t)
        {
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y -= sag * Mathf.Sin(Mathf.PI * t);
            return p;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool on = gm != null && gm.marketFair != null && gm.marketFair.Active
                      && !SettingsPanel.ReduceFX;
            if (root != null && root.gameObject.activeSelf != on)
                root.gameObject.SetActive(on);
            if (!on || flags == null) return;

            float now = Time.time;
            for (int i = 0; i < pennants; i++)
            {
                if (flags[i] == null) continue;
                // Gentle sway — each flag bobs and twists on its own phase.
                float s = Mathf.Sin(now * 1.6f + flagSeed[i]);
                float c = Mathf.Cos(now * 1.1f + flagSeed[i] * 1.7f);
                flags[i].localRotation = Quaternion.Euler(0f, c * 14f, s * 10f);
            }
        }
    }
}
