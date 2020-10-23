using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CompSlotLoadable
{
    // Based off JobDriver_Equip.
    public class JobDriver_GatherSlotItem : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            var toil = new Toil
            {
                initAction = () => pawn.pather.StartPath(TargetThingA, PathEndMode.ClosestTouch),
                defaultCompleteMode = ToilCompleteMode.PatherArrival,
            };
            toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return toil;
            yield return new Toil
            {
                initAction = () =>
                {
                    var itemToGather = job.targetA.Thing;
                    //bool flag = false;
                    var itemToGatherSplit = itemToGather.def.stackLimit > 1 && itemToGather.stackCount > 1
                        ? itemToGather.SplitOff(1)
                        : itemToGather;

                    //Find the compslotloadable
                    if (pawn.equipment.Primary is ThingWithComps primary && primary.GetCompSlotLoadable() is CompSlotLoadable compSlotLoadable)
                    {
                        compSlotLoadable.TryLoadSlot(itemToGather);
                        primary.def.soundInteract?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
                        //if (flag)
                        //    thingWithComps.DeSpawn();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
        }
    }
}
