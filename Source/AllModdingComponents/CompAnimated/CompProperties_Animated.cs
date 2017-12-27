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
    }
}