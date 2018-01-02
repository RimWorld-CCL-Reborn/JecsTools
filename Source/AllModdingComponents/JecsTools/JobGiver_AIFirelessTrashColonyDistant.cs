using Verse;
using Verse.AI;

namespace JecsTools
{
    public class JobGiver_AIFirelessTrashColonyDistant : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            var count = allBuildingsColonist.Count;
            if (count == 0)
                return null;
            for (var i = 0; i < 75; i++)
            {
                var index = Rand.Range(0, count);
                var building = allBuildingsColonist[index];
                if (FirelessTrashUtility.ShouldTrashBuilding(pawn, building))
                    return FirelessTrashUtility.TrashJob(pawn, building);
            }
            return null;
        }
    }
}