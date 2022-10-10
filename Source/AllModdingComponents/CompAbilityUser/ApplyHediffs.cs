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
            Scribe_Values.Look(ref applyChance, nameof(applyChance), -1.0f);
            Scribe_Values.Look(ref severity, nameof(severity), 1.0f);
        }
    }
}
