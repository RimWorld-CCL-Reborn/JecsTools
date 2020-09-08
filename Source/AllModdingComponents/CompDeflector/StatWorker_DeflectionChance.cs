using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace CompDeflector
{
    // TODO: Should also have a StatWorker_ReflectionAccuracy.
    public class StatWorker_DeflectionChance : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (GetDeflector(req) is CompDeflector compDeflector)
                return new CompDeflector.DeflectionChanceCalculator(compDeflector, fixedRandSeed: true).Calculate();
            return 0f;
        }

        private CompDeflector GetDeflector(StatRequest req)
        {
            return req.Thing is Pawn pawn ? GetDeflector(pawn) : null;
        }

        private CompDeflector GetDeflector(Pawn pawn)
        {
            if (pawn.equipment is Pawn_EquipmentTracker equipmentTracker)
            {
                foreach (var equipment in equipmentTracker.AllEquipmentListForReading)
                {
                    if (equipment?.GetCompDeflector() is CompDeflector compDeflector)
                        return compDeflector;
                }
            }
            return null;
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            // Based off StatWorker_MeleeDPS.
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("StatsReport_DeflectionExplanation".Translate());
            stringBuilder.AppendLine();
            var compDeflector = GetDeflector(req);
            if (compDeflector == null)
            {
                stringBuilder.AppendLine("NoDeflectorEquipped".Translate() + ": " + ValueToString(0f, finalized: false));
            }
            else
            {
                var props = compDeflector.Props;
                var calculator = compDeflector.GetDeflectionChanceCalculator(fixedRandSeed: true);
                stringBuilder.AppendLine("StatsReport_BaseDeflectChance".Translate() + ": " + ValueToString(props.baseDeflectChance, finalized: false));
                if (calculator.UseSkill(out var skill))
                {
                    stringBuilder.AppendLine("StatsReport_DeflectPerSkillLevel".Translate() + ": x" + props.deflectRatePerSkillPoint.ToStringPercent());
                    stringBuilder.AppendLine("StatsReport_Skills".Translate());
                    var skillLevel = skill.Level;
                    stringBuilder.AppendLine($"  {compDeflector.Props.deflectSkill.LabelCap} ({skillLevel}): +" +
                        ValueToString(skillLevel * props.deflectRatePerSkillPoint, finalized: false));
                }
                var calc = calculator.Calculate();
                if (calculator.BeforeInfixValue != calculator.InfixValue)
                {
                    var infixDeclaredType = compDeflector.GetType().GetMethod(nameof(CompDeflector.DeflectionChance_InFix)).DeclaringType;
                    stringBuilder.AppendLine(infixDeclaredType.Name);
                    stringBuilder.AppendLine("  " + ValueToString(calculator.BeforeInfixValue, finalized: false) + " => " +
                        ValueToString(calculator.InfixValue, finalized: false));
                }
                if (calculator.UseManipulation(out var capable))
                {
                    var pawn = compDeflector.GetPawn;
                    stringBuilder.AppendLine("StatsReport_Health".Translate());
                    stringBuilder.Append($"  {PawnCapacityDefOf.Manipulation.GetLabelFor(pawn).CapitalizeFirst()}: x");
                    if (capable)
                        stringBuilder.AppendLine(pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation).ToStringPercent());
                    else
                        stringBuilder.AppendLine($"{0f.ToStringPercent()} ({"Incapable".Translate()})");
                }
            }
            return stringBuilder.ToString();
        }

        public override bool IsDisabledFor(Thing thing)
        {
            return thing is Pawn pawn && GetDeflector(pawn) is CompDeflector compDeflector &&
                compDeflector.GetDeflectionChanceCalculator(fixedRandSeed: true).UseSkill(out var skill) && skill.TotallyDisabled;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest statRequest)
        {
            var compDeflector = GetDeflector(statRequest);
            if (compDeflector != null)
                yield return new Dialog_InfoCard.Hyperlink(compDeflector.parent);
        }
    }
}
