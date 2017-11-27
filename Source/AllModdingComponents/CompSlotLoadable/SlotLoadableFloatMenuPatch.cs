using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JecsTools;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace CompSlotLoadable
{
    public class SloatLoadbleFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> GetFloatMenus()
        {
            List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> FloatMenus = new List<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();

            _Condition curCondition = new _Condition(_ConditionType.IsType, typeof(ThingWithComps));
            Func<Vector3, Pawn, Thing, List<FloatMenuOption>> curFunc = delegate (Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                List<FloatMenuOption> opts = new List<FloatMenuOption>();
                ThingWithComps slotLoadable = curThing as ThingWithComps;
                if (slotLoadable != null && slotLoadable.GetComp<CompSlotLoadable>() is CompSlotLoadable compSlotLoadable)
                {
                    IntVec3 c = clickPos.ToIntVec3();
                    List<Thing> thingList = c.GetThingList(pawn.Map);

                    foreach (SlotLoadable slot in compSlotLoadable.Slots)
                    {
                        Thing loadableThing = thingList.FirstOrDefault((Thing y) => slot.CanLoad(y.def));
                        if (loadableThing != null)
                        {
                            FloatMenuOption itemSlotLoadable;
                            string labelShort = loadableThing.Label;
                            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                itemSlotLoadable = new FloatMenuOption("CannotEquip".Translate(new object[]
                                {
                                    labelShort
                                    }) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            }
                            else if (!pawn.CanReach(loadableThing, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                itemSlotLoadable = new FloatMenuOption("CannotEquip".Translate(new object[]
                                {
                                        labelShort
                                    }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            }
                            else if (!pawn.CanReserve(loadableThing, 1))
                            {
                                itemSlotLoadable = new FloatMenuOption("CannotEquip".Translate(new object[]
                                {
                                            labelShort
                                    }) + " (" + "ReservedBy".Translate(new object[]
                                    {
                                                pawn.Map.physicalInteractionReservationManager.FirstReserverOf(loadableThing).LabelShort
                                        }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            }
                            else
                            {
                                string text2 = "Equip".Translate(new object[]
                                {
                                                    labelShort
                                    });
                                itemSlotLoadable = new FloatMenuOption(text2, delegate
                                {
                                    loadableThing.SetForbidden(false, true);
                                    pawn.jobs.TryTakeOrderedJob(new Job(DefDatabase<JobDef>.GetNamed("GatherSlotItem"), loadableThing));
                                    MoteMaker.MakeStaticMote(loadableThing.DrawPos, loadableThing.Map, ThingDefOf.Mote_FeedbackEquip, 1f);
                                    //PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                                }, MenuOptionPriority.High, null, null, 0f, null, null);
                            }
                            opts.Add(itemSlotLoadable);
                        }
                    }
                }
                return opts;
            };
            KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>> curSec =
                new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(curCondition, curFunc);
            FloatMenus.Add(curSec);
            return FloatMenus;
        }

    }
}
