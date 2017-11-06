using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace CompVehicle
{
    /// Component that spawns a vehicle.
    public class CompVehicleSpawner : ThingComp
    {
        /// Get the thing that is creating the vehicle.
        public ThingWithComps Spawner => this.parent;

        /// Use the XML configurations.
        public CompProperties_VehicleSpawner Props => this.props as CompProperties_VehicleSpawner;
        
        /// Adds a right click option to unpack and spawn the vehicle.
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption o in base.CompFloatMenuOptions(selPawn)) yield return o;

            if ((!this.Spawner.DestroyedOrNull() && this.Spawner.Spawned) &&
                !selPawn.DestroyedOrNull() && selPawn.Spawned)
            {
                yield return new FloatMenuOption(String.Format(this.Props.useVerb, this.Spawner.Label), delegate
                {
                    selPawn.jobs.TryTakeOrderedJob(new Verse.AI.Job(DefDatabase<JobDef>.GetNamed("CompVehicle_Assemble"), this.Spawner));
                });
            }
        }

        /// When assembled, be sure to spawn the vehicle and destroy this object.
        public void Notify_Assembled(Pawn assembler)
        {
            Pawn pawn = (Pawn)GenSpawn.Spawn(PawnGenerator.GeneratePawn(this.Props.vehicleToSpawn, assembler.Faction), this.Spawner.PositionHeld, this.Spawner.MapHeld);
            this.Spawner.Destroy(DestroyMode.KillFinalize);
        }

        
    }
}
