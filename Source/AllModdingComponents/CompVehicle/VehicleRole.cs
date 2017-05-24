using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompVehicle
{
    public class VehicleRole
    {
        public string label = "driver";
        public string labelPlural = "drivers";
        public bool handlesMovement = false;
        public bool handlesWeapons = false;
        public int slots = 1;
        public int slotsToOperate = 1;
        public string slotTag = "DriverSeat";
        public List<PawnGenOption> preferredHandlers = new List<PawnGenOption>();
    }
}
