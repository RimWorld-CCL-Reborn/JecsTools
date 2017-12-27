using Verse;

namespace AbilityUser
{
    public class ApplyHediffs : IExposable
    {
        public float applyChance = 1.0f;
        public HediffDef hediffDef;
        public float severity = 1.0f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref applyChance, "applyChance", -1.0f);
            Scribe_Values.Look(ref severity, "severity", 1.0f);
        }
    }
}