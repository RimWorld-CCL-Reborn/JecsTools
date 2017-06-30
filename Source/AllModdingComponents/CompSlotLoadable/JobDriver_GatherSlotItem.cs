using System.Collections.Generic;
using System.Diagnostics;

namespace CompSlotLoadable
{
    /**
     * Modified JobDriver_Equip
     * Repurposed for loading a slot item.
     */
    public class JobDriver_GatherSlotItem : JobDriver
    {


        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);
            Toil toil = new Toil()
            {
                initAction = delegate
                {
                    this.pawn.pather.StartPath(this.TargetThingA, PathEndMode.ClosestTouch);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            toil.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return toil;
            yield return new Toil
            {
                initAction = delegate
                {
                    Thing itemToGather = this.CurJob.targetA.Thing;
                    //bool flag = false;
                    Thing itemToGatherSplit;
                    if (itemToGather.def.stackLimit > 1 && itemToGather.stackCount > 1)
                    {
                        itemToGatherSplit = itemToGather.SplitOff(1);
                    }
                    else
                    {
                        itemToGatherSplit = itemToGather;
                        //flag = true;
                    }

                    //Find the compslotloadable
                    Pawn_EquipmentTracker pawn_EquipmentTracker = this.pawn.equipment;
                    if (pawn_EquipmentTracker != null)
                    {
                        //Log.Message("2");
                        ThingWithComps thingWithComps = pawn_EquipmentTracker.Primary; //(ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

                        if (thingWithComps != null)
                        {
                            //Log.Message("3");
                            CompSlotLoadable CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                            if (CompSlotLoadable != null)
                            {
                                CompSlotLoadable.TryLoadSlot(itemToGather);
                                if (thingWithComps.def.soundInteract != null)
                                {
                                    thingWithComps.def.soundInteract.PlayOneShot(new TargetInfo(this.pawn.Position, this.pawn.Map, false));
                                }
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
            yield break;
        }
    }
}
