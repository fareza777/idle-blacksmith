using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// Keeps one building's prefab in sync with its level. Level 0 means "not built yet", in
    /// which case the plot simply stays empty.
    /// </summary>
    public class BuildingVisuals : MonoBehaviour
    {
        public string buildingId;
        [Tooltip("Prefab per level; index 0 is level 1")]
        public GameObject[] levelPrefabs;

        [Tooltip("Shown while the plot is empty, so the player can see there is something to build here")]
        public GameObject emptyMarker;

        public GameObject Current { get; private set; }
        public int ShownLevel { get; private set; } = -1;
        public bool IsBuilt => ShownLevel > 0;

        void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.buildings != null)
                gm.buildings.OnBuildingChanged += HandleChanged;
            Apply(gm != null && gm.buildings != null ? gm.buildings.GetLevel(buildingId) : 0);
        }

        void OnDestroy()
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.buildings != null)
                gm.buildings.OnBuildingChanged -= HandleChanged;
        }

        void HandleChanged(string id, int level)
        {
            if (id == buildingId) Apply(level);
        }

        public void Apply(int level)
        {
            if (level == ShownLevel) return;
            ShownLevel = level;

            if (Current != null) Destroy(Current);
            Current = null;

            if (level <= 0 || levelPrefabs == null || level > levelPrefabs.Length)
            {
                SetMarker(true);
                return;
            }

            GameObject prefab = levelPrefabs[level - 1];
            if (prefab == null)
            {
                SetMarker(true);
                return;
            }

            SetMarker(false);
            Current = Instantiate(prefab, transform);
            Current.transform.localPosition = Vector3.zero;
            Current.transform.localRotation = Quaternion.identity;
        }

        void SetMarker(bool on)
        {
            if (emptyMarker != null && emptyMarker.activeSelf != on)
                emptyMarker.SetActive(on);
        }
    }
}
