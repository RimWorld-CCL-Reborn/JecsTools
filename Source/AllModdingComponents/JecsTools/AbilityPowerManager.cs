using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace AbilityUser
{
    public class AbilityPowerManager : IExposable
    {
        
        private CompAbilityUser CompAbilityUser;
        public List<PawnAbility> powersint = new List<PawnAbility>();
        public List<PawnAbility> Powers = new List<PawnAbility>();


        public AbilityPowerManager(CompAbilityUser CompAbilityUser)
        {
            this.CompAbilityUser = CompAbilityUser;
        }

        public void Initialize()
        {

        }

        
        public void AbilityPowerManagerTick()
        {
        }

        public void AddPawnAbility(AbilityDef psydef)
        {
            if (!this.CompAbilityUser.Powers.Any(x => x.powerdef.defName == psydef.defName))
            {
                this.CompAbilityUser.Powers.Add(new PawnAbility(this.CompAbilityUser.abilityUser, psydef));
            }

            this.CompAbilityUser.UpdateAbilities();
        }

        public void RemovePawnAbility(AbilityDef abilityDef)
        {
            PawnAbility abilityToRemove = this.CompAbilityUser.Powers.FirstOrDefault(x => x.powerdef.defName == abilityDef.defName);
            if (abilityToRemove != null)
            {
                this.CompAbilityUser.Powers.Remove(abilityToRemove);
            }

            this.CompAbilityUser.UpdateAbilities();
        }

        public void ExposeData()
        {
            Scribe_Values.LookValue<CompAbilityUser>(ref this.CompAbilityUser, "CompAbilityUser", null);
        }

    }
}
