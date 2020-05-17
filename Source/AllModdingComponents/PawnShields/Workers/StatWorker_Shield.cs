using RimWorld;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Worker class specifically made to show off the snazzy stats for shields.
    /// </summary>
    public class StatWorker_Shield : StatWorker
    {
        public override bool ShouldShowFor(StatRequest req)
        {
            var def = req.Def as ThingDef;
            if (def != null && def.HasComp(typeof(CompShield)))
                return true;
            return false;
        }

    }
}
