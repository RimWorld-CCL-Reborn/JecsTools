using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    //public class PawnAbility : ThingWithComps
    public class PawnAbility : IExposable
    {
        public Pawn pawn;
        private List<ThingComp> comps = new List<ThingComp>();
        public AbilityDef powerdef;
        public CompProperties MainEffectProps;
        public Texture2D PowerButton;
        public int TicksUntilCasting = -1;

        public PawnAbility()
        {

        }

        public PawnAbility(CompAbilityUser comp)
        {
            this.pawn = comp.abilityUser;
        }

        public PawnAbility(Pawn user, AbilityDef pdef)
        {
            this.pawn = user;
            this.powerdef = pdef;
            //this.def = pdef;
            this.PowerButton = pdef.uiIcon;
            //this.InitializePawnComps(user);
            //ThingIDMaker.GiveIDTo(this);
        }

        public void PawnAbilityTick()
        {
            if (TicksUntilCasting > -1) TicksUntilCasting--;
        }

        public virtual string PostAbilityVerbDesc()
        {
            return "";
        }


        public bool CanFire
        {
            get
            {
                if (TicksUntilCasting == -1 || TicksUntilCasting < 0) return true;
                return false;
            }
        }

        public int MaxCastingTicks
        {
            get
            { 
                if (this.powerdef != null)
                {
                    if (this.powerdef.MainVerb != null)
                    {
                        if (this.powerdef.MainVerb.SecondsToRecharge > 0)
                        {
                            return (int)(this.powerdef.MainVerb.SecondsToRecharge * GenTicks.TicksPerRealSecond);
                        }
                    }
                }
                return 120;
            }
        }

        //public void InitializePawnComps(Pawn parent)
        //{
        //    //           //Log.Message("Initializng Pawn Comps");
        //    //           //Log.Message(parent.ToString());
        //    for (int i = 0; i < this.powerdef.comps.Count; i++)
        //    {
        //        ThingComp thingComp = (ThingComp)Activator.CreateInstance(this.powerdef.comps[i].compClass);
        //        //              if (thingComp == null) //Log.Message("NoTHingComp");
        //        thingComp.parent = parent;
        //        // if (this.comps == null) //Log.Message("NoCompslist");

        //        thingComp.Initialize(this.powerdef.comps[i]);
        //        this.comps.Add(thingComp);
        //        parent.AllComps.Add(thingComp);
        //    }
        //}
        

        public void ExposeData()
        {
            //base.ExposeData();
            Scribe_Values.Look<int>(ref this.TicksUntilCasting, "TicksUntilcasting", -1);
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn");
            Scribe_Defs.Look<AbilityDef>(ref this.powerdef, "powerdef");
            //Scribe_Collections.LookList<ThingComp>(ref this.comps, "comps", LookMode.Undefined);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.PowerButton = powerdef.uiIcon;
            }
        }

    }
}



