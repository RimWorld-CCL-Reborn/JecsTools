using System.Collections.Generic;
using Verse;

namespace CompAnimated
{
    public class CompProperties_Animated : CompProperties
    {
        public List<GraphicData> movingFrames = new List<GraphicData>();
        public float secondsBetweenFrames = 0.0f;
        public SoundDef sound = null;
        public List<GraphicData> stillFrames = new List<GraphicData>();

        public CompProperties_Animated()
        {
            compClass = typeof(CompAnimated);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (stillFrames.NullOrEmpty() && movingFrames.NullOrEmpty())
                yield return $"both {nameof(stillFrames)} and {nameof(movingFrames)} are null or empty";
            if (secondsBetweenFrames <= 0f)
                yield return nameof(secondsBetweenFrames) + " must be positive";
        }
    }
}
