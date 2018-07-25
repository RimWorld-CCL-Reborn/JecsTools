using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CompSlotLoadable
{
    /**
     * Modified JobDriver_Equip
     * Repurposed for loading a slot item.
     */
    public class JobDriver_GatherSlotItem : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            var toil = new Toil
            {
                initAction = delegate { pawn.pather.StartPath(TargetThingA, PathEndMode.ClosestTouch); },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return toil;
            yield return new Toil
            {
                initAction = delegate
                {
                    var itemToGather = job.targetA.Thing;
                    //bool flag = false;
                    Thing itemToGatherSplit;
                    if (itemToGather.def.stackLimit > 1 && itemToGather.stackCount > 1)
                        itemToGatherSplit = itemToGather.SplitOff(1);
                    else
                        itemToGatherSplit = itemToGather;

                    //Find the compslotloadable
                    var pawn_EquipmentTracker = pawn.equipment;
                    if (pawn_EquipmentTracker != null)
                    {
                        //Log.Message("2");
                        var thingWithComps =
                            pawn_EquipmentTracker
                                .Primary; //(ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

                        if (thingWithComps != null)
                        {
                            //Log.Message("3");
                            var CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                            if (CompSlotLoadable != null)
                            {
                                CompSlotLoadable.TryLoadSlot(itemToGather);
                                if (thingWithComps.def.soundInteract != null)
                                    thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map,
                                        false));
                                //if (flag)
                                //{
                                //    thingWithComps.DeSpawn();
                                //}
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}