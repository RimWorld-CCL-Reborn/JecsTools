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
    }
}
