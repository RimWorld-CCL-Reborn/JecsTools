using System;
using System.Collections.Generic;
using Verse;

namespace AbilityUser
{
    public class CompProperties_AbilityItem : CompProperties
    {
        public List<AbilityDef> Abilities = new List<AbilityDef>();

        public Type AbilityUserClass= null;

        public CompProperties_AbilityItem()
        {
            this.compClass = typeof(CompAbilityItem);
            this.AbilityUserClass = typeof(AbilityUser.GenericCompAbilityUser); // default
        }

    }
}
