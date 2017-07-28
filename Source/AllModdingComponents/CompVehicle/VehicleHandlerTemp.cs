using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace CompVehicle
{
    public class VehicleHandlerTemp : IExposable, ILoadReferenceable
    {
        public int uniqueID = -1;
        public Pawn vehicle = null;
        public VehicleRole role = null;
        //public ThingOwner<Pawn> handlers;
        public List<Pawn> handlers = new List<Pawn>();
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

        public VehicleHandlerTemp()
        {
            if (this.handlers == null)
            {
                this.handlers = new List<Pawn>(); // ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
        }
        
        public VehicleHandlerTemp(VehicleHandlerGroup originHandler)
        {
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = originHandler.vehicle;
            this.role = originHandler.role;
            if (this.handlers == null)
            {
                this.handlers = new List<Pawn>(); // ThingOwner<Pawn>(this, false, LookMode.Reference);
            }
            if ((originHandler.handlers?.Count ?? 0) > 0)
            {
                foreach (Pawn p in originHandler.handlers)
                {
                    if (p.Spawned) p.DeSpawn();
                    if (p.holdingOwner != null) p.holdingOwner = null;
                    if (!p.IsWorldPawn()) Find.WorldPawns.PassToWorld(p, PawnDiscardDecideMode.Decide);
                }
            }
            this.handlers.AddRange(originHandler.handlers);
            //this.handlers = newHandlers;
        }

        //private List<Thing> tmpThings = new List<Thing>();
        private List<Pawn> tmpSavedPawns = new List<Pawn>();
        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.uniqueID, "uniqueID", -1);
            Scribe_References.Look<Pawn>(ref this.vehicle, "vehicle");
            Scribe_Deep.Look<VehicleRole>(ref this.role, "role", new object[0]);
            Scribe_Collections.Look<Pawn>(ref this.handlers, "handlers", LookMode.Reference, new object[0]);
        }

        public string GetUniqueLoadID() => "VehicleHandlerGroup_" + this.uniqueID;
        
    }
}
