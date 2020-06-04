using RimWorld;
using Verse;

namespace PawnShields
{
    // XXX: This should be an abstract class. It's only non-abstract for compatibility with stale bundled versions of PawnShields
    // in other mods, and those stale versions have StatDefs that directly reference StatWorker_Shield.
    // RimWorld as of version 1.1 has earlier loaded assemblies and later loaded XMLs have higher precedence, leading to a situation
    // where a user can end up loading this version of PawnShields.dll and an outdated version of the StatDefs that still directly
    // references StatWorker_Shield. So StatWorker_Shield must be a concrete class rather than an abstract one.
    public class StatWorker_Shield : StatWorker
    {
        public override void FinalizeValue(StatRequest req, ref float val, bool applyPostProcess)
        {
            if (IsDisabledForShield(req))
                val = 0f;
            else
                base.FinalizeValue(req, ref val, applyPostProcess);
        }

        public override string GetExplanationFinalizePart(StatRequest req, ToStringNumberSense numberSense, float finalVal)
        {
            var text = base.GetExplanationFinalizePart(req, numberSense, finalVal);
            if (IsDisabledForShield(req))
                text += $" ({GetDisabledExplanation()})";
            return text;
        }

        // This is needed to prevent "base value" from being shown for disabled stats.
        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            if (IsDisabledForShield(req))
                return "";
            else
                return base.GetExplanationUnfinalized(req, numberSense);
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            if (req.Def is ThingDef def && def.HasComp(typeof(CompShield)))
                return true;
            return false;
        }

        private bool IsDisabledForShield(StatRequest req)
        {
            if (req.Def is ThingDef def)
            {
                var shieldProps = def.GetCompProperties<CompProperties_Shield>();
                if (shieldProps != null && IsDisabledForShield(shieldProps))
                    return true;
            }
            return false;
        }

        protected virtual bool IsDisabledForShield(CompProperties_Shield shieldProps) => false;

        protected virtual string GetDisabledExplanation() => "";
    }

    public class StatWorker_Shield_BaseMeleeBlockChance : StatWorker_Shield
    {
        protected override bool IsDisabledForShield(CompProperties_Shield shieldProps) => !shieldProps.canBlockMelee;

        protected override string GetDisabledExplanation() => "ShieldBlockMeleeNever".Translate();
    }

    public class StatWorker_Shield_BaseRangedBlockChance : StatWorker_Shield
    {
        protected override bool IsDisabledForShield(CompProperties_Shield shieldProps) => !shieldProps.canBlockRanged;

        protected override string GetDisabledExplanation() => "ShieldBlockRangedNever".Translate();
    }

    public class StatWorker_Shield_DamageAbsorbed : StatWorker_Shield
    {
        protected override bool IsDisabledForShield(CompProperties_Shield shieldProps) => !shieldProps.shieldTakeDamage;

        protected override string GetDisabledExplanation() => "ShieldAbsorbDamageNever".Translate();
    }
}
