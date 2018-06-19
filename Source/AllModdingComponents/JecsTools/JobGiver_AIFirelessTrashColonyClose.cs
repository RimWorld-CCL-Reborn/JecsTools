using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class JobGiver_AIFirelessTrashColonyClose : ThinkNode_JobGiver
    {
        private const int CloseSearchRadius = 5;

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.HostileTo(Faction.OfPlayer))
                return null;
            var flag = pawn.natives.IgniteVerb != null && pawn.HostileTo(Faction.OfPlayer);
            var cellRect = CellRect.CenteredOn(pawn.Position, 5);
            for (var i = 0; i < 35; i++)
            {
                var randomCell = cellRect.RandomCell;
                if (randomCell.InBounds(pawn.Map))
                {
                    var edifice = randomCell.GetEdifice(pawn.Map);
                    if (edifice != null && FirelessTrashUtility.ShouldTrashBuilding(pawn, edifice) &&
                        GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map, false, null, 0, 0))
                    {
                        if (DebugViewSettings.drawDestSearch && Find.CurrentMap == pawn.Map)
                            Find.CurrentMap.debugDrawer.FlashCell(randomCell, 1f, "trash bld");
                        return FirelessTrashUtility.TrashJob(pawn, edifice);
                    }
                    if (flag)
                    {
                        var plant = randomCell.GetPlant(pawn.Map);
                        if (plant != null && FirelessTrashUtility.ShouldTrashPlant(pawn, plant) &&
                            GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map, false, null, 0, 0))
                        {
                            if (DebugViewSettings.drawDestSearch && Find.CurrentMap == pawn.Map)
                                Find.CurrentMap.debugDrawer.FlashCell(randomCell, 0.5f, "trash plant");
                            return FirelessTrashUtility.TrashJob(pawn, plant);
                        }
                    }
                    if (DebugViewSettings.drawDestSearch && Find.CurrentMap == pawn.Map)
                        Find.CurrentMap.debugDrawer.FlashCell(randomCell, 0f, "trash no");
                }
            }
            return null;
        }
    }
}