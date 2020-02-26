using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_ExtraMeleeDamages : HediffCompProperties
    {
        public List<ExtraDamage> ExtraDamages = new List<ExtraDamage>();
        public HediffCompProperties_ExtraMeleeDamages()
        {
            compClass = typeof(HediffComp_ExtraMeleeDamages);
        }
    }
}