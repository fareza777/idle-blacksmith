using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Rune stones orbiting the Sanctum — one floating shard per rune level bought,
    /// so rune investment reads on the skyline instead of only inside a menu.
    /// Runtime-spawned; nothing here is scene-serialized.
    /// </summary>
    public class RuneOrbit : MonoBehaviour
    {
        [Tooltip("BuildingId that anchors the orbit ring")]
        public string requiresBuilding = BuildingId.Sanctum;
        [Tooltip("Shard cap — beyond this the ring just spins fuller")]
        public int maxShards = 8;
        [Tooltip("Orbit radius around the plot")]
        public float radius = 1.35f;

        Transform plot;
        readonly System.Collections.Generic.List<Transform> shards =
            new System.Collections.Generic.List<Transform>();
        Material shardMat;
        float spin;

        void Start()
        {
            var go = GameObject.Find("Plot_" + requiresBuilding);
            if (go != null) plot = go.transform;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null)
            {
                shardMat = new Material(sh);
                shardMat.SetColor("_BaseColor", new Color(0.45f, 0.95f, 0.85f));
            }
        }

        void Update()
        {
            if (plot == null) return;
            var gm = GameManager.Instance;
            int want = 0;
            if (gm != null && gm.buildings != null && gm.buildings.GetLevel(requiresBuilding) >= 1
                && gm.runes != null)
                want = Mathf.Min(maxShards, gm.runes.TotalLevels);

            while (shards.Count < want) shards.Add(MakeShard());
            for (int i = shards.Count - 1; i >= want; i--)
            {
                if (shards[i] != null) Destroy(shards[i].gameObject);
                shards.RemoveAt(i);
            }

            if (shards.Count == 0) return;
            spin += Time.deltaTime * 0.45f;
            for (int i = 0; i < shards.Count; i++)
            {
                float a = spin + i * (Mathf.PI * 2f / shards.Count);
                float bob = Mathf.Sin(Time.time * 1.6f + i * 1.9f) * 0.12f;
                shards[i].position = plot.position + new Vector3(
                    Mathf.Cos(a) * radius, 1.5f + bob, Mathf.Sin(a) * radius);
                shards[i].Rotate(0f, 60f * Time.deltaTime, 0f);
            }
        }

        Transform MakeShard()
        {
            var go = Primitives.Create(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(0.13f, 0.22f, 0.13f);
            go.transform.localRotation = Quaternion.Euler(35f, 30f, 20f);
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && shardMat != null)
            {
                r.material = shardMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go.transform;
        }
    }
}
