using System.Collections.Generic;
using System.Linq;
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
                if (this.handlers != null && this.role != null)
                {
                    if (this.handlers.Count >= this.role.slots)
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
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = newVehicle;
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole)
        {
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = newVehicle;
            this.role = newRole;
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole, List<Pawn> newHandlers)
        {
            this.uniqueID = Find.UniqueIDsManager.GetNextThingID();
            this.vehicle = newVehicle;
            this.role = newRole;
            this.handlers = newHandlers;
        }
        

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.uniqueID, "uniqueID", -1);
            Scribe_References.Look<Pawn>(ref this.vehicle, "vehicle");
            Scribe_Deep.Look<VehicleRole>(ref this.role, "role", new object[0]);
            //Scribe_Values.Look<VehicleRole>(ref this.role, "role", null);
            Scribe_Collections.Look<Pawn>(ref this.handlers, "handlers", LookMode.Deep, new object[0]);
        }

        public string GetUniqueLoadID() => "VehicleHandlerGroup_" + this.uniqueID;
    }
}
