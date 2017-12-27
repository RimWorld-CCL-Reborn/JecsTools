using System;
using System.Collections.Generic;
using Verse;

namespace AbilityUser
{
    public class CompProperties_AbilityItem : CompProperties
    {
        public List<AbilityDef> Abilities = new List<AbilityDef>();

        public Type AbilityUserClass;

        public CompProperties_AbilityItem()
        {
            compClass = typeof(CompAbilityItem);
            AbilityUserClass = typeof(GenericCompAbilityUser); // default
        }
    }
}