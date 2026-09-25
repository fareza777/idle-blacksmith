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
