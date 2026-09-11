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

        CustomerController current;
        float timer = 2.5f;

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
            timer = Random.Range(config.minCustomerInterval, config.maxCustomerInterval) * Mathf.Max(0.15f, pace);

            if (rack.Stock <= 0) return;
            Spawn(rack, config);
        }

        void Spawn(SwordRack rack, GameConfig config)
        {
            GameObject prefab = Random.value < 0.5f ? config.customerPrefabA : config.customerPrefabB;
            if (prefab == null || spawnPoint == null) return;

            GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);
            current = go.GetComponent<CustomerController>();
            if (current == null) return;

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

            current.Init(rack, enter.ToArray(), leave.ToArray(), _ => current = null);
        }
    }
}
