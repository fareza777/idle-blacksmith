using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// A pale moon that climbs the sky through the night half of the day cycle and slips away
    /// at dawn. Reads DayCycle.Phase so it stays glued to the real day clock even after pause
    /// or frame hitches. Runtime-spawned, plain fields only — same rules as StarField.
    /// </summary>
    public class MoonDrift : MonoBehaviour
    {
        [Tooltip("Horizontal distance the moon travels from rise to set")]
        public float arcWidth = 26f;
        public float peakHeight = 15f;
        public float zDepth = 12f;

        Transform moon;
        Transform halo;
        Material moonMat;
        Material haloMat;

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            moonMat = new Material(sh);
            moonMat.SetColor("_BaseColor", new Color(0.88f, 0.92f, 1f));
            haloMat = new Material(sh);
            haloMat.SetColor("_BaseColor", new Color(0.55f, 0.62f, 0.9f, 0.35f));

            var h = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(h.GetComponent<Collider>());
            h.transform.SetParent(transform, false);
            h.GetComponent<MeshRenderer>().material = haloMat;
            h.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halo = h.transform;

            var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(m.GetComponent<Collider>());
            m.transform.SetParent(transform, false);
            m.GetComponent<MeshRenderer>().material = moonMat;
            m.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            moon = m.transform;

            halo.localScale = Vector3.zero;
            moon.localScale = Vector3.zero;
        }

        void Update()
        {
            // Night occupies the last quarter of the loop; map 0.75..1 onto a rise→set arc.
            float u = Mathf.InverseLerp(0.75f, 1.005f, DayCycle.Phase);
            float vis = Mathf.InverseLerp(0.25f, 0.75f, DayCycle.Night);
            if (u <= 0f || vis <= 0.001f)
            {
                if (halo.localScale.x != 0f)
                {
                    halo.localScale = Vector3.zero;
                    moon.localScale = Vector3.zero;
                }
                return;
            }

            // Parabola: low at rise and set, high at midnight.
            float x = Mathf.Lerp(-arcWidth, arcWidth, u);
            float y = peakHeight * (0.35f + 0.65f * Mathf.Sin(u * Mathf.PI));
            halo.position = new Vector3(x, y, zDepth);
            // The moon sits a step closer to the camera than its glow blob so the
            // opaque halo reads as a ring of light around it, not a wall in front.
            moon.position = new Vector3(x, y, zDepth - 1.4f);
            halo.localScale = Vector3.one * (2.1f * vis);
            moon.localScale = Vector3.one * (1.15f * vis);
        }
    }
}
