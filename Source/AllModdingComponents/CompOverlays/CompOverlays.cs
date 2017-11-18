using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CompOverlays
{
    public class CompOverlays : ThingComp
    {
        public CompProperties_Overlays Props => this.props as CompProperties_Overlays;

        public override void PostDraw()
        {
            base.PostDraw();
            if (Props.fuelRequired && this.parent.TryGetComp<CompRefuelable>() is CompRefuelable rf && rf.HasFuel ||
                Props.fuelRequired == false)
            {
                Vector3 drawPos = this.parent.DrawPos;
                drawPos.y += 0.046875f;
                for (int i = 0; i < Props.overlays.Count; i++)
                {
                    GraphicOverlay o = Props.overlays[i];
                    Vector3 vec3 = drawPos + o.offset;
                    o.graphicData.Graphic.Draw(vec3, this.parent.Rotation, this.parent, 0f);
                }
            }

        }
    }
}
