using System;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_DamageSoak : HediffCompProperties
    {
        public int damageToSoak = 1;

        public HediffCompProperties_DamageSoak()
        {
            this.compClass = typeof(HediffComp_DamageSoak);
        }
    }
}
