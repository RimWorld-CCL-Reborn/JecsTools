using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CompVehicle
{
    /// Component that spawns a vehicle.
    public class CompVehicleSpawner : ThingComp
    {
        /// Get the thing that is creating the vehicle.
        public ThingWithComps Spawner => parent;

        /// Use the XML configurations.
        public CompProperties_VehicleSpawner Props => props as CompProperties_VehicleSpawner;

        /// Adds a right click option to unpack and spawn the vehicle.
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (var o in base.CompFloatMenuOptions(selPawn)) yield return o;

            if (!Spawner.DestroyedOrNull() && Spawner.Spawned &&
                !selPawn.DestroyedOrNull() && selPawn.Spawned)
                yield return new FloatMenuOption(string.Format(Props.useVerb, Spawner.Label),
                    delegate
                    {
                        selPawn.jobs.TryTakeOrderedJob(new Job(DefDatabase<JobDef>.GetNamed("CompVehicle_Assemble"),
                            Spawner));
                    });
        }

        /// When assembled, be sure to spawn the vehicle and destroy this object.
        public void Notify_Assembled(Pawn assembler)
        {
            var pawn = (Pawn) GenSpawn.Spawn(PawnGenerator.GeneratePawn(Props.vehicleToSpawn, assembler.Faction),
                Spawner.PositionHeld, Spawner.MapHeld);
            Spawner.Destroy(DestroyMode.KillFinalize);
        }
    }
}