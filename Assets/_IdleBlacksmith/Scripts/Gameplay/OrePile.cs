using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>Infinite ore source. Rocks bounce a little when picked.</summary>
    public class OrePile : MonoBehaviour
    {
        [Tooltip("One approach point per worker lane")]
        public Transform[] pickupPoints;
        [Tooltip("Rock meshes that jiggle when ore is taken")]
        public Transform[] rockVisuals;

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
