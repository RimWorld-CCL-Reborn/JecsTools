using RimWorld;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_DamageOverTime : HediffCompProperties
    {
        public DamageDef cycleDamage = DamageDefOf.Bite;
        public int cycleDamageAmt = 1;
        public int cycleInTicks = 30000; //Half a day
        public float spreadChance = 0.0f;
        public float armorPenetration = 0f;

        public HediffCompProperties_DamageOverTime()
        {
            compClass = typeof(HediffCompDamageOverTime);
        }
    }
}