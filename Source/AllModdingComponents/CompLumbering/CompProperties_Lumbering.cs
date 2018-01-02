using Verse;

namespace CompLumbering
{
    public class CompProperties_Lumbering : CompProperties
    {
        public GraphicData cycledGraphic = null;
        public float secondsBetweenSteps = 0.0f;
        public float secondsPerStep = 0.0f;
        public SoundDef sound = null;
        public bool staggerEffect = true;

        public CompProperties_Lumbering()
        {
            compClass = typeof(CompLumbering);
        }
    }
}