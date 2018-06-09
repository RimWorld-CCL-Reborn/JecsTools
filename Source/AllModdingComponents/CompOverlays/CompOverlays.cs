using RimWorld;
using Verse;

namespace CompOverlays
{
    public class CompOverlays : ThingComp
    {
        public CompProperties_Overlays Props => props as CompProperties_Overlays;

        public override void PostDraw()
        {
            base.PostDraw();
            if (Props.fuelRequired && parent.TryGetComp<CompRefuelable>() is CompRefuelable rf && rf.HasFuel ||
                Props.fuelRequired == false)
            {
                var drawPos = parent.DrawPos;
                drawPos.y += 0.046875f;
                for (var i = 0; i < Props.overlays.Count; i++)
                {
                    var o = Props.overlays[i];
                    var vec3 = drawPos + o.offset;
                    if (o.usesStuff)
                    {
                        o.graphicData.GraphicColoredFor(this.parent).Draw(vec3, parent.Rotation, parent, 0f);
                        continue;
                    }
                    o.graphicData.Graphic.Draw(vec3, parent.Rotation, parent, 0f);
                }
            }
        }
    }
}