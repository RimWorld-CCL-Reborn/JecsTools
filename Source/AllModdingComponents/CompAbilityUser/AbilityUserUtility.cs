using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace AbilityUser
{
    // register each class that inherits from CompAbilityUser so their callbacks are called
    // then use standard call when generating a pawn to create their  CompAbilityUser


    public static class AbilityUserUtility
    {
        public static readonly List<Type> abilityUserChildren = GenTypes.AllSubclassesNonAbstract(typeof(CompAbilityUser)).ToList();

        public static bool TransformPawn(Pawn p)
        {
            var retval = false;
            foreach (var t in abilityUserChildren)
            {
                var st = true;
                /*
                // this code does a check, but since there is no good way to create triggers when specific events occur to
                // add the CompAbilityUser to a Pawn, this just adds them and then checks them on each CompTick.
                */
                if (st)
                {
                    retval = true;
                    var thingComp = (ThingComp)Activator.CreateInstance(t);
                    thingComp.parent = p;
                    compsField(p)?.Add(thingComp);
                    thingComp.Initialize(null);
                }
            }
            return retval;
        }

        private static readonly AccessTools.FieldRef<ThingWithComps, List<ThingComp>> compsField =
            AccessTools.FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompAbilityUser GetCompAbilityUser(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompAbilityUser comp)
                    return comp;
            }
            return null;
        }

        // ThingWithComps.GetComps<T> is also slow for the same reason, so implementing a specific non-generic version of it here.
        public static IEnumerable<CompAbilityUser> GetCompAbilityUsers(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompAbilityUser comp)
                    yield return comp;
            }
        }

        public static CompAbilityUser GetExactCompAbilityUser(this ThingWithComps thing, Type compClass)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                var comp = comps[i];
                if (comp.GetType() == compClass)
                    return comp as CompAbilityUser;
            }
            return null;
        }

        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompAbilityItem GetCompAbilityItem(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompAbilityItem comp)
                    return comp;
            }
            return null;
        }

        // ThingWithComps.GetComps<T> is also slow for the same reason, so implementing a specific non-generic version of it here.
        public static IEnumerable<CompAbilityItem> GetCompAbilityItems(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompAbilityItem comp)
                    yield return comp;
            }
        }
    }
}
