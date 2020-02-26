using System.Linq;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class PlaceWorker_OnTopOfWalls : PlaceWorker
    {
        // Changed by Tad : Missing two on the overrides
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            if (loc.GetThingList(map).FirstOrDefault(x =>
                    x.def.defName.Contains("Wall")) != null)
                return true;
            return new AcceptanceReport("JT_PlaceWorker_OnTopOfWalls".Translate());
        }
    }
}