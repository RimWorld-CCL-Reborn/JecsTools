using RimWorld;
using System.Collections.Generic;
using Verse;

namespace CompVehicle
{
    public class VehicleRole : IExposable
    {
        public string label = "driver";
        public string labelPlural = "drivers";
        public HandlingTypeFlags handlingTypes = HandlingTypeFlags.None;
        public int slots = 1;
        public int slotsToOperate = 1;
        public string slotTag = "DriverSeat";
        public List<PawnGenOption> preferredHandlers = new List<PawnGenOption>();

        public VehicleRole()
        {

        }

        public VehicleRole(VehicleHandlerGroup group)
        {
            this.label = group.role.label;
            this.labelPlural = group.role.labelPlural;
            this.handlingTypes = group.role.handlingTypes;
            this.slots = group.role.slots;
            this.slotsToOperate = group.role.slotsToOperate;
            this.slotTag = group.role.slotTag;
            this.preferredHandlers = group.role.preferredHandlers;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref this.label, "label", "");
            Scribe_Values.Look<string>(ref this.labelPlural, "labelPlural", "");
            Scribe_Values.Look<HandlingTypeFlags>(ref this.handlingTypes, "handlingTypes", HandlingTypeFlags.None);
            Scribe_Values.Look<int>(ref this.slots, "slots", 1);
            Scribe_Values.Look<int>(ref this.slotsToOperate, "slotsToOperate", 1);
            Scribe_Values.Look<string>(ref this.slotTag, "slotTag", "DriverSeat");
            Scribe_Collections.Look<PawnGenOption>(ref this.preferredHandlers, "preferredHandlers", LookMode.Deep, new object[0]);
        }
    }
}
