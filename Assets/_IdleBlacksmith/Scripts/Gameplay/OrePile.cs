using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>Ore stockpile. Rocks jiggle when picked and drain as the stock runs down.</summary>
    public class OrePile : MonoBehaviour
    {
        [Tooltip("One approach point per worker lane")]
        public Transform[] pickupPoints;
        [Tooltip("Rock meshes that jiggle when ore is taken")]
        public Transform[] rockVisuals;

        void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.resources == null) return;
            gm.resources.OnOreChanged += HandleOreChanged;
            HandleOreChanged(gm.resources.Ore, gm.resources.OreCapacity);
        }

        void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.resources != null)
                gm.resources.OnOreChanged -= HandleOreChanged;
        }

        /// <summary>Shows one rock per slice of the stock so the pile visibly empties.</summary>
        void HandleOreChanged(int ore, int capacity)
        {
            if (rockVisuals == null || rockVisuals.Length == 0) return;
            float fill = capacity > 0 ? Mathf.Clamp01(ore / (float)capacity) : 1f;
            int visible = ore <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(rockVisuals.Length * fill), 1, rockVisuals.Length);
            for (int i = 0; i < rockVisuals.Length; i++)
                if (rockVisuals[i] != null && rockVisuals[i].gameObject.activeSelf != (i < visible))
                    rockVisuals[i].gameObject.SetActive(i < visible);
        }

        public Vector3 GetPickupPoint(int lane)
        {
            if (pickupPoints == null || pickupPoints.Length == 0) return transform.position + Vector3.forward;
            return pickupPoints[Mathf.Clamp(lane, 0, pickupPoints.Length - 1)].position;
        }

        public GameObject TakeOre(Transform hand)
        {
            if (rockVisuals != null && rockVisuals.Length > 0)
            {
                Transform rock = rockVisuals[Random.Range(0, rockVisuals.Length)];
                if (rock != null) Tween.PunchScale(rock, Vector3.one * 0.3f, 0.4f);
            }
            GameObject prefab = GameManager.Instance != null ? GameManager.Instance.config.oreChunkPrefab : null;
            if (prefab == null || hand == null) return null;
            GameObject chunk = Instantiate(prefab, hand);
            chunk.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
            chunk.transform.localRotation = Quaternion.identity;
            return chunk;
        }
    }
}
