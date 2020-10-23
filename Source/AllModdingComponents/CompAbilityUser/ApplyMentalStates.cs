using Verse;

namespace AbilityUser
{
    public class ApplyMentalStates : IExposable
    {
        public float applyChance = 1.0f;
        public MentalStateDef mentalStateDef;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref mentalStateDef, nameof(mentalStateDef));
            Scribe_Values.Look(ref applyChance, nameof(applyChance), 1.0f);
        }
    }
}
