using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CompVehicle
{
    public class VehicleHandlerGroup : IExposable, ILoadReferenceable, IThingHolder
    {
        public int uniqueID = -1;
        public Pawn vehicle = null;
        public VehicleRole role = null;
        public ThingOwner<Pawn> handlers;
        //public List<Pawn> handlers = new List<Pawn>();
        public List<BodyPartRecord> occupiedParts = null;
        public List<BodyPartRecord> OccupiedParts
        {
            get
            {
                if (this.occupiedParts == null)
                {
                    this.occupiedParts = new List<BodyPartRecord>(this.vehicle.health.hediffSet.GetNotMissingParts(
                        BodyPartHeight.Undefined,
                        BodyPartDepth.Undefined
                        ).ToList<BodyPartRecord>().FindAll(((BodyPartRecord x) => x.def.tags.Contains(this.role.slotTag))));
                }
                return this.occupiedParts;
            }
        }

        public bool AreSlotsAvailable
        {
            get
            {
                bool result = true;
                if (this.role != null)
                {
                    if ((this?.handlers?.Count ?? 0) >= this.role.slots)
                    {
                        result = false;
                    }
                }
                return result;
            }
        }

        public IThingHolder ParentHolder => this.vehicle;

        public VehicleHandlerGroup()
        {
            if (this.handlers == null)
            {
                this.handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }

        public VehicleHandlerGroup(Pawn newVehicle)
        {
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = newVehicle;
            if (this.handlers == null)
            {
                this.handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole)
        {
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = newVehicle;
            this.role = newRole;
            if (this.handlers == null)
            {
                this.handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole, List<Pawn> newHandlers)
        {
            Log.Message("1");
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = newVehicle;
            this.role = newRole;
            if (this.handlers == null)
            {
                this.handlers = new ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
            if ((newHandlers?.Count ?? 0) > 0)
            {
                foreach (Pawn p in newHandlers)
                {
                    if (p.Spawned) p.DeSpawn();
                    if (p.holdingOwner != null) p.holdingOwner = null;
                    Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.KeepForever);
                }
            }
            this.handlers.TryAddRange(newHandlers);
            //this.handlers = newHandlers;
        }
        

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.uniqueID, "uniqueID", -1);
            Scribe_References.Look<Pawn>(ref this.vehicle, "vehicle");
            Scribe_Deep.Look<VehicleRole>(ref this.role, "role", new object[0]);
            //Scribe_Values.Look<VehicleRole>(ref this.role, "role", null);
            Scribe_Deep.Look<ThingOwner<Pawn>>(ref this.handlers, "handlers", new object[]
            {
                    this
            });
            //Scribe_Collections.Look<Pawn>(ref this.handlers, "handlers", LookMode.Deep, new object[0]);
        }

        public string GetUniqueLoadID() => "VehicleHandlerGroup_" + this.uniqueID;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.handlers;
        }
    }
}
