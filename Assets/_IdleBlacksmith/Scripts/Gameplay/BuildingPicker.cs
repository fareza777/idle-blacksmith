using IdleBlacksmith.Core;
using IdleBlacksmith.UI;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Lets the player tap a building in the world to jump straight to its upgrade row.
    ///
    /// There are no colliders in the generated world, so the hit test is done in screen space:
    /// each building contributes one point just above its base, and a tap picks the nearest one
    /// within a finger-sized radius. That keeps the interaction free of any physics setup.
    /// </summary>
    public class BuildingPicker : MonoBehaviour
    {
        [Tooltip("All complex buildings; a tap opens whichever is nearest")]
        public BuildingVisuals[] buildings;

        [Tooltip("The anvil — while a craft is running, taps near it hammer the sword faster")]
        public AnvilStation anvil;

        [Tooltip("Tap tolerance for the anvil, tighter than buildings so it doesn't steal building taps")]
        public float anvilTapRadiusNormalized = 0.085f;

        [Tooltip("The ore pile — taps grab a chunk straight off the stock")]
        public OrePile orePile;

        [Tooltip("The forge cat — tapping it earns a purr, nothing more")]
        public CatAmbient cat;

        [Tooltip("Workbench tools — bellows, grindstone, quench trough — each with its own tap effect")]
        public ToolStation[] tools;

        [Tooltip("Tap tolerance for workbench tools")]
        public float toolTapRadiusNormalized = 0.075f;

        [Tooltip("Tap tolerance for the cat, the tightest hitbox in the shop")]
        public float catTapRadiusNormalized = 0.05f;

        [Tooltip("Tap tolerance for the ore pile")]
        public float oreTapRadiusNormalized = 0.09f;

        [Tooltip("Tap tolerance as a fraction of screen height, so it feels the same on any device")]
        public float tapRadiusNormalized = 0.12f;

        [Tooltip("How far the finger may travel and still count as a tap rather than a drag")]
        public float dragThreshold = 22f;

        [Tooltip("The camera director; taps are ignored while the player is dragging the view")]
        public CameraDirector director;

        Camera cam;
        Vector2 pressedAt;
        bool pressed;
        bool pressedOverUI;

        void Awake()
        {
            cam = Camera.main;
            if (director == null) director = FindFirstObjectByType<CameraDirector>();
        }

        void Update()
        {
            if (buildings == null || buildings.Length == 0) return;
            if (cam == null) { cam = Camera.main; if (cam == null) return; }

            // A building opens on release, and only when the press stayed put: otherwise every
            // attempt to drag the view would also open whatever was under the finger.
            if (Input.GetMouseButtonDown(0))
            {
                pressed = true;
                pressedAt = Input.mousePosition;
                var es = UnityEngine.EventSystems.EventSystem.current;
                pressedOverUI = es != null && es.IsPointerOverGameObject();
                return;
            }

            if (!Input.GetMouseButtonUp(0) || !pressed) return;
            pressed = false;

            if (pressedOverUI) return;
            if (director != null && director.IsDragging) return;
            if (Vector2.Distance(Input.mousePosition, pressedAt) > dragThreshold) return;

            TryPick(Input.mousePosition);
        }

        void TryPick(Vector3 screenPos)
        {
            float bestDist = float.MaxValue;
            BuildingVisuals best = null;

            foreach (BuildingVisuals b in buildings)
            {
                if (b == null) continue;

                // Unbuilt plots are pickable too — that is how a new player learns they can build.
                Vector3 sp = cam.WorldToScreenPoint(b.transform.position + Vector3.up * 0.9f);
                if (sp.z <= 0f) continue;

                float d = Vector2.Distance(sp, screenPos);
                if (d < bestDist) { bestDist = d; best = b; }
            }

            // While a sword is on the anvil, a tap on the anvil is a hammer blow, not a menu
            // open — the active-craft read matters more than opening the smithy row.
            if (anvil != null && anvil.IsCrafting)
            {
                Vector3 ap = cam.WorldToScreenPoint(anvil.transform.position + Vector3.up * 0.55f);
                if (ap.z > 0f && Vector2.Distance(ap, screenPos) <= anvilTapRadiusNormalized * Screen.height)
                {
                    anvil.TapBoost();
                    return;
                }
            }

            // A tap on the ore pile chips a chunk straight into the stock.
            if (orePile != null)
            {
                Vector3 op = cam.WorldToScreenPoint(orePile.transform.position + Vector3.up * 0.5f);
                if (op.z > 0f && Vector2.Distance(op, screenPos) <= oreTapRadiusNormalized * Screen.height)
                {
                    orePile.ManualMine();
                    return;
                }
            }

            // Workbench tools each carry their own function.
            if (tools != null)
            {
                foreach (ToolStation t in tools)
                {
                    if (t == null) continue;
                    Vector3 tp = cam.WorldToScreenPoint(t.AnchorWorld);
                    if (tp.z > 0f && Vector2.Distance(tp, screenPos) <= toolTapRadiusNormalized * Screen.height)
                    {
                        t.Use();
                        return;
                    }
                }
            }

            // The cat wins no menus — only a purr.
            if (cat != null)
            {
                Vector3 cp = cam.WorldToScreenPoint(cat.transform.position + Vector3.up * 0.25f);
                if (cp.z > 0f && Vector2.Distance(cp, screenPos) <= catTapRadiusNormalized * Screen.height)
                {
                    cat.Pet();
                    return;
                }
            }

            if (best == null) return;
            if (bestDist > tapRadiusNormalized * Screen.height) return;

            Open(best.buildingId);
        }

        /// <summary>Opens the complex sheet with this building's row highlighted and scrolled to.</summary>
        public static void Open(string buildingId)
        {
            UIManager ui = UIManager.Instance;
            if (ui == null || ui.complexPanel == null) return;
            AudioManager.Play("pop", 0.03f);
            ui.OpenComplexFocused(buildingId);
        }
    }
}
