using System.Text;
using RimWorld;
using Verse;

namespace CompDeflector
{
    public class StatWorker_DeflectionChance : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return GetBaseDeflectionChance(req, applyPostProcess) + GetSkillLevel(req, applyPostProcess) *
                   GetDeflectPerSkillLevel(req, applyPostProcess) * GetManipulationModifier(req, applyPostProcess);
        }

        private Pawn GetPawn(StatRequest req)
        {
            return req.Thing as Pawn;
        }

        private CompDeflector GetDeflector(StatRequest req)
        {
            if (req.Thing is Pawn pawn)
            {
                var pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                    foreach (var thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                        if (thingWithComps != null)
                        {
                            ////Log.Message("3");
                            var compDeflector = thingWithComps.GetComp<CompDeflector>();
                            if (compDeflector != null)
                                return compDeflector;
                        }
            }
            return null;
        }

        private float GetBaseDeflectionChance(StatRequest req, bool applyPostProcess = true)
        {
            var compDeflector = GetDeflector(req);
            if (compDeflector != null)
                return compDeflector.Props.baseDeflectChance;
            return 0f;
        }

        private float GetSkillLevel(StatRequest req, bool applyPostProcess = true)
        {
            var compDeflector = GetDeflector(req);
            if (compDeflector != null)
                if (compDeflector.Props.useSkillInCalc)
                {
                    var skillDef = compDeflector.Props.deflectSkill;
                    if (GetPawn(req).skills != null)
                        if (GetPawn(req).skills.GetSkill(skillDef) != null)
                            return GetPawn(req).skills.GetSkill(skillDef).Level;
                }
            return 0f;
        }

        private float GetDeflectPerSkillLevel(StatRequest req, bool applyPostProcess = true)
        {
            var compDeflector = GetDeflector(req);
            if (compDeflector != null)
                if (compDeflector.Props.useSkillInCalc)
                    return compDeflector.Props.deflectRatePerSkillPoint;
            return 0f;
        }

        private float GetManipulationModifier(StatRequest req, bool applyPostProcess = true)
        {
            var pawn = GetPawn(req);
            if (pawn != null)
                if (pawn.health != null)
                    if (pawn.health.capacities != null)
                        return pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
            return 1.0f;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(
                "Stat is displayed in the following format:\nDeflection chance equals ( Base chance + ( Skill Level * % per Skill Level) / Manipulation Efficiency\n\n");
            //stringBuilder.AppendLine("StatsReport_DeflectionExplanation".Translate());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Base deflect chance");
            //stringBuilder.AppendLine("StatsReport_BaseDeflectChance".Translate());
            stringBuilder.AppendLine("  " + GetBaseDeflectionChance(req, true).ToStringPercent());
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Skill level");
            //stringBuilder.AppendLine("StatsReport_SkillLevel".Translate());
            stringBuilder.AppendLine("  " + GetSkillLevel(req, true).ToString("0"));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Deflect % per skill level");
            //stringBuilder.AppendLine("StatsReport_DeflectPerSkillLevel".Translate());
            stringBuilder.AppendLine("  " + GetDeflectPerSkillLevel(req, true).ToStringPercent("0.##"));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Manipulation modifier");
            //stringBuilder.AppendLine("StatsReport_ManipulationModifier".Translate());
            stringBuilder.AppendLine("  " + GetManipulationModifier(req, true).ToStringPercent());
            stringBuilder.AppendLine();
            return stringBuilder.ToString();
        }

        // Tad changed missing additional final parameter on the end, the "finalized" bool.
        public override string GetStatDrawEntryLabel(StatDef stat, float value, ToStringNumberSense numberSense, StatRequest optionalReq, bool finalized = true)
        {
            return string.Format("{0} (({1} + ( {2} * {3} )) / {4} )",
                value.ToStringByStyle(stat.toStringStyle, numberSense),
                GetBaseDeflectionChance(optionalReq, true).ToStringPercent(),
                GetSkillLevel(optionalReq, true).ToString("0"),
                GetDeflectPerSkillLevel(optionalReq, true).ToStringPercent("0.##"),
                GetManipulationModifier(optionalReq, true).ToStringPercent(),
                finalized);
        }
       
    }
}