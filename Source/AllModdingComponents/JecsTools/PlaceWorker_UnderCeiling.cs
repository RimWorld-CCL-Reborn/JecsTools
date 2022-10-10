using Verse;

namespace JecsTools
{
    public class PlaceWorker_UnderCeiling : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
            Thing thingToIgnore = null, Thing thing = null)
        {
            if (!loc.Roofed(map))
                return new AcceptanceReport("JT_PlaceWorker_UnderCeiling".Translate());
            return true;
        }
    }
}
