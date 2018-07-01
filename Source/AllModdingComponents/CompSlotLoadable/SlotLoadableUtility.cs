using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CompSlotLoadable
{
    [StaticConstructorOnStartup]
    public static class SlotLoadableUtility
    {
        // Grab slots of the thing if they exists. Returns null if none
        public static List<SlotLoadable> GetSlots(Thing someThing)
        {
            List<SlotLoadable> retval = null;

            if (someThing is ThingWithComps thingWithComps)
            {
                var comp = thingWithComps.AllComps.FirstOrDefault(x => x is CompSlotLoadable);
                if (comp != null)
                {
                    var compSlotLoadable = comp as CompSlotLoadable;

                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                        retval = compSlotLoadable.Slots;
                }
            }

            return retval;
        }


        // Get the thing's modificaiton to stat from it's slots
        public static float CheckThingSlotsForStatAugment(Thing slottedThing, StatDef stat)
        {
            var retval = 0.0f;
            var slots = GetSlots(slottedThing);

            if (slots != null)
                foreach (var slot in slots)
                    if (!slot.IsEmpty())
                    {
                        var slottable = slot.SlotOccupant;
                        retval += DetermineSlottableStatAugment(slottable, stat);
                    }
            return retval;
        }

        
        public static float DetermineSlottableStatAugment(Thing slottable, StatDef stat)
        {
            var retval = 0.0f;
            var slotBonus = slottable.TryGetComp<CompSlottedBonus>();
            if (slotBonus != null)
                if (slotBonus.Props != null)
                    if (slotBonus.Props.statModifiers != null && slotBonus.Props.statModifiers.Count > 0)
                        foreach (var thisStat in slotBonus.Props.statModifiers)
                            //Log.Message("Check for modding "+stat+"  against "+thisStat.stat);
                            if (thisStat.stat == stat)
                            {
                                //Log.Message("adding in stat "+thisStat.stat+":"+thisStat.value+" to result "+retval);
                                retval += thisStat.value;

                                // apply stats parts from Slottable
                                if (stat.parts != null && stat.parts.Count > 0)
                                {
                                    var req = StatRequest.For(slottable);
                                    for (var i = 0; i < stat.parts.Count; i++)
                                        //Log.Message("adding in parts "+stat.parts[i]);
                                        stat.parts[i].TransformValue(req, ref retval);
                                    //Log.Message("added in parts of a stat for result "+retval);
                                }
                            }

            return retval;
        }
    }
}