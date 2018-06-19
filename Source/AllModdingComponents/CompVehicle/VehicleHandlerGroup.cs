using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace CompVehicle
{
    public class VehicleHandlerGroup : IExposable, ILoadReferenceable, IThingHolder
    {
        public ThingOwner<Pawn> handlers;

        //public List<Pawn> handlers = new List<Pawn>();
        public List<BodyPartRecord> occupiedParts;

        public VehicleRole role;

        //private List<Thing> tmpThings = new List<Thing>();
        private List<Pawn> tmpSavedPawns = new List<Pawn>();

        public int uniqueID = -1;
        public Pawn vehicle;

        public VehicleHandlerGroup()
        {
            if (handlers == null)
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
        }

        public VehicleHandlerGroup(Pawn newVehicle)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = newVehicle;
            if (handlers == null)
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = newVehicle;
            role = newRole;
            if (handlers == null)
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole, List<Pawn> newHandlers)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = newVehicle;
            role = newRole;
            if (handlers == null)
                handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            if ((newHandlers?.Count ?? 0) > 0)
                foreach (var p in newHandlers)
                {
                    if (p.Spawned) p.DeSpawn();
                    if (p.holdingOwner != null) p.holdingOwner = null;
                    if (!p.IsWorldPawn()) Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
                }
            handlers.TryAddRangeOrTransfer(newHandlers);
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

        public void ExposeData()
        {
            Scribe_Values.Look(ref uniqueID, "uniqueID", -1);
            Scribe_References.Look(ref vehicle, "vehicle");
            Scribe_Deep.Look(ref role, "role");
            //Scribe_Values.Look<VehicleRole>(ref this.role, "role", null);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                //this.tmpThings.Clear();
                //this.tmpThings.AddRange(this.handlers);
                tmpSavedPawns.Clear();
                tmpSavedPawns.AddRange(handlers.InnerListForReading);
                handlers.RemoveAll(x => x is Pawn);
                //for (int i = 0; i < this.tmpThings.Count; i++)
                //{
                //    Pawn pawn = this.tmpThings[i] as Pawn;
                //    if (pawn != null)
                //    {
                //        this.innerContainer.Remove(pawn);
                //        this.tmpSavedPawns.Add(pawn);
                //    }
                //}
                //this.tmpThings.Clear();
            }
            Scribe_Collections.Look(ref tmpSavedPawns, "tmpSavedPawns", LookMode.Reference);
            Scribe_Deep.Look(ref handlers, "handlers", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving)
            {
                for (var j = 0; j < tmpSavedPawns.Count; j++)
                    handlers.TryAdd(tmpSavedPawns[j], true);
                tmpSavedPawns.Clear();
            }

            //Scribe_Collections.Look<Pawn>(ref this.handlers, "handlers", LookMode.Deep, new object[0]);
        }

        public string GetUniqueLoadID()
        {
            return "VehicleHandlerGroup_" + uniqueID;
        }

        public IThingHolder ParentHolder => vehicle;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return handlers;
        }
    }
}