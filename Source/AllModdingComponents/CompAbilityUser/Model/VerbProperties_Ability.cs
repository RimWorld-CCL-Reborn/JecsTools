using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbilityUser
{
    public class VerbProperties_Ability : VerbProperties
    {
        public AbilityDef abilityDef;

        public AbilityTargetCategory AbilityTargetCategory = AbilityTargetCategory.TargetThing;

        public bool AlwaysHits = true;

        public bool mustHaveTarget = false;
        public bool refundsPointsAfterFailing = false;

        public bool canCastInMelee = true;

        public List<ExtraDamage> extraDamages = null;

        public List<ApplyHediffs> hediffsToApply = null;
        public bool isViolent = true;

        public List<ApplyMentalStates> mentalStatesToApply = null;

        public bool requiresLineOfSight = true;

        public float SecondsToRecharge = 10.0f;

        public List<StatModifier> statModifiers = null;
        public TargetAoEProperties TargetAoEProperties = null;

        public List<SpawnThings> thingsToSpawn = null;
        public bool tooltipShowExtraDamages = true;
        public bool tooltipShowHediffsToApply = true;
        public bool tooltipShowMentalStatesToApply = true;

        public bool tooltipShowProjectileDamage = true;
    }
}