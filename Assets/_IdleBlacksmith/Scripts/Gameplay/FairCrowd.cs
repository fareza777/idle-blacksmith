using System.Collections;
using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Fair-day crowd: a few villagers strolling between the market stalls while the
    /// fair runs. Reuses the customer prefabs minus their CustomerController so they
    /// keep the walk animation; spawn/despawn follows the fair's Active state.
    /// Plain fields and parallel arrays — no nested types (they corrupt level0).
    /// </summary>
    public class FairCrowd : MonoBehaviour
    {
        [Tooltip("Villagers wandering the fair grounds at once")]
        public int strollers = 3;
        public float strollSpeed = 1.15f;
        public float idleSecondsMin = 0.6f;
        public float idleSecondsMax = 1.9f;

        // Stroll loop over the forecourt: between the stalls, around the bunting posts.
        static readonly Vector3[] CourtWaypoints =
        {
            new Vector3(-3.2f, 0f, -0.7f), new Vector3(-2.0f, 0f, -1.1f),
            new Vector3(-0.6f, 0f, -0.6f), new Vector3(0.7f, 0f, -1.5f),
            new Vector3(2.0f, 0f, -1.1f), new Vector3(3.2f, 0f, -0.7f),
            new Vector3(2.3f, 0f, -2.3f), new Vector3(0f, 0f, -2.6f),
            new Vector3(-2.3f, 0f, -2.3f),
        };

        GameObject[] villagers;
        Coroutine[] routines;

        void Update()
        {
            var gm = GameManager.Instance;
            bool on = gm != null && gm.marketFair != null && gm.marketFair.Active
                      && !SettingsPanel.ReduceFX;
            if (on && villagers == null) Spawn(gm);
            else if (!on && villagers != null) Despawn();
        }

        void Spawn(GameManager gm)
        {
            var config = gm != null ? gm.config : null;
            if (config == null || config.customerPrefabA == null) return;

            var pool = new[] { config.customerPrefabA, config.customerPrefabB, config.customerPrefabD };
            villagers = new GameObject[strollers];
            routines = new Coroutine[strollers];
            for (int i = 0; i < strollers; i++)
            {
                var prefab = pool[Random.Range(0, pool.Length)];
                if (prefab == null) prefab = config.customerPrefabA;
                Vector3 start = CourtWaypoints[(i * 3 + Random.Range(0, 2)) % CourtWaypoints.Length];
                var go = Instantiate(prefab, start, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
                // Strip the buyer brain — strollers just wander; SimpleWalker + Animator stay.
                var buyer = go.GetComponent<CustomerController>();
                if (buyer != null) Destroy(buyer);
                var walker = go.GetComponent<SimpleWalker>();
                if (walker == null)
                {
                    Destroy(go);
                    continue;
                }
                villagers[i] = go;
                routines[i] = StartCoroutine(Stroll(walker, i * 3 + Random.Range(0, 3)));
            }
        }

        IEnumerator Stroll(SimpleWalker walker, int offset)
        {
            int idx = offset % CourtWaypoints.Length;
            while (true)
            {
                idx = (idx + Random.Range(1, 4)) % CourtWaypoints.Length;
                yield return walker.MoveTo(CourtWaypoints[idx], strollSpeed);
                // Linger at the stalls like a shopper would.
                yield return new WaitForSeconds(Random.Range(idleSecondsMin, idleSecondsMax));
            }
        }

        void Despawn()
        {
            if (routines != null)
                foreach (Coroutine c in routines)
                    if (c != null) StopCoroutine(c);
            routines = null;
            if (villagers != null)
                foreach (GameObject g in villagers)
                    if (g != null) Destroy(g);
            villagers = null;
        }

        void OnDisable()
        {
            Despawn();
        }
    }
}
