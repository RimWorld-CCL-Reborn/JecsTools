using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace AbilityUser
{
    // Registers each class that inherits from CompAbilityUser so their callbacks are called.
    // Also provides CompAbilityUser/CompAbilityItem access utility methods.
    public static class AbilityUserUtility
    {
        // Compatibility note: Must remain a public list in case other mods are accessing it.
        public static readonly List<Type> abilityUserChildren = GenTypes.AllSubclassesNonAbstract(typeof(CompAbilityUser)).ToList();

        public static bool TransformPawn(Pawn p)
        {
            static bool ContainsType(List<ThingComp> comps, int compCount, Type compClass)
            {
                for (var i = 0; i < compCount; i++)
                {
                    if (comps[i].GetType() == compClass)
                        return true;
                }
                return false;
            }

            ref var compsRef = ref compsField(p);
            compsRef ??= new List<ThingComp>();
            var comps = compsRef;
            var compCount = comps.Count; // used in ContainsType to avoid iterating over just-added comps
            foreach (var abilityUserType in abilityUserChildren)
            {
                // Avoid adding the same comp type if the pawn already has it (e.g. defined in XML).
                if (ContainsType(comps, compCount, abilityUserType))
                    continue;

                // This code used to do a TryTransformPawn check, but since there is no good way to create triggers when
                // specific events occur to add the CompAbilityUser to a Pawn, this just adds them (and always returns true),
                // and CompAbilityUser's CompTick calls TryTransformPawn until it succeeds (and calls CompAbilityUser.Initialize()).
                var thingComp = (ThingComp)Activator.CreateInstance(abilityUserType);
                thingComp.parent = p;
                comps.Add(thingComp);
                // Always initialize with null CompProperties, since no XML def (and no way to determine the exact type anyway).
                thingComp.Initialize(null);
                //Log.Message($"AbilityUserUtility.TransformPawn({p}): added comp {thingComp}");
            }
            return true;
        }

        private static readonly AccessTools.FieldRef<ThingWithComps, List<ThingComp>> compsField =
            AccessTools.FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        [Obsolete("This isn't safe to use when there are multiple CompAbilityUsers - " +
            "use HasCompAbilityUser or GetCompAbilityUsers or GetExactCompAbilityUser instead")]
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

        public static bool HasCompAbilityUser(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompAbilityUser)
                    return true;
            }
            return false;
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
