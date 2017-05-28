using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompVehicle
{
    public class VehicleHandlerGroup : IExposable, ILoadReferenceable
    {
        public int uniqueID = -1;
        public Pawn vehicle = null;
        public VehicleRole role = null;
        public List<Pawn> handlers = new List<Pawn>();
        public List<BodyPartRecord> occupiedParts = null;
        public List<BodyPartRecord> OccupiedParts
        {
            get
            {
                if (occupiedParts == null)
                {
                    occupiedParts = new List<BodyPartRecord>(vehicle.health.hediffSet.GetNotMissingParts(
                        BodyPartHeight.Undefined,
                        BodyPartDepth.Undefined
                        ).ToList<BodyPartRecord>().FindAll(((BodyPartRecord x) => x.def.tags.Contains(role.slotTag))));
                }
                return occupiedParts;
            }
        }

        public bool AreSlotsAvailable
        {
            get
            {
                bool result = true;
                if (handlers != null && role != null)
                {
                    if (handlers.Count >= role.slots)
                    {
                        result = false;
                    }
                }
                return result;
            }
        }

        public VehicleHandlerGroup()
        {

        }

        public VehicleHandlerGroup(Pawn newVehicle)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = newVehicle;
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = newVehicle;
            role = newRole;
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole, List<Pawn> newHandlers)
        {
            uniqueID = Find.UniqueIDsManager.GetNextThingID();
            vehicle = newVehicle;
            role = newRole;
            handlers = newHandlers;
        }
        

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.uniqueID, "uniqueID", -1);
            Scribe_References.Look<Pawn>(ref this.vehicle, "vehicle");
            Scribe_Deep.Look<VehicleRole>(ref this.role, "role", new object[0]);
            //Scribe_Values.Look<VehicleRole>(ref this.role, "role", null);
            Scribe_Collections.Look<Pawn>(ref this.handlers, "handlers", LookMode.Deep, new object[0]);
        }

        public string GetUniqueLoadID()
        {
            return "VehicleHandlerGroup_" + this.uniqueID;
        }
    }
}
