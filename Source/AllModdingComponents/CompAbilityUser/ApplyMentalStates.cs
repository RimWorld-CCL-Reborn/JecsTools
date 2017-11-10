using System;
using Verse;

namespace AbilityUser
{
    public class ApplyMentalStates : IExposable
    {
        public MentalStateDef mentalStateDef;
        public float applyChance = 1.0f;

        public ApplyMentalStates() { }

        public void ExposeData()
        {
            Scribe_Defs.Look<MentalStateDef>(ref this.mentalStateDef, "mentalStateDef");
            Scribe_Values.Look<float>(ref this.applyChance, "applyChance", 1.0f);
        }
    }
}
