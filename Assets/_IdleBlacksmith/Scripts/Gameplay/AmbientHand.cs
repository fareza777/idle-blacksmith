using System.Collections;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// A named villager who mans a building once it stands — the market vendor patrolling
    /// her stall, the hooded mystic watching the sanctum gate. Hidden until the required
    /// building reaches level 1. Spawned at runtime only (see GameManager.SpawnAmbientNpcs).
    /// </summary>
    public class AmbientHand : MonoBehaviour
    {
        [Tooltip("Villager prefab instantiated at runtime")]
        public GameObject characterPrefab;
        [Tooltip("BuildingId the hand is bound to — appears when that building is built")]
        public string requiresBuilding;
        [Tooltip("Side-to-side patrol range in front of the plot (0 = stationary)")]
        public float patrolOffset = 1.4f;
        public float moveSpeed = 1.0f;
        public float idleMin = 3.5f;
        public float idleMax = 7f;

        SimpleWalker walker;
        Transform model;
        Transform shadow;
        Transform npc;
        Transform plot;
        bool active;

        void Start()
        {
            if (characterPrefab != null)
            {
                npc = Instantiate(characterPrefab, transform.position, transform.rotation).transform;
                var cc = npc.GetComponent<CustomerController>();
                if (cc != null) Destroy(cc);
                walker = npc.GetComponent<SimpleWalker>();
                model = npc.Find("Model");
                shadow = npc.Find("Shadow");
            }

            var go = GameObject.Find("Plot_" + requiresBuilding);
            if (go != null) plot = go.transform;

            SetVisual(false);
            StartCoroutine(Routine());
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool built = gm != null && gm.buildings != null && !string.IsNullOrEmpty(requiresBuilding)
                         && gm.buildings.GetLevel(requiresBuilding) >= 1;
            if (built == active) return;
            active = built;
            SetVisual(built);
        }

        IEnumerator Routine()
        {
            yield return null;
            int side = 1;
            while (true)
            {
                if (!active || walker == null || plot == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Working spot in front of the building; alternates sides when patrolling.
                Vector3 post = plot.position + plot.forward * 1.5f
                               + plot.right * patrolOffset * side + Jitter(0.15f);
                yield return walker.MoveTo(post, moveSpeed);
                walker.FaceTowards(plot.position + plot.forward * 5f);

                // Busy gestures at the post — haggling, arranging wares, chanting.
                int gestures = Random.Range(1, 3);
                for (int g = 0; g < gestures; g++)
                {
                    if (model != null)
                        Tween.PunchScale(model, new Vector3(0.07f, -0.07f, 0.07f), 0.45f);
                    yield return new WaitForSeconds(Random.Range(0.9f, 1.5f));
                }

                yield return new WaitForSeconds(Random.Range(idleMin, idleMax));
                side = -side;
            }
        }

        void SetVisual(bool on)
        {
            if (model != null) model.gameObject.SetActive(on);
            if (shadow != null) shadow.gameObject.SetActive(on);
        }

        Vector3 Jitter(float r)
            => new Vector3(Random.Range(-r, r), 0f, Random.Range(-r, r));
    }
}
