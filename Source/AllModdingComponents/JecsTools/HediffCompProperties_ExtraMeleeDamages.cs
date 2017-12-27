using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_ExtraMeleeDamages : HediffCompProperties
    {
        public List<ExtraMeleeDamage> extraDamages = new List<ExtraMeleeDamage>();

        public HediffCompProperties_ExtraMeleeDamages()
        {
            compClass = typeof(HediffComp_ExtraMeleeDamages);
        }
    }
}