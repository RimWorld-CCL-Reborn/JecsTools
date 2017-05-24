using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompVehicle
{
    public class VehicleHandlerGroup : IExposable
    {
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

        public VehicleHandlerGroup()
        {

        }

        public VehicleHandlerGroup(Pawn newVehicle)
        {
            vehicle = newVehicle;
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole)
        {
            vehicle = newVehicle;
            role = newRole;
        }

        public VehicleHandlerGroup(Pawn newVehicle, VehicleRole newRole, List<Pawn> newHandlers)
        {
            vehicle = newVehicle;
            role = newRole;
            handlers = newHandlers;
        }
        

        public void ExposeData()
        {
            Scribe_References.Look<Pawn>(ref this.vehicle, "vehicle");
            Scribe_Values.Look<VehicleRole>(ref this.role, "role", null);
            Scribe_Collections.Look<Pawn>(ref this.handlers, "handlers", LookMode.Reference);
        }
    }
}
