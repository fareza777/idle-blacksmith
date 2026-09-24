using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The sun itself: a warm gold disc with a soft amber halo that climbs the sky through
    /// the daylight stretch of the cycle and slips away at dusk. Mirror of MoonDrift —
    /// runtime-spawned, plain fields only, reads DayCycle.Phase.
    /// </summary>
    public class SunDrift : MonoBehaviour
    {
        [Tooltip("Horizontal distance the sun travels from rise to set")]
        public float arcWidth = 28f;
        public float peakHeight = 16f;
        public float zDepth = 12f;

        Transform sun;
        Transform halo;
        Material sunMat;
        Material haloMat;

        void Start()
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            sunMat = new Material(sh);
            sunMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.62f));
            haloMat = new Material(sh);
            haloMat.SetColor("_BaseColor", new Color(1f, 0.78f, 0.45f, 0.3f));

            var h = Primitives.Create(PrimitiveType.Sphere);
            h.transform.SetParent(transform, false);
            h.GetComponent<MeshRenderer>().material = haloMat;
            h.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            halo = h.transform;

            var m = Primitives.Create(PrimitiveType.Sphere);
            m.transform.SetParent(transform, false);
            m.GetComponent<MeshRenderer>().material = sunMat;
            m.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sun = m.transform;

            halo.localScale = Vector3.zero;
            sun.localScale = Vector3.zero;
        }

        void Update()
        {
            // Daylight is roughly the first three quarters of the loop; dusk dims the disc
            // ahead of the moon taking over.
            float u = Mathf.InverseLerp(0f, 0.72f, DayCycle.Phase);
            float vis = SettingsPanel.ReduceFX ? 0f : 1f - Mathf.InverseLerp(0.05f, 0.45f, DayCycle.Night);
            // Fade the disc in over the first stretch of dawn so it doesn't pop at phase 0.
            vis *= Mathf.InverseLerp(0.005f, 0.06f, DayCycle.Phase);
            if (u <= 0f || vis <= 0.001f)
            {
                if (halo.localScale.x != 0f)
                {
                    halo.localScale = Vector3.zero;
                    sun.localScale = Vector3.zero;
                }
                return;
            }

            float x = Mathf.Lerp(-arcWidth, arcWidth, u);
            float y = peakHeight * (0.3f + 0.7f * Mathf.Sin(u * Mathf.PI));
            halo.position = new Vector3(x, y, zDepth);
            sun.position = new Vector3(x, y, zDepth - 1.4f);
            halo.localScale = Vector3.one * (3.4f * vis);
            sun.localScale = Vector3.one * (1.5f * vis);
        }
    }
}
