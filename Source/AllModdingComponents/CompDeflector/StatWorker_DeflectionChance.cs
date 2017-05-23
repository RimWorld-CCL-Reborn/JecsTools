using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;

namespace CompDeflector
{
    public class StatWorker_DeflectionChance : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return (this.GetBaseDeflectionChance(req, applyPostProcess) + (this.GetSkillLevel(req, applyPostProcess) * this.GetDeflectPerSkillLevel(req, applyPostProcess)) * this.GetManipulationModifier(req, applyPostProcess));
        }

        private Pawn GetPawn(StatRequest req)
        {
            return req.Thing as Pawn;
        }

        private CompDeflector GetDeflector(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn != null)
            {
                Pawn_EquipmentTracker pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {
                    foreach (ThingWithComps thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                    {
                        if (thingWithComps != null)
                        {
                            ////Log.Message("3");
                            CompDeflector compDeflector = thingWithComps.GetComp<CompDeflector>();
                            if (compDeflector != null)
                            {
                                return compDeflector;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private float GetBaseDeflectionChance(StatRequest req, bool applyPostProcess = true)
        {

            CompDeflector compDeflector = GetDeflector(req);
            if (compDeflector != null)
            {
                return compDeflector.Props.baseDeflectChance;
            }
            return 0f;
        }

        private float GetSkillLevel(StatRequest req, bool applyPostProcess = true)
        {
            CompDeflector compDeflector = GetDeflector(req);
            if (compDeflector != null)
            {
                if (compDeflector.Props.useSkillInCalc)
                {
                    SkillDef skillDef = compDeflector.Props.deflectSkill;
                    if (GetPawn(req).skills != null)
                    {
                        if (GetPawn(req).skills.GetSkill(skillDef) != null)
                        {
                            return (float)GetPawn(req).skills.GetSkill(skillDef).Level;
                        }
                    }

                }
            }
            return 0f;
        }

        private float GetDeflectPerSkillLevel(StatRequest req, bool applyPostProcess = true)
        {
            CompDeflector compDeflector = GetDeflector(req);
            if (compDeflector != null)
            {
                if (compDeflector.Props.useSkillInCalc)
                {
                    return compDeflector.Props.deflectRatePerSkillPoint;
                }
            }
            return 0f;
        }

        private float GetManipulationModifier(StatRequest req, bool applyPostProcess = true)
        {
            Pawn pawn = GetPawn(req);
            if (pawn != null)
            {
                if (pawn.health != null)
                {
                    if (pawn.health.capacities != null)
                    {
                        return pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
                    }
                }
            }
            return 1.0f;
        }

        public override string GetExplanation(StatRequest req, ToStringNumberSense numberSense)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Stat is displayed in the following format:\nDeflection chance equals ( Base chance + ( Skill Level * % per Skill Level) / Manipulation Efficiency\n\n");
            //stringBuilder.AppendLine("StatsReport_DeflectionExplanation".Translate());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Base deflect chance");
            //stringBuilder.AppendLine("StatsReport_BaseDeflectChance".Translate());
            stringBuilder.AppendLine("  " + this.GetBaseDeflectionChance(req, true).ToStringPercent());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Skill level");
            //stringBuilder.AppendLine("StatsReport_SkillLevel".Translate());
            stringBuilder.AppendLine("  " + this.GetSkillLevel(req, true).ToString("0"));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Deflect % per skill level");
            //stringBuilder.AppendLine("StatsReport_DeflectPerSkillLevel".Translate());
            stringBuilder.AppendLine("  " + this.GetDeflectPerSkillLevel(req, true).ToStringPercent("0.##"));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Manipulation modifier");
            //stringBuilder.AppendLine("StatsReport_ManipulationModifier".Translate());
            stringBuilder.AppendLine("  " + this.GetManipulationModifier(req, true).ToStringPercent());
            stringBuilder.AppendLine();
            return stringBuilder.ToString();
        }

        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq)
        {
            return string.Format("{0} (({1} + ( {2} * {3} )) / {4} )", new object[]
            {
                value.ToStringByStyle(stat.toStringStyle, numberSense),
                this.GetBaseDeflectionChance(optionalReq, true).ToStringPercent(),
                this.GetSkillLevel(optionalReq, true).ToString("0"),
                this.GetDeflectPerSkillLevel(optionalReq, true).ToStringPercent("0.##"),
                this.GetManipulationModifier(optionalReq, true).ToStringPercent()
            });
        }
        
    }
}
