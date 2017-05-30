using Verse;

namespace CompVehicle
{
    public class Bill_LoadVehicle : IExposable
    {
        public Pawn pawnToLoad;
        public VehicleHandlerGroup group;

        public Bill_LoadVehicle()
        {

        }

        public Bill_LoadVehicle(Pawn newLoad, Pawn newVehicle, VehicleHandlerGroup newGroup)
        {
            this.pawnToLoad = newLoad;
            this.group = newGroup;
        }

        public void ExposeData()
        {
            Scribe_References.Look<Pawn>(ref this.pawnToLoad, "pawnToLoad");
            Scribe_References.Look<VehicleHandlerGroup>(ref this.group, "group");
        }
    }
}
