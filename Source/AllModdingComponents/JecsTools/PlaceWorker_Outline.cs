using System;
using System.Linq;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace JecsTools
{
    public class PlaceWorker_Outline : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot)
        {
            //RoomGroup roomGroup = center.GetRoomGroup(base.Map);
            //if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
            //{
            List<IntVec3> drawFieldCells = new List<IntVec3>();
            foreach (IntVec3 c in GenAdj.CellsOccupiedBy(center, rot, def.size))
            {
                drawFieldCells.Add(c);
            }
            GenDraw.DrawFieldEdges(drawFieldCells);
            drawFieldCells = null;
            //}
        }
    }
}
