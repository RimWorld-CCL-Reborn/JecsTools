using System.Linq;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class PlaceWorker_UnderCeiling : PlaceWorker
    {
        //Tad Changed - Again override was modified with additional checks.
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
                return base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
        }
    }
        //public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        //    Thing thingToIgnore = null)
        //{
        //    if (!loc.Roofed(map))
        //        return new AcceptanceReport("JT_PlaceWorker_UnderCeiling".Translate());
        //    return true;
        //}
}