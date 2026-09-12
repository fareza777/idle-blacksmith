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
