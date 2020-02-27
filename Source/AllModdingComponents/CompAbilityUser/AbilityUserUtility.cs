using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace AbilityUser
{
    // register each class that inherits from CompAbilityUser so their callbacks are called
    // then use standard call when generating a pawn to create their  CompAbilityUser


    public static class AbilityUserUtility
    {
        public static List<Type> abilityUserChildren;

        public static List<Type> GetAllChildrenOf(Type pType)
        {
            var retval = new List<Type>();
            var asslist = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            if (asslist != null)
                foreach (var ass in asslist)
                    if (ass != null)
                    {
                        var asschildren = ass.GetTypes()
                            .Where(t => t.IsClass && t != pType && pType.IsAssignableFrom(t)).ToList();
                        if (asschildren != null) retval.AddRange(asschildren);
                    }
            return retval;
        }


        public static bool TransformPawn(Pawn p)
        {
            var retval = false;
            if (abilityUserChildren == null)
                abilityUserChildren = GetAllChildrenOf(typeof(CompAbilityUser));

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
                    var thingComp = (ThingComp) Activator.CreateInstance(t);
                    thingComp.parent = p;
                    var comps = AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(p);
                    if (comps != null)
                        ((List<ThingComp>) comps).Add(thingComp);
                    thingComp.Initialize(null);
                }
            }
            return retval;
        }
    }
}