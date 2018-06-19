using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace CompVehicle
{
    public class VehicleHandlerTemp : IExposable, ILoadReferenceable
    {
        //public ThingOwner<Pawn> handlers;
        public List<Pawn> handlers = new List<Pawn>();

        public List<BodyPartRecord> occupiedParts;
        public VehicleRole role;

        //private List<Thing> tmpThings = new List<Thing>();
        private List<Pawn> tmpSavedPawns = new List<Pawn>();

        public int uniqueID = -1;
        public Pawn vehicle;

        public VehicleHandlerTemp()
        {
            if (handlers == null)
                handlers = new List<Pawn>(); // ThingOwner<Pawn>(this, false, LookMode.Reference);
        }

        public VehicleHandlerTemp(VehicleHandlerGroup originHandler)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = originHandler.vehicle;
            role = originHandler.role;
            if (handlers == null)
                handlers = new List<Pawn>(); // ThingOwner<Pawn>(this, false, LookMode.Reference);
            if ((originHandler.handlers?.Count ?? 0) > 0)
                foreach (var p in originHandler.handlers)
                {
                    if (p.Spawned) p.DeSpawn();
                    if (p.holdingOwner != null) p.holdingOwner = null;
                    if (!p.IsWorldPawn()) Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
                }
            handlers.AddRange(originHandler.handlers);
            //this.handlers = newHandlers;
        }

//        public List<BodyPartRecord> OccupiedParts
//        {
//            get
//            {
//                if (occupiedParts == null)
//                    occupiedParts = new List<BodyPartRecord>(vehicle.health.hediffSet.GetNotMissingParts(
//                        BodyPartHeight.Undefined,
//                        BodyPartDepth.Undefined
//                    ).ToList().FindAll(x => x.def.tags.Contains(role.slotTag)));
//                return occupiedParts;
//            }
//        }

        public bool AreSlotsAvailable
        {
            get
            {
                var result = true;
                if (role != null)
                    if ((this?.handlers?.Count ?? 0) >= role.slots)
                        result = false;
                return result;
            }
        }

        public IThingHolder ParentHolder => vehicle;

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_References.Look(ref vehicle, "vehicle");
            Scribe_Deep.Look(ref role, "role");
            Scribe_Collections.Look(ref handlers, "handlers", LookMode.Reference);
        }

        public string GetUniqueLoadID()
        {
            return "VehicleHandlerGroup_" + uniqueID;
        }
    }
}