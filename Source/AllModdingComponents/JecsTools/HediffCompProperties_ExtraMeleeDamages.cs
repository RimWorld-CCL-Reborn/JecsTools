using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_ExtraMeleeDamages : HediffCompProperties
    {
        public List<Verse.ExtraDamage> ExtraDamages = new List<Verse.ExtraDamage>();
        public HediffCompProperties_ExtraMeleeDamages()
        {
            compClass = typeof(HediffComp_ExtraMeleeDamages);
        }
    }
}