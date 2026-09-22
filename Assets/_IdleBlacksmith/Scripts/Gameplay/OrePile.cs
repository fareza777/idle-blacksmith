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
        [Tooltip("Ore granted by one manual tap on the pile")]
        public int tapOre = 1;
        [Tooltip("Minimum gap between manual taps")]
        public float tapCooldown = 0.25f;

        float nextTapAllowed;

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

        /// <summary>
        /// Player grabs a chunk straight off the pile — the manual half of the loop that
        /// complements tapping the anvil. Ignored while the stock is already capped.
        /// </summary>
        public void ManualMine()
        {
            GameManager gm = GameManager.Instance;
            var res = gm != null ? gm.resources : null;
            if (res == null || res.IsFull || Time.time < nextTapAllowed) return;
            nextTapAllowed = Time.time + tapCooldown;

            res.Add(tapOre);
            AudioManager.Play("mine_pick", 0.1f, 0.7f);
            UI.SettingsPanel.Buzz();
            Tween.PunchScale(transform, Vector3.one * 0.04f, 0.2f);
            if (rockVisuals != null && rockVisuals.Length > 0)
            {
                Transform rock = rockVisuals[Random.Range(0, rockVisuals.Length)];
                if (rock != null) Tween.PunchScale(rock, Vector3.one * 0.35f, 0.3f);
            }
            UI.UIManager.Instance?.SpawnFloatingText(
                transform.position + Vector3.up * 1.2f, "+" + tapOre + " ore",
                new Color(0.6f, 0.8f, 0.9f));
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
