//CompVehicle_LoadPassenger
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace CompVehicle
{
    public class JobDriver_LoadPassenger : JobDriver
    {
        private TargetIndex TransporterInd = TargetIndex.A;

        public override bool TryMakePreToilReservations()
        {
            return true;
        }

        private CompVehicle Vehicle
        {
            get
            {
                Thing thing = this.job.GetTarget(this.TransporterInd).Thing;
                if (thing == null)
                {
                    return null;
                }
                return thing.TryGetComp<CompVehicle>();
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(this.TransporterInd);
            //this.FailOn(() => !this.<> f__this.Transporter.LoadingInProgressOrReadyToLaunch);
            yield return Toils_Reserve.Reserve(this.TransporterInd, 1, -1, null);
            yield return Toils_Goto.GotoThing(this.TransporterInd, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = delegate
                {
                    CompVehicle vehicle = this.Vehicle;
                    vehicle.Notify_Loaded(this.pawn);
                }
            };
        }
    }
}

