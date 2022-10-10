using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PawnShields
{
    public class StatPart_ShieldStatFactor : StatPart
    {
        public StatDef shieldStat;

        public override void TransformValue(StatRequest req, ref float val)
        {
            var shield = GetShield(req);
            if (shield != null)
                val *= shield.GetStatValue(shieldStat);
            else
                val = 0;
        }

        public override string ExplanationPart(StatRequest req)
        {
            var shield = GetShield(req);
            var value = shield?.GetStatValue(shieldStat) ?? 0f;
            var text = $"    {shieldStat.LabelCap}: {value.ToStringByStyle(shieldStat.toStringStyle, ToStringNumberSense.Factor)}";
            if (shield == null)
                text += $" ({"ShieldNotEquipped".Translate()})";
            return text;
        }

        public override IEnumerable<Dialog_InfoCard.Hyperlink> GetInfoCardHyperlinks(StatRequest req)
        {
            var shield = GetShield(req);
            if (shield != null)
                yield return new Dialog_InfoCard.Hyperlink(shield);
        }

        private static ThingWithComps GetShield(StatRequest req)
        {
            if (req.Thing is Pawn pawn)
                return pawn.GetShield();
            return null;
        }
    }
}
