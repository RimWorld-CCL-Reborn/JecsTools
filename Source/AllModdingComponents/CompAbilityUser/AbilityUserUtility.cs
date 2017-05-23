using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;
using Harmony;


namespace AbilityUser
{
    // register each class that inherits from CompAbilityUser so their callbacks are called
    // then use standard call when generating a pawn to create their  CompAbilityUser


    public static class AbilityUserUtility
    {
        //public static List<AbilityUserTypeTracker> typeTrackers = null ; // = new List<AbilityUserTypeTracker>();
        public static List<Type> abilityUserChildren = null;

        public static List<Type> GetAllChildrenOf(Type pType) {
            List<Type> retval = new List<Type>();
            /*
            List<Assembly> asslist = new List<Assembly>();
            List<AssemblyName> argh = new List<AssemblyName>( System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies() );
            foreach( System.Reflection.AssemblyName an in argh ) {
                System.Reflection.Assembly ass = System.Reflection.Assembly.Load(an.ToString()); asslist.Add(ass); }
            asslist.Add(System.Reflection.Assembly.GetExecutingAssembly());
            */

            List<Assembly> asslist = new List<Assembly>( AppDomain.CurrentDomain.GetAssemblies() );

            //            Log.Message("GetAllChildrenOf 1");
            foreach( System.Reflection.Assembly ass in asslist ) {
                //                Log.Message(" ... assembly "+an.ToString());
                //System.Reflection.Assembly ass = System.Reflection.Assembly.Load(an.ToString());
                if ( ass != null ) {
//                    Log.Message(" ... ass : "+ass.ToString()+" "+ass.GetName().Name);
//                    if ( ass.GetName().Name == "UnificaMagica" ) {
//                        foreach ( Type tt in ass.GetTypes() ) {
//                            Log.Message("   UnificaMagica : "+tt.ToString() );
//                        }
//                    }
                    List<Type> asschildren = ass.GetTypes().Where(t => t.IsClass && t != pType && pType.IsAssignableFrom(t) ).ToList();
                    //                    Log.Message("GetAllChildrenOf 1.2");
//                    if ( asschildren.Count > 0 ) { Log.Message("found "+asschildren.Count+" children in assembly "+ass.FullName); }
                    //                    Log.Message("GetAllChildrenOf 1.3");
                    retval.AddRange(asschildren);
                    //                    Log.Message("GetAllChildrenOf 1.4");
                }
                //                else Log.Message(" ... ass is null");
            }
            //            Log.Message("GetAllChildrenOf 2");

            return retval;
        }


        public static bool TransformPawn(Pawn p) {
            bool retval = false;
//            Log.Message("AbilityUserUtility.TransformPawn(p)");

            // init... grab all child classes
            if ( AbilityUserUtility.abilityUserChildren == null ) {
//                Log.Message("initializing all abilityUserChlildren");
                AbilityUserUtility.abilityUserChildren = AbilityUserUtility.GetAllChildrenOf(typeof(CompAbilityUser));
//                Log.Message("initializing CompAbilityUser children: found "+AbilityUserUtility.abilityUserChildren.Count+" classes");
            }

            foreach ( Type t in AbilityUserUtility.abilityUserChildren) {
                bool st = true;
                /*
                // this code does a check, but since there is no good way to create triggers when specific events occur to
                // add the CompAbilityUser to a Pawn, this just adds them and then checks them on each CompTick.
                bool st = false;
                object shouldtransform = t.GetMethod("TryTransformPawn").Invoke(null,new object[]{p}); // call static method of child class
                if ( shouldtransform is bool ) st = (bool) shouldtransform;
                */
                if ( st ) {
//                    Log.Message(" YES: actually adding in AbilityUser");
                    retval = true;
                    ThingComp thingComp = (ThingComp)Activator.CreateInstance((t));
                    thingComp.parent = p;
                    var comps = AccessTools.Field(typeof(ThingWithComps), "comps").GetValue(p);
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
