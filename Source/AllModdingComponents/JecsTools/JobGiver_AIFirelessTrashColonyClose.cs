using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            {
                return null;
            }
            bool flag = pawn.natives.IgniteVerb != null && pawn.HostileTo(Faction.OfPlayer);
            CellRect cellRect = CellRect.CenteredOn(pawn.Position, 5);
            for (int i = 0; i < 35; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                if (randomCell.InBounds(pawn.Map))
                {
                    Building edifice = randomCell.GetEdifice(pawn.Map);
                    if (edifice != null && FirelessTrashUtility.ShouldTrashBuilding(pawn, edifice) && GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map, false, null, 0, 0))
                    {
                        if (DebugViewSettings.drawDestSearch && Find.VisibleMap == pawn.Map)
                        {
                            Find.VisibleMap.debugDrawer.FlashCell(randomCell, 1f, "trash bld");
                        }
                        return FirelessTrashUtility.TrashJob(pawn, edifice);
                    }
                    if (flag)
                    {
                        Plant plant = randomCell.GetPlant(pawn.Map);
                        if (plant != null && FirelessTrashUtility.ShouldTrashPlant(pawn, plant) && GenSight.LineOfSight(pawn.Position, randomCell, pawn.Map, false, null, 0, 0))
                        {
                            if (DebugViewSettings.drawDestSearch && Find.VisibleMap == pawn.Map)
                            {
                                Find.VisibleMap.debugDrawer.FlashCell(randomCell, 0.5f, "trash plant");
                            }
                            return FirelessTrashUtility.TrashJob(pawn, plant);
                        }
                    }
                    if (DebugViewSettings.drawDestSearch && Find.VisibleMap == pawn.Map)
                    {
                        Find.VisibleMap.debugDrawer.FlashCell(randomCell, 0f, "trash no");
                    }
                }
            }
            return null;
        }



    }
}
