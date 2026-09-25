using IdleBlacksmith.Core;
using UnityEngine;

namespace IdleBlacksmith.Gameplay
{
    /// <summary>
    /// A small floating marker that hovers over the building the current quest wants upgraded.
    /// Quests like "Reach Ore Mine level 1" name a building by text only; the marker turns that
    /// into a physical place in the yard so the player always knows where to look next.
    ///
    /// Spawned at runtime by GameManager (no scene wiring). A gold diamond bobs slowly above
    /// the target plot; it hides itself the moment the quest's goal is met.
    /// </summary>
    public class QuestMarker : MonoBehaviour
    {
        static Material goldMat;
        BuildingVisuals[] plots;
        GameObject diamond;
        GameManager gm;
        float t;
        Vector3 anchor;

        void Start()
        {
            gm = GameManager.Instance;
            plots = FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null)
            {
                goldMat = new Material(sh);
                goldMat.color = new Color(1f, 0.78f, 0.30f);
            }
            diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "QuestMarkerGem";
            diamond.transform.SetParent(transform, false);
            diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            diamond.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
            if (goldMat != null)
                diamond.GetComponent<Renderer>().sharedMaterial = goldMat;
            var col = diamond.GetComponent<Collider>();
            if (col != null) Destroy(col);
            diamond.SetActive(false);
        }

        void Update()
        {
            t += Time.deltaTime;
            Transform target = ResolveTarget();
            if (target == null)
            {
                if (diamond != null && diamond.activeSelf) diamond.SetActive(false);
                return;
            }

            anchor = target.position + new Vector3(0f, 2.3f, 0f);
            if (diamond == null) return;
            if (!diamond.activeSelf) diamond.SetActive(true);
            diamond.transform.position = anchor + new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.18f, 0f);
            diamond.transform.rotation = Quaternion.Euler(45f, t * 40f, 45f);
        }

        /// <summary>
        /// The plot the active quest points at, or null when the quest has no building goal.
        /// Only building quests get a marker — the rest already say where to look in words.
        /// </summary>
        Transform ResolveTarget()
        {
            if (gm == null || gm.quests == null || plots == null) return null;
            var q = gm.quests.Active;
            if (q == null || q.goal != QuestGoal.UpgradeBuilding || string.IsNullOrEmpty(q.targetId))
                return null;
            if (gm.buildings != null && gm.buildings.GetLevel(q.targetId) >= q.target)
                return null;
            for (int i = 0; i < plots.Length; i++)
                if (plots[i] != null && plots[i].buildingId == q.targetId)
                    return plots[i].transform;
            return null;
        }
    }
}
