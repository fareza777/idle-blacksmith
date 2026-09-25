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
        static Material beamMat;
        BuildingVisuals[] plots;
        GameObject diamond;
        GameObject beam;
        GameManager gm;
        float t;
        Vector3 anchor;
        float beamBase;

        void Start()
        {
            gm = GameManager.Instance;
            plots = FindObjectsByType<BuildingVisuals>(FindObjectsSortMode.None);
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null)
            {
                goldMat = new Material(sh);
                goldMat.color = new Color(1f, 0.78f, 0.30f);
                beamMat = new Material(sh);
                beamMat.color = new Color(1f, 0.82f, 0.35f, 0.55f);
            }
            // Thin golden beam rising from the target to the bobbing diamond —
            // reads as a quest marker from any angle instead of a tiny cube in the sky.
            beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "QuestMarkerBeam";
            beam.transform.SetParent(transform, false);
            beam.transform.localScale = new Vector3(0.07f, 1f, 0.07f);
            if (beamMat != null) beam.GetComponent<Renderer>().sharedMaterial = beamMat;
            var bcol = beam.GetComponent<Collider>();
            if (bcol != null) Destroy(bcol);
            beam.SetActive(false);

            diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "QuestMarkerGem";
            diamond.transform.SetParent(transform, false);
            diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            diamond.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
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
                if (beam != null && beam.activeSelf) beam.SetActive(false);
                return;
            }

            // Smithy anchor hangs well above the roofline, pulled toward the camera-facing
            // side so it sits in open sky and can't be lost behind the chimney/tent/awning.
            bool smithy = target == gm.environmentRoot;
            anchor = smithy
                ? target.position + new Vector3(0f, 5.2f, -2.6f)
                : target.position + new Vector3(0f, 2.3f, 0f);
            beamBase = smithy ? target.position.y + 0.5f : target.position.y + 0.4f;
            if (diamond == null) return;
            if (!diamond.activeSelf) diamond.SetActive(true);
            diamond.transform.position = anchor + new Vector3(0f, Mathf.Sin(t * 2.2f) * 0.18f, 0f);
            diamond.transform.rotation = Quaternion.Euler(45f, t * 40f, 45f);
            if (beam != null)
            {
                if (!beam.activeSelf) beam.SetActive(true);
                float len = anchor.y - beamBase - 0.2f;
                beam.transform.position = new Vector3(anchor.x, beamBase + len * 0.5f, anchor.z);
                beam.transform.localScale = new Vector3(0.07f, Mathf.Max(0.1f, len), 0.07f);
            }
        }

        /// <summary>
        /// The building the active quest happens at, or null when the quest has no
        /// physical location. Nearly every actionable goal maps to a building so the
        /// player always knows where to look next; passive goals (time, dailies,
        /// cat, ember) get no marker since they complete on their own.
        /// </summary>
        Transform ResolveTarget()
        {
            if (gm == null || gm.quests == null || plots == null) return null;
            var q = gm.quests.Active;
            if (q == null || gm.quests.IsComplete) return null;
            string id = GoalLocation(q);
            if (id == null) return null;
            // The smithy is the environment building itself, not a BuildingVisuals plot —
            // point at the environment root so forge/sell quests still get their marker.
            if (id == BuildingId.Smithy)
                return gm.environmentRoot;
            for (int i = 0; i < plots.Length; i++)
                if (plots[i] != null && plots[i].buildingId == id)
                    return plots[i].transform;
            return null;
        }

        static string GoalLocation(QuestDef q)
        {
            switch (q.goal)
            {
                case QuestGoal.UpgradeBuilding:
                    return string.IsNullOrEmpty(q.targetId) ? null : q.targetId;
                case QuestGoal.ClaimExpedition:
                    return BuildingId.Gate;
                case QuestGoal.BuildRunes:
                    return BuildingId.Sanctum;
                case QuestGoal.ReachOre:
                    return BuildingId.Mine;
                case QuestGoal.ReachGold:
                    return BuildingId.Market;
                case QuestGoal.ForgeSwords:
                case QuestGoal.SellSwords:
                case QuestGoal.EarnGoldRun:
                case QuestGoal.OwnRarity:
                case QuestGoal.MasterRecipe:
                case QuestGoal.UseTools:
                case QuestGoal.ServeOrders:
                case QuestGoal.RushOrders:
                case QuestGoal.FairSales:
                case QuestGoal.HireHelper:
                case QuestGoal.UnlockRecipe:
                    return BuildingId.Smithy;
                default:
                    return null;
            }
        }
    }
}
