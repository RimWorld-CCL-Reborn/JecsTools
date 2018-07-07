using System;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class AbilityDef : Def
    {
        public Type abilityClass = typeof(PawnAbility);
        public VerbProperties_Ability MainVerb;
        public PassiveEffectProperties PassiveProps;

        [Unsaved] public Texture2D uiIcon = BaseContent.BadTex;

        public string uiIconPath;

        public override int GetHashCode()
        {
            return Gen.HashCombineInt(defName.GetHashCode(), "AbilityDef".GetHashCode());
        }

        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (!uiIconPath.NullOrEmpty())
                    uiIcon = ContentFinder<Texture2D>.Get(uiIconPath, true);
                //else if (this.DrawMatSingle != null && this.DrawMatSingle != BaseContent.BadMat)
                //{
                //    this.uiIcon = (Texture2D)this.DrawMatSingle.mainTexture;
                //}
            });
        }

        public Job GetJob(AbilityTargetCategory cat, LocalTargetInfo target)
        {
            switch (cat)
            {
                case AbilityTargetCategory.TargetSelf:
                {
                    return new Job(AbilityDefOf.CastAbilitySelf, target);
                }
                case AbilityTargetCategory.TargetAoE:
                {
                    return new Job(AbilityDefOf.CastAbilityVerb, target);
                }
                case AbilityTargetCategory.TargetThing:
                {
                    return new Job(AbilityDefOf.CastAbilityVerb, target);
                }
                default:
                {
                    return new Job(AbilityDefOf.CastAbilityVerb, target);
                }
            }
        }

        public virtual string GetDescription()
        {
            var result = "";
            var coolDesc = GetBasics();
            var AoEDesc = GetAoEDesc();
            //string postDesc = PostAbilityVerbDesc();
            var desc = new StringBuilder();
            desc.AppendLine(description);
            if (coolDesc != "") desc.AppendLine(coolDesc);
            if (AoEDesc != "") desc.AppendLine(AoEDesc);
            //if (postDesc != "") desc.AppendLine(postDesc);
            result = desc.ToString();
            return result;
        }

        public virtual string GetAoEDesc()
        {
            var result = "";
            var def = MainVerb;
            if (def != null)
                if (def.TargetAoEProperties != null)
                {
                    var s = new StringBuilder();
                    s.AppendLine(StringsToTranslate.AU_AoEProperties);
                    if (def.TargetAoEProperties.targetClass == typeof(Pawn))
                        s.AppendLine("\t" + StringsToTranslate.AU_TargetClass + StringsToTranslate.AU_TargetClass);
                    else
                        s.AppendLine("\t" + StringsToTranslate.AU_TargetClass +
                                     def.TargetAoEProperties.targetClass.ToString().CapitalizeFirst());
                    s.AppendLine("\t" + "Range".Translate() + ": " + def.TargetAoEProperties.range);
                    s.AppendLine("\t" + StringsToTranslate.AU_TargetClass + def.TargetAoEProperties.friendlyFire);
                    s.AppendLine("\t" + StringsToTranslate.AU_AoEMaxTargets + def.TargetAoEProperties.maxTargets);
                    if (def.TargetAoEProperties.startsFromCaster)
                        s.AppendLine("\t" + StringsToTranslate.AU_AoEStartsFromCaster);
                    result = s.ToString();
                }
            return result;
        }

        public string GetBasics()
        {
            var result = "";
            var def = MainVerb;
            if (def != null)
            {
                var s = new StringBuilder();
                s.AppendLine(StringsToTranslate.AU_Cooldown + def.SecondsToRecharge.ToString("N0") + " " +
                             "SecondsLower".Translate());
                switch (def.AbilityTargetCategory)
                {
                    case AbilityTargetCategory.TargetAoE:
                        s.AppendLine(StringsToTranslate.AU_Type + StringsToTranslate.AU_TargetAoE);
                        break;
                    case AbilityTargetCategory.TargetSelf:
                        s.AppendLine(StringsToTranslate.AU_Type + StringsToTranslate.AU_TargetSelf);
                        break;
                    case AbilityTargetCategory.TargetThing:
                        s.AppendLine(StringsToTranslate.AU_Type + StringsToTranslate.AU_TargetThing);
                        break;
                    case AbilityTargetCategory.TargetLocation:
                        s.AppendLine(StringsToTranslate.AU_Type + StringsToTranslate.AU_TargetLocation);
                        break;
                }
                if (def.tooltipShowProjectileDamage)
                    if (def.defaultProjectile != null)
                        if (def.defaultProjectile.projectile != null)
                            if (def.defaultProjectile.projectile.GetDamageAmount(1f) > 0)
                            {
                                s.AppendLine("Damage".Translate() + ": " +
                                             def.defaultProjectile.projectile.GetDamageAmount(1f));
                                s.AppendLine("Damage".Translate() + " " + StringsToTranslate.AU_Type +
                                             def.defaultProjectile.projectile.damageDef.LabelCap);
                            }
                if (def.tooltipShowExtraDamages)
                    if (def.extraDamages != null && def.extraDamages.Count > 0)
                        if (def.extraDamages.Count == 1)
                        {
                            s.AppendLine(StringsToTranslate.AU_Extra + " " + "Damage".Translate() + ": " +
                                         def.extraDamages[0].damage);
                            s.AppendLine(StringsToTranslate.AU_Extra + " " + "Damage".Translate() + " " +
                                         StringsToTranslate.AU_Type + def.extraDamages[0].damageDef.LabelCap);
                        }
                        else
                        {
                            s.AppendLine(StringsToTranslate.AU_Extra + " " + "Damage".Translate() + ": ");
                            foreach (var extraDam in def.extraDamages)
                            {
                                s.AppendLine("\t" + StringsToTranslate.AU_Extra + " " + "Damage".Translate() + " " +
                                             StringsToTranslate.AU_Type + extraDam.damageDef.LabelCap);
                                s.AppendLine("\t" + StringsToTranslate.AU_Extra + " " + "Damage".Translate() + ": " +
                                             extraDam.damage);
                            }
                        }
                if (def.tooltipShowMentalStatesToApply)
                    if (def.mentalStatesToApply != null && def.mentalStatesToApply.Count > 0)
                        if (def.mentalStatesToApply.Count == 1)
                        {
                            s.AppendLine(StringsToTranslate.AU_MentalStateChance + ": " +
                                         def.mentalStatesToApply[0].mentalStateDef.LabelCap + " " +
                                         def.mentalStatesToApply[0].applyChance.ToStringPercent());
                        }
                        else
                        {
                            s.AppendLine(StringsToTranslate.AU_MentalStateChance);
                            foreach (var mentalState in def.mentalStatesToApply)
                                s.AppendLine("\t" + mentalState.mentalStateDef.LabelCap + " " +
                                             mentalState.applyChance.ToStringPercent());
                        }
                if (def.tooltipShowHediffsToApply)
                {
                    if (def.hediffsToApply != null && def.hediffsToApply.Count > 0)
                        if (def.hediffsToApply.Count == 1)
                        {
                            s.AppendLine(StringsToTranslate.AU_EffectChance + def.hediffsToApply[0].hediffDef.LabelCap +
                                         " " + def.hediffsToApply[0].applyChance.ToStringPercent());
                        }
                        else
                        {
                            s.AppendLine(StringsToTranslate.AU_EffectChance);
                            foreach (var hediff in def.hediffsToApply)
                            {
                                float duration = 0;
                                if (hediff.hediffDef.comps != null)
                                    if (hediff.hediffDef.HasComp(typeof(HediffComp_Disappears)))
                                    {
                                        var intDuration =
                                        ((HediffCompProperties_Disappears) hediff.hediffDef.CompPropsFor(
                                            typeof(HediffComp_Disappears))).disappearsAfterTicks.max;
                                        duration = intDuration.TicksToSeconds();
                                    }
                                if (duration == 0)
                                    s.AppendLine("\t" + hediff.hediffDef.LabelCap + " " +
                                                 hediff.applyChance.ToStringPercent());
                                else
                                    s.AppendLine("\t" + hediff.hediffDef.LabelCap + " " +
                                                 hediff.applyChance.ToStringPercent() + " " + duration + " " +
                                                 "SecondsToLower".Translate());
                            }
                        }
                    if (def.burstShotCount > 1)
                        s.AppendLine(StringsToTranslate.AU_BurstShotCount + " " + def.burstShotCount);
                }

                result = s.ToString();
            }
            return result;
        }
    }
}