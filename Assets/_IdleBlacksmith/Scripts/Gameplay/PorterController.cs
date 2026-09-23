using System.Collections;
using IdleBlacksmith.Core;
using PrimeTween;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// The storehouse hand: once the Storehouse stands, this porter hauls crates between the
    /// smithy door and the depot on a loop — empty-handed out, cargo back. He rests at the
    /// depot between runs so the building visibly does work. Hidden until the building exists.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SimpleWalker))]
    public class PorterController : MonoBehaviour
    {
        [Tooltip("Depot plot root (Plot_storehouse); its position anchors the route")]
        public Transform depot;
        [Tooltip("Smithy-side waypoint the porter walks to each run")]
        public Transform shopPoint;
        public float moveSpeed = 1.25f;
        public float crateSize = 0.30f;

        SimpleWalker walker;
        Transform model;
        GameObject crate;
        bool active;

        void Awake()
        {
            walker = GetComponent<SimpleWalker>();
            model = transform.Find("Model");
        }

        void Start()
        {
            if (depot == null)
            {
                var go = GameObject.Find("Plot_" + BuildingId.Storehouse);
                if (go != null) depot = go.transform;
            }
            if (depot != null) transform.position = depot.position + Vector3.forward * 1.3f;

            // Customer.controller has no Carry param — the crate prop itself sells the haul.
            crate = BuildCrate();
            crate.SetActive(false);

            if (model != null) model.gameObject.SetActive(false);
            StartCoroutine(RouteLoop());
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool built = gm != null && gm.buildings != null
                         && gm.buildings.GetLevel(BuildingId.Storehouse) >= 1;
            if (built == active) return;
            active = built;
            if (model != null) model.gameObject.SetActive(built);
        }

        IEnumerator RouteLoop()
        {
            yield return null;
            while (true)
            {
                if (!active || depot == null || shopPoint == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // Empty-handed walk to the smithy.
                yield return walker.MoveTo(shopPoint.position + Jitter(0.25f), moveSpeed);
                walker.FaceTowards(shopPoint.position + Vector3.up);
                yield return new WaitForSeconds(Random.Range(1.2f, 2.4f));

                // Load up and haul the crate back to the depot.
                PickUpCrate();
                Vector3 home = depot.position + Vector3.forward * 1.35f + Jitter(0.2f);
                yield return walker.MoveTo(home, moveSpeed);
                walker.FaceTowards(depot.position);
                DropCrate();
                yield return new WaitForSeconds(Random.Range(4f, 8f));
            }
        }

        Vector3 Jitter(float r)
            => new Vector3(Random.Range(-r, r), 0f, Random.Range(-r, r));

        GameObject BuildCrate()
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(transform, false);
            c.transform.localScale = Vector3.one * crateSize;
            var r = c.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                var m = new Material(sh);
                m.SetColor("_BaseColor", new Color(0.55f, 0.38f, 0.20f));
                r.material = m;
            }
            // Held low in front of the chest — reads as a two-handed carry.
            c.transform.localPosition = new Vector3(0f, 0.62f, 0.34f);
            c.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            return c;
        }

        void PickUpCrate()
        {
            crate.SetActive(true);
            if (model != null) Tween.PunchScale(model, new Vector3(0.10f, -0.10f, 0.10f), 0.3f);
            AudioManager.Play("pop", 0.06f, 0.45f);
        }

        void DropCrate()
        {
            crate.SetActive(false);
            if (model != null) Tween.PunchScale(model, new Vector3(0.10f, -0.10f, 0.10f), 0.3f);
        }
    }
}
