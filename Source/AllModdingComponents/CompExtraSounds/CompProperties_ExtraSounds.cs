using Verse;

namespace CompExtraSounds
{
    internal class CompProperties_ExtraSounds : CompProperties
    {
        public SoundDef soundExtra;
        public SoundDef soundExtraTwo;
        public SoundDef soundHitBuilding;
        public SoundDef soundHitPawn;
        public SoundDef soundMiss;

        public CompProperties_ExtraSounds()
        {
            compClass = typeof(CompExtraSounds);
        }
    }
}