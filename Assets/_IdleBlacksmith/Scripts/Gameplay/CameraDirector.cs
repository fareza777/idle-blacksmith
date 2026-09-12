using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Frames the forge complex. As buildings go up the camera pulls back and re-centres, so
    /// growing the complex is something you watch happen rather than read about.
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        [Tooltip("Always-present roots kept in frame (the smithy)")]
        public Transform[] staticAnchors;

        [Tooltip("The smithy's footprint per growth level, filled in by the scene builder")]
        public BuildingFootprint[] smithyTiers;

        [Tooltip("Complex buildings; unbuilt ones are ignored so the camera only widens as they appear")]
        public BuildingVisuals[] buildings;

        /// <summary>
        /// One smithy growth tier's footprint. The environment is authored as a single merged mesh
        /// that includes a 46x46 grass plate, so its renderer bounds are useless for framing — the
        /// dimensions are recorded here at build time instead.
        /// </summary>
        [System.Serializable]
        public struct BuildingFootprint
        {
            public float halfWidth;
            public float frontZ;
            public float backZ;
            public float height;
        }

        [Tooltip("Where the camera looks when only the starting Smithy exists")]
        public Vector3 baseFocus = new Vector3(0.15f, 0f, -0.55f);
        public float baseSize = 5.7f;

        [Tooltip("Breathing room around the outermost building")]
        public float padding = 0.4f;
        [Tooltip("Hard limit on how far the camera may pull back. The forge is the hero of the shot, " +
                 "so this stays tight and satellite buildings are allowed to sit near the frame edge.")]
        public float maxSize = 14f;

        [Tooltip("Positive values push the complex up the screen, clear of the bottom bar")]
        public float verticalBias = 0.10f;

        [Tooltip("Seconds for the framing to settle; larger is a slower, calmer pull-back")]
        public float easeSeconds = 1.6f;

        [Tooltip("Barely-there idle sway so the shop feels alive; 0 disables it")]
        public float swayAmount = 0.04f;
        public float swaySpeed = 0.35f;

        Camera cam;
        Vector3 focus;
        float size;

        /// <summary>Where the framing has settled, before the idle sway offset is added.</summary>
        Vector3 settledPos;
        Vector3 targetPos;
        float targetSize;
        Vector3 offsetDir;
        float distance;
        bool easing;
        float checkTimer;
        bool initialized;

        void Awake() => EnsureInit();

        /// <summary>
        /// Resolves the camera and the fixed viewing direction. Runs lazily because Awake never
        /// fires for a plain MonoBehaviour in edit mode, and the editor previews call Frame()
        /// directly.
        /// </summary>
        void EnsureInit()
        {
            if (initialized) return;
            initialized = true;

            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            focus = baseFocus;
            size = cam != null ? cam.orthographicSize : baseSize;

            // The viewing angle stays fixed; only the distance and ortho size change.
            Vector3 dir = transform.position - baseFocus;
            offsetDir = dir.sqrMagnitude > 0.001f ? dir.normalized : new Vector3(0.4f, 0.72f, -0.63f);
            distance = dir.magnitude > 0.001f ? dir.magnitude : 14f;
            settledPos = transform.position;
            targetPos = settledPos;
            targetSize = size;
        }

        void Start()
        {
            EnsureInit();
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.buildings != null)
            {
                gm.buildings.OnBuildingChanged += (_, __) => Frame();
                gm.OnShopTierChanged += _ => Frame();
            }
            Frame();
        }

        void Update()
        {
            if (!initialized) EnsureInit();
            // Cheap safety net: the anchor set can change without an event (prestige, load).
            checkTimer -= Time.deltaTime;
            if (checkTimer <= 0f)
            {
                checkTimer = 0.5f;
                Frame();
            }

            if (easing)
            {
                float tau = Mathf.Max(0.05f, easeSeconds / 3f);
                float k = 1f - Mathf.Exp(-Time.deltaTime / tau);

                settledPos = Vector3.Lerp(settledPos, targetPos, k);
                size = Mathf.Lerp(size, targetSize, k);
                if (cam != null) cam.orthographicSize = size;

                if (Vector3.SqrMagnitude(settledPos - targetPos) < 0.0009f
                    && Mathf.Abs(size - targetSize) < 0.02f)
                {
                    settledPos = targetPos;
                    size = targetSize;
                    if (cam != null) cam.orthographicSize = size;
                    easing = false;
                }
            }

            // This component is the only writer of the camera transform (a second sway script
            // writing position here would fight the framing and make the whole view jitter), so
            // the idle sway is applied on top of the settled position instead.
            ApplySway();
        }

        /// <summary>Places the camera at its settled position plus the idle sway offset.</summary>
        void ApplySway()
        {
            if (swayAmount <= 0f)
            {
                transform.position = settledPos;
                AimAtFocus();
                return;
            }

            float t = Time.time * swaySpeed;
            var offset = new Vector3(
                Mathf.Sin(t) * swayAmount,
                Mathf.Sin(t * 0.7f) * swayAmount * 0.5f,
                Mathf.Cos(t * 0.85f) * swayAmount);

            transform.position = settledPos + offset;
            AimAtFocus();
        }

        /// <summary>
        /// Recomputes the framing from the world bounds of everything that exists. The bounds are
        /// gathered as a point cloud and projected onto the camera's own right/up axes, so the
        /// diagonal view angle is handled exactly. Fitting actual points rather than the corner of
        /// an axis-aligned box matters a lot here: the complex is a cross, so the corners of its
        /// bounding box are empty grass.
        /// </summary>
        public void Frame()
        {
            EnsureInit();

            points.Clear();
            CollectSmithy();
            CollectBuildings();
            if (staticAnchors != null)
                foreach (Transform t in staticAnchors)
                    if (t != null && t.gameObject.activeInHierarchy) points.Add(t.position);

            if (points.Count == 0) points.Add(baseFocus);

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector3 p in points)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z);
                maxZ = Mathf.Max(maxZ, p.z);
            }

            focus = new Vector3((minX + maxX) * 0.5f, baseFocus.y, (minZ + maxZ) * 0.5f);

            // offsetDir is the direction from the focus toward the camera, so the camera's own
            // right and up axes come straight from it.
            Vector3 right = Vector3.Cross(Vector3.up, offsetDir).normalized;
            Vector3 viewUp = Vector3.Cross(offsetDir, right).normalized;

            // Measure the extent along each view axis, then slide the focus to the middle of that
            // range. Both axes are perpendicular to the view direction, so this recentres the shot
            // without changing the viewing distance.
            float minR = float.MaxValue, maxR = float.MinValue;
            float minU = float.MaxValue, maxU = float.MinValue;
            foreach (Vector3 p in points)
            {
                Vector3 d = p - focus;
                float dr = Vector3.Dot(d, right);
                float du = Vector3.Dot(d, viewUp);
                minR = Mathf.Min(minR, dr); maxR = Mathf.Max(maxR, dr);
                minU = Mathf.Min(minU, du); maxU = Mathf.Max(maxU, du);
            }

            focus += right * ((minR + maxR) * 0.5f) + viewUp * ((minU + maxU) * 0.5f);

            float halfRight = (maxR - minR) * 0.5f;
            float halfUp = (maxU - minU) * 0.5f;

            float aspect = cam != null && cam.aspect > 0.05f ? cam.aspect : 0.5625f;
            float needed = Mathf.Max(halfUp, halfRight / aspect) + padding;
            targetSize = Mathf.Clamp(needed, baseSize, maxSize);

            // Slide the shot up so the yard sits above the bottom bar instead of behind it.
            focus += viewUp * (targetSize * verticalBias);

            // Pull back as the frame widens so nothing clips through the near plane.
            distance = (baseFocus - new Vector3(5.5f, 10.1f, -8.9f)).magnitude + (targetSize - baseSize) * 2.2f;
            targetPos = focus + offsetDir * distance;

            if (Vector3.SqrMagnitude(targetPos - settledPos) > 0.0009f
                || Mathf.Abs(targetSize - size) > 0.02f)
                easing = true;
        }

        readonly System.Collections.Generic.List<Vector3> points = new System.Collections.Generic.List<Vector3>(256);

        /// <summary>
        /// Adds a mesh renderer's bounds corners. Only meshes count: particle systems report bounds
        /// sized for their maximum particle budget, which is far larger than the effect itself and
        /// would drag the camera much too far back.
        /// </summary>
        void CollectRenderer(Renderer r)
        {
            if (r == null || !(r is MeshRenderer)) return;
            Bounds b = r.bounds;
            if (b.size.x > 20f || b.size.z > 20f) return;

            Vector3 c = b.center, e = b.extents;
            for (int i = 0; i < 8; i++)
                points.Add(new Vector3(
                    c.x + ((i & 1) == 0 ? -e.x : e.x),
                    c.y + ((i & 2) == 0 ? -e.y : e.y),
                    c.z + ((i & 4) == 0 ? -e.z : e.z)));
        }

        /// <summary>Adds the smithy's footprint box for its current level.</summary>
        void CollectSmithy()
        {
            if (smithyTiers == null || smithyTiers.Length == 0) return;
            GameManager gm = GameManager.Instance;
            int level = gm != null && gm.buildings != null ? gm.buildings.GetLevel(BuildingId.Smithy) : 1;
            int idx = Mathf.Clamp(level - 1, 0, smithyTiers.Length - 1);
            BuildingFootprint f = smithyTiers[idx];

            for (int i = 0; i < 8; i++)
                points.Add(new Vector3(
                    (i & 1) == 0 ? -f.halfWidth : f.halfWidth,
                    (i & 2) == 0 ? 0f : f.height,
                    (i & 4) == 0 ? f.frontZ : f.backZ));
        }

        void CollectBuildings()
        {
            if (buildings == null) return;
            foreach (BuildingVisuals b in buildings)
            {
                if (b == null) continue;

                if (!b.IsBuilt || b.Current == null)
                {
                    // An empty plot still counts: its "build here" sign has to be on screen, otherwise
                    // a new player sees a single shop and never learns the yard can grow.
                    points.Add(b.transform.position);
                    continue;
                }

                foreach (Renderer r in b.Current.GetComponentsInChildren<Renderer>(true))
                    CollectRenderer(r);
            }
        }

        /// <summary>
        /// Applies the current framing immediately. Editor tooling uses this because the eased
        /// path only advances in Update, which never runs outside play mode.
        /// </summary>
        public void SnapToTarget()
        {
            EnsureInit();
            Frame();
            settledPos = targetPos;
            size = targetSize;
            if (cam != null) cam.orthographicSize = size;
            easing = false;
            ApplySway();
        }

        void AimAtFocus()
        {
            Vector3 dir = focus - transform.position;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
