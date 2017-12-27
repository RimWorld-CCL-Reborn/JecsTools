using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CompVehicle
{
    public class VehicleRole : IExposable
    {
        public HandlingTypeFlags handlingTypes = HandlingTypeFlags.None;
        public string label = "driver";
        public string labelPlural = "drivers";
        public List<PawnGenOption> preferredHandlers = new List<PawnGenOption>();
        public int slots = 1;
        public int slotsToOperate = 1;
        public string slotTag = "DriverSeat";

        public VehicleRole()
        {
        }

        public VehicleRole(VehicleHandlerGroup group)
        {
            label = group.role.label;
            labelPlural = group.role.labelPlural;
            handlingTypes = group.role.handlingTypes;
            slots = group.role.slots;
            slotsToOperate = group.role.slotsToOperate;
            slotTag = group.role.slotTag;
            preferredHandlers = group.role.preferredHandlers;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref label, "label", "");
            Scribe_Values.Look(ref labelPlural, "labelPlural", "");
            Scribe_Values.Look(ref handlingTypes, "handlingTypes", HandlingTypeFlags.None);
            Scribe_Values.Look(ref slots, "slots", 1);
            Scribe_Values.Look(ref slotsToOperate, "slotsToOperate", 1);
            Scribe_Values.Look(ref slotTag, "slotTag", "DriverSeat");
            Scribe_Collections.Look(ref preferredHandlers, "preferredHandlers", LookMode.Deep);
        }
    }
}