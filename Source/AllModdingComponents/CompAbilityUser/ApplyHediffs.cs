using System;
using Verse;

namespace AbilityUser
{
    public class ApplyHediffs : IExposable
    {
        public HediffDef hediffDef;
        public float applyChance = 1.0f;
        public float severity = 1.0f;

        public ApplyHediffs() { }

        public void ExposeData()
        {
            Scribe_Values.Look<float>(ref this.applyChance, "applyChance", -1.0f);
            Scribe_Values.Look<float>(ref this.severity, "severity", 1.0f);
        }
    }
}
