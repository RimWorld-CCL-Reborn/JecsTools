using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class DamageSoakSettings
    {
        public int damageToSoak = 1;
        public DamageDef damageType = null;
        public List<DamageDef> damageTypesToExclude = null; //E.g. vampires have a general damage immunity, but not to
        //sunlight and burning damages.
    }
    
    public class HediffCompProperties_DamageSoak : HediffCompProperties
    {
        public List<DamageSoakSettings> settings = null;
        public int damageToSoak = 1;
        public DamageDef damageType = null;
        public List<DamageDef> damageTypesToExclude = null; //E.g. vampires have a general damage immunity, but not to
            
        public HediffCompProperties_DamageSoak()
        {
            compClass = typeof(HediffComp_DamageSoak);
        }
    }
}