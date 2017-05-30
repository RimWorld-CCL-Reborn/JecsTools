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
//            Log.Message("new PawnAbility on "+user+" of "+pdef);
            this.pawn = user;
            this.powerdef = pdef;
            this.PowerButton = pdef.uiIcon;
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
