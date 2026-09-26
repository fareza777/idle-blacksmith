using System.Collections;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The mine hand: once the Ore Mine stands, this miner works a loop between the mine
    /// mouth and the smithy's ore pile — walks in empty, chips the rock a few swings, then
    /// hauls a chunk of ore to the pile. Hidden until the building exists.
    /// The villager itself is spawned at runtime so the scene holds only a plain marker GO.
    /// </summary>
    public class MinerController : MonoBehaviour
    {
        [Tooltip("Villager prefab instantiated at runtime (CustomerA/B)")]
        public GameObject characterPrefab;
        [Tooltip("Ore pile root; the drop-off end of the haul")]
        public Transform pile;
        public float moveSpeed = 1.2f;

        SimpleWalker walker;
        Transform model;
        Transform shadow;
        Transform npc;
        Transform mine;
        GameObject chunk;
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

            var plot = GameObject.Find("Plot_" + BuildingId.Mine);
            if (plot != null)
            {
                mine = plot.transform;
                if (npc != null)
                    npc.position = mine.position + mine.forward * 1.4f;
            }

            chunk = BuildChunk();
            chunk.SetActive(false);

            SetVisual(false);
            StartCoroutine(RouteLoop());
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool built = gm != null && gm.buildings != null
                         && gm.buildings.GetLevel(BuildingId.Mine) >= 1;
            if (built == active) return;
            active = built;
            SetVisual(built);
        }

        IEnumerator RouteLoop()
        {
            yield return null;
            while (true)
            {
                if (!active || mine == null || pile == null || walker == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Empty-handed walk to the mine mouth.
                Vector3 mouth = mine.position + mine.forward * 1.5f + Jitter(0.2f);
                yield return walker.MoveTo(mouth, moveSpeed);
                walker.FaceTowards(mine.position);

                // Chip the rock — a few staggered swings read as pick work.
                int swings = Random.Range(3, 5);
                for (int i = 0; i < swings; i++)
                {
                    if (model != null)
                        Tween.PunchScale(model, new Vector3(0.10f, -0.10f, 0.10f), 0.28f);
                    yield return new WaitForSeconds(Random.Range(0.45f, 0.7f));
                }

                // Haul the chunk to the ore pile.
                chunk.SetActive(true);
                AudioManager.Play("pop", 0.05f, 0.4f);
                Vector3 drop = pile.position + Vector3.forward * 0.9f + Jitter(0.25f);
                yield return walker.MoveTo(drop, moveSpeed);
                walker.FaceTowards(pile.position);
                chunk.SetActive(false);
                Tween.PunchScale(pile, new Vector3(0.06f, -0.05f, 0.06f), 0.35f);
                yield return new WaitForSeconds(Random.Range(3f, 6f));
            }
        }

        void SetVisual(bool on)
        {
            if (model != null) model.gameObject.SetActive(on);
            if (shadow != null) shadow.gameObject.SetActive(on);
            if (chunk != null && !on) chunk.SetActive(false);
        }

        Vector3 Jitter(float r)
            => new Vector3(Random.Range(-r, r), 0f, Random.Range(-r, r));

        GameObject BuildChunk()
        {
            var c = Primitives.Create(PrimitiveType.Cube);
            c.transform.SetParent(npc != null ? npc : transform, false);
            c.transform.localScale = new Vector3(0.20f, 0.15f, 0.24f);
            var r = c.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh);
                m.SetColor("_BaseColor", new Color(0.35f, 0.52f, 0.62f));
                r.material = m;
            }
            // Held low in front of the chest — two-handed carry, same as the porter's crate.
            c.transform.localPosition = new Vector3(0f, 0.62f, 0.30f);
            c.transform.localRotation = Quaternion.Euler(12f, 28f, 8f);
            return c;
        }
    }
}
