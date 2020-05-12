using System.Collections.Generic;
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
            var compSlotLoadable = someThing.TryGetComp<CompSlotLoadable>();
            if (compSlotLoadable?.Slots?.Count > 0) // note: null compared with any number => false
                return compSlotLoadable.Slots;
            return null;
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
            if (slotBonus?.Props?.statModifiers != null)
                foreach (var thisStat in slotBonus.Props.statModifiers)
                    //Log.Message("Check for modding "+stat+"  against "+thisStat.stat);
                    if (thisStat.stat == stat)
                    {
                        //Log.Message("adding in stat "+thisStat.stat+":"+thisStat.value+" to result "+retval);
                        retval += thisStat.value;

                        // apply stats parts from Slottable
                        if (!stat.parts.NullOrEmpty())
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