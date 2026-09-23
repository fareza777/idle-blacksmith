using System.Collections;
using System.Collections.Generic;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The delver: makes the Dungeon Gate visibly work. When an expedition launches the
    /// adventurer walks to the gate arch and slips inside; when one is claimed they come
    /// back out carrying a glowing sack and drop it at the smithy door before wandering
    /// off. Driven by ExpeditionManager.OnChanged — slot count up = a dive, down = a return.
    /// The villager itself is spawned at runtime so the scene holds only a plain marker GO.
    /// </summary>
    public class AdventurerController : MonoBehaviour
    {
        [Tooltip("Villager prefab instantiated at runtime (CustomerA/B)")]
        public GameObject characterPrefab;
        [Tooltip("Show the carried sword prop on the spawned villager")]
        public bool armSword;
        public float moveSpeed = 1.45f;

        SimpleWalker walker;
        Transform model;
        Transform shadow;
        Transform npc;
        Transform gate;
        Transform door;
        GameObject sack;
        int lastRunning = -1;
        readonly Queue<IEnumerator> pending = new Queue<IEnumerator>();
        bool busy;

        void Start()
        {
            if (characterPrefab != null)
            {
                npc = Instantiate(characterPrefab, transform.position, transform.rotation).transform;
                var cc = npc.GetComponent<CustomerController>();
                if (cc != null)
                {
                    if (armSword && cc.carriedSwordProp != null)
                        cc.carriedSwordProp.SetActive(true);   // armed for the delve
                    Destroy(cc);
                }
                walker = npc.GetComponent<SimpleWalker>();
                model = npc.Find("Model");
                shadow = npc.Find("Shadow");
            }

            var go = GameObject.Find("Plot_" + BuildingId.Gate);
            if (go != null) gate = go.transform;
            var d = GameObject.Find("WpDoor");
            if (d != null) door = d.transform;

            sack = BuildSack();
            sack.SetActive(false);
            SetVisual(false);

            var gm = GameManager.Instance;
            if (gm != null && gm.expeditions != null)
            {
                lastRunning = gm.expeditions.RunningCount;
                gm.expeditions.OnChanged += HandleChanged;
            }
            StartCoroutine(Worker());
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.expeditions != null)
                gm.expeditions.OnChanged -= HandleChanged;
        }

        void HandleChanged()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.expeditions == null || gate == null || walker == null) return;
            int running = gm.expeditions.RunningCount;
            if (lastRunning < 0) lastRunning = running;
            if (running > lastRunning)
                for (int i = lastRunning; i < running; i++)
                    pending.Enqueue(DiveIn());
            else if (running < lastRunning)
                for (int i = running; i < lastRunning; i++)
                    pending.Enqueue(WalkOut());
            lastRunning = running;
        }

        IEnumerator Worker()
        {
            yield return null;
            while (true)
            {
                if (!busy && pending.Count > 0)
                {
                    busy = true;
                    yield return pending.Dequeue();
                    busy = false;
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        IEnumerator DiveIn()
        {
            if (gate == null || npc == null) yield break;

            // Stroll up from the yard path and slip through the arch.
            Vector3 approach = gate.position + gate.forward * 3.2f;
            npc.position = approach + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f);
            SetVisual(true);
            Vector3 mouth = gate.position + gate.forward * 1.1f;
            yield return walker.MoveTo(mouth, moveSpeed);
            walker.FaceTowards(gate.position);
            yield return new WaitForSeconds(0.35f);

            // Shrink into the dark — reads as stepping below.
            if (model != null)
            {
                Tween.Scale(model, Vector3.one * 0.02f, 0.38f, Ease.InQuad);
                yield return new WaitForSeconds(0.42f);
                model.localScale = Vector3.one;
            }
            SetVisual(false);
            AudioManager.Play("pop", 0.04f, 0.3f);
        }

        IEnumerator WalkOut()
        {
            if (gate == null || npc == null) yield break;

            // Emerge at the arch, sack in hand.
            Vector3 mouth = gate.position + gate.forward * 1.1f;
            npc.position = mouth;
            SetVisual(true);
            if (model != null)
            {
                model.localScale = Vector3.one * 0.2f;
                Tween.Scale(model, Vector3.one, 0.3f, Ease.OutBack);
            }
            yield return new WaitForSeconds(0.15f);
            sack.SetActive(true);
            AudioManager.Play("fanfare", 0.05f, 0.5f);

            // Deliver the haul at the smithy door, then head home the way they came.
            if (door != null)
            {
                yield return walker.MoveTo(door.position + Jitter(0.3f), moveSpeed);
                walker.FaceTowards(door.position + Vector3.up * 2f);
                yield return new WaitForSeconds(0.8f);
                sack.SetActive(false);
                if (model != null)
                    Tween.PunchScale(model, new Vector3(0.06f, 0.10f, 0.06f), 0.4f);
            }
            Vector3 away = gate.position + gate.forward * 3.4f + new Vector3(Random.Range(-0.5f, 0.5f), 0f, 0f);
            yield return walker.MoveTo(away, moveSpeed);
            SetVisual(false);
            sack.SetActive(false);
        }

        void SetVisual(bool on)
        {
            if (model != null) model.gameObject.SetActive(on);
            if (shadow != null) shadow.gameObject.SetActive(on);
            if (sack != null && !on) sack.SetActive(false);
        }

        Vector3 Jitter(float r)
            => new Vector3(Random.Range(-r, r), 0f, Random.Range(-r, r));

        GameObject BuildSack()
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(npc != null ? npc : transform, false);
            c.transform.localScale = new Vector3(0.30f, 0.24f, 0.30f);
            var r = c.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh);
                // Faint cyan — delved relic ore glowing through the cloth.
                m.SetColor("_BaseColor", new Color(0.35f, 0.75f, 0.85f));
                m.SetColor("_EmissionColor", new Color(0.12f, 0.4f, 0.5f) * 1.6f);
                m.EnableKeyword("_EMISSION");
                r.material = m;
            }
            // Slung on the back.
            c.transform.localPosition = new Vector3(0f, 0.72f, -0.26f);
            return c;
        }
    }
}
