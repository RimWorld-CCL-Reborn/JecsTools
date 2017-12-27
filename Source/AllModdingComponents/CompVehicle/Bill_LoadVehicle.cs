using Verse;

namespace CompVehicle
{
    public class Bill_LoadVehicle : IExposable
    {
        public VehicleHandlerGroup group;
        public Pawn pawnToLoad;

        public Bill_LoadVehicle()
        {
        }

        public Bill_LoadVehicle(Pawn newLoad, Pawn newVehicle, VehicleHandlerGroup newGroup)
        {
            pawnToLoad = newLoad;
            group = newGroup;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawnToLoad, "pawnToLoad");
            Scribe_References.Look(ref group, "group");
        }
    }
}