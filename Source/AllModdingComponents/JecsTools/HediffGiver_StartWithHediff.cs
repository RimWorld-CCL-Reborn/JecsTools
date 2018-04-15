using Verse;

namespace JecsTools
{
    /// <summary>
    /// Adds a hediff to a character after it is generated/spawned.
    /// Perfect for Alien Race bonuses/modifiers.
    /// Chance option to add for some randomization.
    /// </summary>
    public class HediffGiver_StartWithHediff : HediffGiver
    {
        public float chance = 100.0f;
        public float maleCommonality = 100.0f;
        public float femaleCommonality = 100.0f;
        public HediffExpandedDef expandedDef;
        
        public void GiveHediff(Pawn pawn)
        {
            //If the random number is not within the chance range, exit.
            if (!(chance >= Rand.Range(0.0f, 100.0f))) return;
            //If the gender is male, check the male commonality chance, and if it fails, exit.
            if (pawn.gender == Gender.Male && !(maleCommonality >= Rand.Range(0.0f, 100.0f)))
                return;
            //If the gender is female, check the female commonality chance, and if it fails, exit.
            if (pawn.gender == Gender.Female && !(femaleCommonality >= Rand.Range(0.0f, 100.0f)))
                return;

            if (expandedDef != null)
                HealthUtility.AdjustSeverity(pawn, expandedDef, 1f);
            else
                HealthUtility.AdjustSeverity(pawn, this.hediff, 1f);
            
        }
    }
}