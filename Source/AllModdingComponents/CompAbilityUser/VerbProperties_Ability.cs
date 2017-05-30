using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AbilityUser
{
    public class VerbProperties_Ability : VerbProperties
    {
        public bool isViolent = true;
        
        public AbilityDef abilityDef;

        public List<SpawnThings> thingsToSpawn = null;

        public List<ApplyHediffs> hediffsToApply = null;

        public List<ApplyMentalStates> mentalStatesToApply = null;

        public bool AlwaysHits = true;
        
        public float SecondsToRecharge = 10.0f;
        
        public AbilityTargetCategory AbilityTargetCategory = AbilityTargetCategory.TargetThing;
        public TargetAoEProperties TargetAoEProperties = null;

        public bool tooltipShowProjectileDamage = true;
        public bool tooltipShowExtraDamages = true;
        public bool tooltipShowHediffsToApply = true;
        public bool tooltipShowMentalStatesToApply = true;
        
        public List<StatModifier> statModifiers = null;

        public List<ExtraDamage> extraDamages = null;
    }
}
