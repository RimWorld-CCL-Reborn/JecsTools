using System;
using System.Collections.Generic;
using JecsTools;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompSlotLoadable
{
    public class SloatLoadbleFloatMenuPatch : FloatMenuPatch
    {
        public override IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>
            GetFloatMenus()
        {
            var curCondition = new _Condition(_ConditionType.ThingHasComp, typeof(CompSlottedBonus));

            static List<FloatMenuOption> CurFunc(Vector3 clickPos, Pawn pawn, Thing curThing)
            {
                //Log.Message("Patch is loaded");
                var opts = new List<FloatMenuOption>();
                var holders = new List<IThingHolder>();
                pawn.GetChildHolders(holders);
                foreach (var holder in holders)
                {
                    foreach (var item in holder.GetDirectlyHeldThings())
                    {
                        var slots = item.GetSlots();
                        if (slots != null)
                        {
                            foreach (var slot in slots)
                            {
                                var loadableThing = slot.CanLoad(curThing.def) ? curThing : null;
                                if (loadableThing != null)
                                {
                                    FloatMenuOption itemSlotLoadable;
                                    var labelShort = loadableThing.Label;
                                    if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                                    {
                                        itemSlotLoadable = new FloatMenuOption(
                                            "CannotEquip".Translate(labelShort) + " (" + "Incapable".Translate() + ")", null);
                                    }
                                    else if (!pawn.CanReach(loadableThing, PathEndMode.ClosestTouch, Danger.Deadly))
                                    {
                                        itemSlotLoadable = new FloatMenuOption(
                                            "CannotEquip".Translate(labelShort) + " (" + "NoPath".Translate() + ")", null);
                                    }
                                    else if (!pawn.CanReserve(loadableThing, 1))
                                    {
                                        itemSlotLoadable = new FloatMenuOption(
                                            "CannotEquip".Translate(labelShort) + " (" +
                                            "ReservedBy".Translate(pawn.Map.physicalInteractionReservationManager
                                                .FirstReserverOf(loadableThing).LabelShort) + ")", null);
                                    }
                                    else
                                    {
                                        itemSlotLoadable = new FloatMenuOption(
                                            "Equip".Translate(labelShort), () =>
                                            {
                                                loadableThing.SetForbidden(false, true);
                                                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(CompSlotLoadableDefOf.GatherSlotItem, loadableThing));
                                                FleckMaker.Static(loadableThing.DrawPos, loadableThing.Map, FleckDefOf.FeedbackEquip);
                                                //PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                                            }, MenuOptionPriority.High);
                                    }
                                    opts.Add(itemSlotLoadable);
                                }
                            }
                            return opts;
                        }
                    }
                }
                return opts;
            }

            yield return new KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>(curCondition, CurFunc);
        }
    }
}
