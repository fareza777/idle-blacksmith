using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// A scatter of stars high above the village that fades in with the night. Runtime-spawned
    /// and plain-field only, same pattern as FireflyDrift — nested types in a MonoBehaviour
    /// corrupt level0 on device, so everything here is flat fields and parallel arrays.
    /// </summary>
    public class StarField : MonoBehaviour
    {
        public int count = 40;
        [Tooltip("Stars are scattered on a disc this wide, centred on the village")]
        public float radius = 16f;
        [Tooltip("Height band above the rooftops")]
        public float minY = 9f;
        public float maxY = 16f;

        Transform[] starT;
        float[] starSeed;
        float[] starSize;
        Material starMat;

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            starMat = new Material(sh);
            starMat.SetColor("_BaseColor", new Color(0.92f, 0.95f, 1f));

            starT = new Transform[count];
            starSeed = new float[count];
            starSize = new float[count];
            for (int i = 0; i < count; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(transform, false);
                var r = c.GetComponent<MeshRenderer>();
                r.material = starMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                // Random point on a high disc, biased to the village centre.
                float a = Random.value * Mathf.PI * 2f;
                float d = Mathf.Sqrt(Random.value) * radius;
                c.transform.position = new Vector3(Mathf.Cos(a) * d, Random.Range(minY, maxY), Mathf.Sin(a) * d + 1f);
                starSeed[i] = Random.value * 6.283f;
                starSize[i] = 0.035f + Random.value * 0.05f;
                starT[i] = c.transform;
                starT[i].localScale = Vector3.zero;
            }
        }

        void Update()
        {
            // Stars need true dark before they earn their keep.
            float vis = Mathf.InverseLerp(0.55f, 0.95f, DayCycle.Night);
            if (vis <= 0.001f)
            {
                if (starT != null && starT[0] != null && starT[0].localScale.x != 0f)
                    for (int i = 0; i < starT.Length; i++)
                        if (starT[i] != null) starT[i].localScale = Vector3.zero;
                return;
            }

            float t = Time.time;
            for (int i = 0; i < starT.Length; i++)
            {
                if (starT[i] == null) continue;
                // Each star twinkles on its own clock; slow enough to read as sky, not sparks.
                float tw = 0.7f + 0.3f * Mathf.Sin(t * (0.8f + i * 0.13f) + starSeed[i]);
                starT[i].localScale = Vector3.one * (starSize[i] * tw * vis);
            }
        }
    }
}
