using System.Collections.Generic;

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

        public PawnAbility(CompAbilityUser comp) => this.pawn = comp.AbilityUser;

        public PawnAbility(Pawn user, AbilityDef pdef)
        {
//            Log.Message("new PawnAbility on "+user+" of "+pdef);
            this.pawn = user;
            this.powerdef = pdef;
            this.PowerButton = pdef.uiIcon;
        }

        public void PawnAbilityTick()
        {
            if (this.TicksUntilCasting > -1) this.TicksUntilCasting--;
        }

        public virtual string PostAbilityVerbDesc() => "";


        public bool CanFire
        {
            get
            {
                if (this.TicksUntilCasting == -1 || this.TicksUntilCasting < 0) return true;
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
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.PowerButton = this.powerdef.uiIcon;

            }
        }

    }
}
