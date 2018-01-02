using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_DamageSoak : HediffCompProperties
    {
        public int damageToSoak = 1;
        public DamageDef damageType = null;
        public List<DamageDef> damageTypesToExclude = null; //E.g. vampires have a general damage immunity, but not to
                                                            //sunlight and burning damages.

        public HediffCompProperties_DamageSoak()
        {
            compClass = typeof(HediffComp_DamageSoak);
        }
    }
}