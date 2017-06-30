using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;


namespace AbilityUser
{
    // register each class that inherits from CompAbilityUser so their callbacks are called
    // then use standard call when generating a pawn to create their  CompAbilityUser


    public static class AbilityUserUtility
    {
        public static List<Type> abilityUserChildren = null;
        public static List<Type> GetAllChildrenOf(Type pType)
        {
            List<Type> retval = new List<Type>();
            var asslist = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            if (asslist != null)
            {
                foreach (System.Reflection.Assembly ass in asslist)
                {
                    if (ass != null)
                    {
                        var asschildren = ass.GetTypes().Where(t => t.IsClass && t != pType && pType.IsAssignableFrom(t)).ToList();
                        if (asschildren != null) retval.AddRange(asschildren);
                    }
                }
            }
            return retval;
        }


        public static bool TransformPawn(Pawn p)
        {
            bool retval = false;
            if (AbilityUserUtility.abilityUserChildren == null)
            {
                AbilityUserUtility.abilityUserChildren = AbilityUserUtility.GetAllChildrenOf(typeof(CompAbilityUser));
            }

            foreach (Type t in AbilityUserUtility.abilityUserChildren)
            {
                bool st = true;
                /*
                // this code does a check, but since there is no good way to create triggers when specific events occur to
                // add the CompAbilityUser to a Pawn, this just adds them and then checks them on each CompTick.
                */
                if (st)
                {
                    retval = true;
                    ThingComp thingComp = (ThingComp)Activator.CreateInstance((t));
                    thingComp.parent = p;
                    object comps = AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(p);
                    if (comps != null)
                    {
                        ((List<ThingComp>)comps).Add(thingComp);
                    }
                    thingComp.Initialize(null);
                }
            }
            return retval;
        }
    }
}
