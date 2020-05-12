using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompDeflector
{
    public class CompProperties_Deflector : CompProperties
    {
        public float baseDeflectChance = 0.3f;

        public bool canReflect = false;
        public float deflectRatePerSkillPoint = 0.015f;
        public SkillDef deflectSkill;
        public float deflectSkillLearnRate = 250f;
        public SoundDef deflectSound;

        public VerbProperties DeflectVerb;
        public float reflectRatePerSkillPoint = 3f;
        public SkillDef reflectSkill;
        public float reflectSkillLearnRate = 500f;

        public bool useManipulationInCalc = false;

        public bool useSkillInCalc = false;
        
        public CompProperties_Deflector()
        {
            compClass = typeof(CompDeflector);
        }

        public virtual IEnumerable<StatDrawEntry> PostSpecialDisplayStats()
        {
            yield break;
        }

        [DebuggerHidden]
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            if (!useSkillInCalc)
            {
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "DeflectChance".Translate(), baseDeflectChance.ToStringPercent(),
                    "DeflectChanceEx".Translate(), 0);
            }
            else
            {
                var deflectRatePerSkillPointStr = deflectRatePerSkillPoint.ToStringPercent();
                var maxDeflectChanceStr = Mathf.Clamp(baseDeflectChance + deflectRatePerSkillPoint * 20, 0f, 1.0f).ToStringPercent();
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "MaxDeflectChance".Translate(), maxDeflectChanceStr,
                    "MaxDeflectChanceEx".Translate(deflectSkill.label, deflectRatePerSkillPointStr, maxDeflectChanceStr), 0);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "DeflectChancePerLevel".Translate(deflectSkill.label), deflectRatePerSkillPointStr,
                    "DeflectChancePerLevelEx".Translate(deflectSkill.label), 0);
            }

            foreach (var current in PostSpecialDisplayStats())
                yield return current;
        }
    }
}