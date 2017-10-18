using System;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class AbilityDef : Def
    {
        public string uiIconPath;
        public Type abilityClass = typeof(PawnAbility);
        public VerbProperties_Ability MainVerb;
        public PassiveEffectProperties PassiveProps;

        [Unsaved]
        public Texture2D uiIcon = BaseContent.BadTex;

        public override void PostLoad()
        {
            base.PostLoad();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (!this.uiIconPath.NullOrEmpty())
                {
                    this.uiIcon = ContentFinder<Texture2D>.Get(this.uiIconPath, true);
                }
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
            string result = "";
            string coolDesc = GetBasics();
            string AoEDesc = GetAoEDesc();
            //string postDesc = PostAbilityVerbDesc();
            StringBuilder desc = new StringBuilder();
            desc.AppendLine(this.description);
            if (coolDesc != "") desc.AppendLine(coolDesc);
            if (AoEDesc != "") desc.AppendLine(AoEDesc);
            //if (postDesc != "") desc.AppendLine(postDesc);
            result = desc.ToString();
            return result;
        }

        public virtual string GetAoEDesc()
        {
            string result = "";
            VerbProperties_Ability def = this.MainVerb;
            if (def != null)
            {
                if (def.TargetAoEProperties != null)
                {
                    StringBuilder s = new StringBuilder();
                    s.AppendLine(StringsToTranslate.AU_AoEProperties);
                    if (def.TargetAoEProperties.targetClass == typeof(Pawn))
                        s.AppendLine("\t" + StringsToTranslate.AU_TargetClass + StringsToTranslate.AU_TargetClass);
                    else
                        s.AppendLine("\t" + StringsToTranslate.AU_TargetClass + def.TargetAoEProperties.targetClass.ToString().CapitalizeFirst());
                    s.AppendLine("\t" + "Range".Translate() + ": " + def.TargetAoEProperties.range.ToString());
                    s.AppendLine("\t" + StringsToTranslate.AU_TargetClass + def.TargetAoEProperties.friendlyFire.ToString());
                    s.AppendLine("\t" + StringsToTranslate.AU_AoEMaxTargets + def.TargetAoEProperties.maxTargets.ToString());
                    if (def.TargetAoEProperties.startsFromCaster)
                    {
                        s.AppendLine("\t" + StringsToTranslate.AU_AoEStartsFromCaster);
                    }
                    result = s.ToString();
                }
            }
            return result;
        }

        public string GetBasics()
        {
            string result = "";
            VerbProperties_Ability def = this.MainVerb;
            if (def != null)
            {
                StringBuilder s = new StringBuilder();
                s.AppendLine(StringsToTranslate.AU_Cooldown + def.SecondsToRecharge.ToString("N0") + " " + "SecondsLower".Translate());
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
                {
                    if (def.projectileDef != null)
                    {
                        if (def.projectileDef.projectile != null)
                        {
                            if (def.projectileDef.projectile.damageAmountBase > 0)
                            {
                                s.AppendLine("Damage".Translate() + ": " + def.projectileDef.projectile.damageAmountBase);
                                s.AppendLine("Damage".Translate() + " " + StringsToTranslate.AU_Type + def.projectileDef.projectile.damageDef.LabelCap);
                            }
                        }
                    }
                }
                if (def.tooltipShowExtraDamages)
                {
                    if (def.extraDamages != null && def.extraDamages.Count > 0)
                    {
                        if (def.extraDamages.Count == 1)
                        {
                            s.AppendLine(StringsToTranslate.AU_Extra + " " + "Damage".Translate() + ": " + def.extraDamages[0].damage);
                            s.AppendLine(StringsToTranslate.AU_Extra + " " + "Damage".Translate() + " " + StringsToTranslate.AU_Type + def.extraDamages[0].damageDef.LabelCap);
                        }
                        else
                        {
                            s.AppendLine(StringsToTranslate.AU_Extra + " " + "Damage".Translate() + ": ");
                            foreach (ExtraDamage extraDam in def.extraDamages)
                            {
                                s.AppendLine("\t" + StringsToTranslate.AU_Extra + " " + "Damage".Translate() + " " + StringsToTranslate.AU_Type + extraDam.damageDef.LabelCap);
                                s.AppendLine("\t" + StringsToTranslate.AU_Extra + " " + "Damage".Translate() + ": " + extraDam.damage);
                            }
                        }
                    }
                }
                if (def.tooltipShowMentalStatesToApply)
                {
                    if (def.mentalStatesToApply != null && def.mentalStatesToApply.Count > 0)
                    {
                        if (def.mentalStatesToApply.Count == 1)
                        {
                            s.AppendLine(StringsToTranslate.AU_MentalStateChance + ": " + def.mentalStatesToApply[0].mentalStateDef.LabelCap + " " + def.mentalStatesToApply[0].applyChance.ToStringPercent());
                        }
                        else
                        {
                            s.AppendLine(StringsToTranslate.AU_MentalStateChance);
                            foreach (ApplyMentalStates mentalState in def.mentalStatesToApply)
                            {
                                s.AppendLine("\t" + mentalState.mentalStateDef.LabelCap + " " + mentalState.applyChance.ToStringPercent());
                            }

                        }
                    }
                }
                if (def.tooltipShowHediffsToApply)
                {
                    if (def.hediffsToApply != null && def.hediffsToApply.Count > 0)
                    {
                        if (def.hediffsToApply.Count == 1)
                        {
                            s.AppendLine(StringsToTranslate.AU_EffectChance + def.hediffsToApply[0].hediffDef.LabelCap + " " + def.hediffsToApply[0].applyChance.ToStringPercent());
                        }
                        else
                        {
                            s.AppendLine(StringsToTranslate.AU_EffectChance);
                            foreach (ApplyHediffs hediff in def.hediffsToApply)
                            {
                                float duration = 0;
                                if (hediff.hediffDef.comps != null)
                                {
                                    if (hediff.hediffDef.HasComp(typeof(HediffComp_Disappears)))
                                    {
                                        int intDuration = ((HediffCompProperties_Disappears)hediff.hediffDef.CompPropsFor(typeof(HediffComp_Disappears))).disappearsAfterTicks.max;
                                        duration = GenTicks.TicksToSeconds(intDuration);
                                    }
                                }
                                if (duration == 0) s.AppendLine("\t" + hediff.hediffDef.LabelCap + " " + hediff.applyChance.ToStringPercent());
                                else s.AppendLine("\t" + hediff.hediffDef.LabelCap + " " + hediff.applyChance.ToStringPercent() + " " + duration + " " + "SecondsToLower".Translate());
                            }

                        }
                    }
                    if (def.burstShotCount > 1)
                    {
                        s.AppendLine(StringsToTranslate.AU_BurstShotCount + " " + def.burstShotCount.ToString());
                    }
                }

                result = s.ToString();

            }
            return result;
        }
    }
}
