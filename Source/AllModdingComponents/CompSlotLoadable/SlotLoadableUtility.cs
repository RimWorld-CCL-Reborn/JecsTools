﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CompSlotLoadable
{
    public static class SlotLoadableUtility
    {
        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompSlotLoadable GetCompSlotLoadable(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompSlotLoadable comp)
                    return comp;
            }
            return null;
        }

        public static CompSlotLoadable TryGetCompSlotLoadable(this Thing thing)
        {
            return thing is ThingWithComps thingWithComps ? thingWithComps.GetCompSlotLoadable() : null;
        }

        public static CompSlottedBonus GetCompSlottedBonus(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompSlottedBonus comp)
                    return comp;
            }
            return null;
        }

        public static CompSlottedBonus TryGetCompSlottedBonus(this Thing thing)
        {
            return thing is ThingWithComps thingWithComps ? thingWithComps.GetCompSlottedBonus() : null;
        }

        // Grab slots of the thing if they exists. Returns null if none.
        public static List<SlotLoadable> GetSlots(this Thing thing)
        {
            var slots = thing.TryGetCompSlotLoadable()?.Slots;
            return !slots.NullOrEmpty() ? slots : null;
        }

        public static List<SlotLoadable> GetSlots(this ThingWithComps thing)
        {
            var slots = thing.GetCompSlotLoadable()?.Slots;
            return !slots.NullOrEmpty() ? slots : null;
        }

        // Get the thing's modification to stat from its slots.
        public static float CheckThingSlotsForStatAugment(Thing slottedThing, StatDef stat)
        {
            var retval = 0.0f;
            var slots = slottedThing.GetSlots();
            if (slots != null)
                foreach (var slot in slots)
                {
                    var slotOccupant = slot.SlotOccupant;
                    if (slotOccupant != null)
                        retval += DetermineSlottableStatAugment(slotOccupant, stat);
                }
            return retval;
        }

        public static float DetermineSlottableStatAugment(Thing slottable, StatDef stat)
        {
            var retval = 0.0f;
            var slotBonus = slottable.TryGetCompSlottedBonus();
            if (slotBonus?.Props?.statModifiers is List<StatModifier> statModifiers)
                foreach (var thisStat in statModifiers)
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
