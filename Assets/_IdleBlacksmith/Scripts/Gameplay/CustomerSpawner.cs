using System.Collections.Generic;
using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// One customer at a time. Spawns only when the rack has stock, on a randomized
    /// interval, and routes them through the door waypoints.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [Tooltip("Path from outside to the rack (last point is added automatically: rack customer point)")]
        public Transform[] enterWaypoints;
        [Tooltip("Path back out after the purchase")]
        public Transform[] exitWaypoints;
        public Transform spawnPoint;
        [Tooltip("Chance a shopper is a VIP paying five times the price")]
        [Range(0f, 0.4f)] public float vipChance = 0.08f;
        [Tooltip("Price multiplier a VIP pays")]
        public float vipPriceMult = 5f;

        CustomerController current;
        float timer = 2.5f;

        /// <summary>The shopper currently in the shop, if any — read by the chatter system.</summary>
        public CustomerController Current => current;

        void Update()
        {
            if (current != null || GameManager.Instance == null) return;
            SwordRack rack = GameManager.Instance.rack;
            GameConfig config = GameManager.Instance.config;
            if (rack == null || config == null) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            // The Trading Post and Shop Charm upgrade both shorten the gap between customers.
            float pace = Production.CustomerIntervalMult;
            if (GameManager.Instance.upgrades != null)
                pace *= GameManager.Instance.upgrades.CustomerIntervalMult;
            if (GameManager.Instance.rush != null)
                pace *= GameManager.Instance.rush.PaceMult;
            if (GameManager.Instance.marketFair != null)
                pace *= GameManager.Instance.marketFair.PaceMult;
            timer = Random.Range(config.minCustomerInterval, config.maxCustomerInterval) * Mathf.Max(0.15f, pace);

            if (rack.Stock <= 0) return;
            Spawn(rack, config);
        }

        void Spawn(SwordRack rack, GameConfig config)
        {
            // Rare golden visitor: a distinct noble when the model exists, else a gilded regular.
            bool vip = Random.value < vipChance;
            GameObject prefab;
            if (vip && config.customerPrefabC != null)
            {
                prefab = config.customerPrefabC;
            }
            else
            {
                // Three regular villagers: the bonneted peddler joins the farmer and the seamstress.
                float roll = Random.value;
                prefab = roll < 0.36f ? config.customerPrefabA
                     : roll < 0.72f ? config.customerPrefabB
                     : (config.customerPrefabD != null ? config.customerPrefabD : config.customerPrefabA);
            }
            if (prefab == null || spawnPoint == null) return;

            GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);
            current = go.GetComponent<CustomerController>();
            if (current == null) return;

            if (vip && config.customerPrefabC == null)
            {
                var block = new MaterialPropertyBlock();
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                {
                    block.SetColor("_BaseColor", new Color(1f, 0.80f, 0.32f));
                    block.SetColor("_Color", new Color(1f, 0.80f, 0.32f));
                    r.SetPropertyBlock(block);
                }
            }

            var enter = new List<Vector3>();
            if (enterWaypoints != null)
                foreach (Transform t in enterWaypoints)
                    if (t != null) enter.Add(t.position);
            // No explicit path authored: fall back to walking straight at the rack.
            if (enter.Count == 0)
                enter.Add(rack.customerPoint != null
                    ? rack.customerPoint.position
                    : rack.transform.position + Vector3.forward);

            var leave = new List<Vector3>();
            if (exitWaypoints != null)
                foreach (Transform t in exitWaypoints)
                    if (t != null) leave.Add(t.position);
            leave.Add(spawnPoint.position);

            current.Init(rack, enter.ToArray(), leave.ToArray(), _ => current = null,
                vip ? vipPriceMult : 1f, vip);
        }
    }
}
