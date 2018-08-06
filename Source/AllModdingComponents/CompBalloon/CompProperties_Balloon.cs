using Verse;

namespace CompBalloon
{
    public class CompProperties_Balloon : CompProperties
    {
        public float secondsBetweenCycles = 3.0f;
        public FloatRange balloonRange = new FloatRange(0.95f, 1.05f);

        public CompProperties_Balloon()
        {
            compClass = typeof(CompBalloon);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (secondsBetweenCycles <= 0.0f)
                Log.ErrorOnce("CompBalloon :: CompProperties_Balloon secondsBetweenSteps needs to be more than 0",
                    132);
        }
    }
}