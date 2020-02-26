using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace JecsTools
{
    public class PlaceWorker_Outline : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            // Changed by Tad : Missing Bool in Override, base provided.
            // base.DrawGhost(def, center, rot, ghostCol, thing);
            var drawFieldCells = new List<IntVec3>();
            foreach (var c in GenAdj.CellsOccupiedBy(center, rot, def.size))
                drawFieldCells.Add(c);
            GenDraw.DrawFieldEdges(drawFieldCells);
            drawFieldCells = null;
            
        }

    }
}