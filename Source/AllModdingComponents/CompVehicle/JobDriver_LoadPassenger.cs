//CompVehicle_LoadPassenger

using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;

namespace CompVehicle
{
    public class JobDriver_LoadPassenger : JobDriver
    {
        private readonly TargetIndex TransporterInd = TargetIndex.A;

        private CompVehicle Vehicle
        {
            get
            {
                var thing = job.GetTarget(TransporterInd).Thing;
                if (thing == null)
                    return null;
                return thing.TryGetComp<CompVehicle>();
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TransporterInd);
            //this.FailOn(() => !this.<> f__this.Transporter.LoadingInProgressOrReadyToLaunch);
            yield return Toils_Reserve.Reserve(TransporterInd, 1, -1, null);
            yield return Toils_Goto.GotoThing(TransporterInd, PathEndMode.Touch);
            yield return new Toil
            {
                initAction = delegate
                {
                    var vehicle = Vehicle;
                    vehicle.Notify_Loaded(pawn);
                }
            };
        }
    }
}