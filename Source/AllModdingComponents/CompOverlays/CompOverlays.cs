using RimWorld;
using Verse;

namespace CompOverlays
{
    public class CompOverlays : ThingComp
    {
        public CompProperties_Overlays Props => props as CompProperties_Overlays;

        private CompRefuelable compRefuelable;

        public CompRefuelable GetRefuelable => compRefuelable;

        // Caching comps needs to happen after all comps are created. Ideally, this would be done right after
        // ThingWithComps.InitializeComps(). This requires overriding two hooks: PostPostMake and PostExposeData.

        public override void PostPostMake()
        {
            base.PostPostMake();
            CacheComps();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                CacheComps();
        }

        private void CacheComps()
        {
            // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
            // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
            // while `isinst` instruction against non-generic type operand like used below is fast.
            var comps = parent.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompRefuelable compRefuelable)
                {
                    this.compRefuelable = compRefuelable;
                    break;
                }
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (Props.fuelRequired == false ||
                GetRefuelable is CompRefuelable rf && rf.HasFuel)
            {
                var drawPos = parent.DrawPos;
                drawPos.y += 0.046875f;
                for (var i = 0; i < Props.overlays.Count; i++)
                {
                    var o = Props.overlays[i];
                    var vec3 = drawPos + o.offset;
                    if (o.usesStuff)
                    {
                        o.graphicData.GraphicColoredFor(parent).Draw(vec3, parent.Rotation, parent, 0f);
                        continue;
                    }
                    o.graphicData.Graphic.Draw(vec3, parent.Rotation, parent, 0f);
                }
            }
        }
    }
}
