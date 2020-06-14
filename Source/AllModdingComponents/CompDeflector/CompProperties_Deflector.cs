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
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "Deflect chance", baseDeflectChance.ToStringPercent(), "", 0, "Determines how often this weapon returns projectiles back at the attacker.");
            }
            else
            {
                var calc = Mathf.Clamp(baseDeflectChance + deflectRatePerSkillPoint * 20, 0f, 1.0f);
                //yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "MaxDeflectChance".Translate(), calc.ToStringPercent(), 0)
                //{
                //    overrideReportText = "MaxDeflectChanceEx".Translate(new object[]
                //    {
                //        deflectSkill.label,
                //        deflectRatePerSkillPoint.ToStringPercent(),
                //        calc.ToStringPercent()
                //    }),
                //};
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "Max deflect chance", calc.ToStringPercent(), "", 0, "For each point in " + deflectSkill.label + ", the user gains a " +
                                         deflectRatePerSkillPoint.ToStringPercent() +
                                         " chance of deflecting the projectile back the target. " +
                                         calc.ToStringPercent() + " is the maximum possible deflection chance.");

                //yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "DeflectChancePerLevel".Translate(new object[] { deflectSkill.label }), deflectRatePerSkillPoint.ToStringPercent(), 0)
                //{
                //    overrideReportText = "DeflectChancePerLevelEx".Translate(new object[] { deflectSkill.label })
                //};
                //yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "DeflectChancePerLevel".Translate(new object[] { deflectSkill.label }), deflectRatePerSkillPoint.ToStringPercent(), 0)
                //{
                //    overrideReportText = "DeflectChancePerLevelEx".Translate(new object[] { deflectSkill.label })
                //};
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon, "Deflect % per " + deflectSkill.label + " skill", deflectRatePerSkillPoint.ToStringPercent(), "For each level in " + deflectSkill.label +
                                         ", the user gains this much % chance to deflect a projectile.", 0);

            }

            var enumerator2 = PostSpecialDisplayStats().GetEnumerator();
            while (enumerator2.MoveNext())
            {
                var current2 = enumerator2.Current;
                yield return current2;
            }
        }
    }
}