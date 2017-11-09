using AbilityUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-23
 */

namespace AbilityUserAI
{
    /// <summary>
    /// Attempt to pick a ability which match these tags.
    /// </summary>
    public class AbilityDecisionNode_PickAbilityWithMatchingTags : AbilityDecisionNode
    {
        /// <summary>
        /// Tags to look for.
        /// </summary>
        public List<string> tags = new List<string>();

        /// <summary>
        /// Tags to NEVER look for.
        /// </summary>
        public List<string> blacklistedTags = new List<string>();

        public override AbilityAIDef TryPickAbility(Pawn caster)
        {
            //Get all eligible abilities.
            IEnumerable<AbilityAIDef> abilities = 
                from validAbilityDef in 
                //Initial filtering.
                (from abilityAIDef in profileDef.abilities
               //where tags.FindAll(tag => tags.Contains(tag))?.Count() >= tags.Count
                 where tags.FindAll(tag => abilityAIDef.tags.Contains(tag))?.Count() >= tags.Count
                 select abilityAIDef)
                //Blacklist filtering.
                where !validAbilityDef.tags.Any(tag => blacklistedTags.Contains(tag))
                select validAbilityDef;

            //Debug
            //Log.Message("-=abilities list=-");
            //GenDebug.LogList(abilities);

            if (abilities != null)
            {
                //Filter out abilities we do not have.
                ThingComp thingComp = caster.AllComps.First(comp => comp.GetType() == profileDef.compAbilityUserClass);
                CompAbilityUser compAbilityUser = thingComp as CompAbilityUser;

                IEnumerable<AbilityAIDef> knownAbilities =
                    from abilityAIDef in abilities
                    from abilityUserAbility in compAbilityUser.AbilityData.AllPowers
                    where abilityAIDef.ability == abilityUserAbility.Def && profileDef.Worker.CanUseAbility(profileDef, caster, abilityAIDef)
                    orderby abilityAIDef.Worker.PowerScoreFor(abilityAIDef, caster) descending
                    select abilityAIDef;

                //Debug
                //Log.Message("-=knownAbilities list=-");
                //GenDebug.LogList(knownAbilities);

                if (compAbilityUser != null)
                {
                    if (knownAbilities != null && knownAbilities.Count() > 0)
                    {
                        //Return highest power ability which can be cast.
                        foreach (AbilityAIDef ability in knownAbilities)
                        {
                            string reason = "";

                            //Log.Message("-=AbilityVerbs list=-");
                            //GenDebug.LogList(compAbilityUser.AbilityVerbs);

                            //Can we cast the ability in the implementation of it?
                            PawnAbility pawnAbility = compAbilityUser.AbilityData.AllPowers.First(pawnAbilityInt => pawnAbilityInt.Def == ability.ability);
                            Verb_UseAbility abilityVerb = pawnAbility.Verb; //.First(abilityIntVerb => abilityIntVerb.Ability.Def == ability.ability);

                            //Log.Message("abilityVerb=" + abilityVerb.ability.powerdef.defName);
                            if (compAbilityUser.CanCastPowerCheck(abilityVerb, out reason) /*&& !pawnAbility.NeedsCooldown*/) //To-Do: Put back check after Ability Framework redesign.
                            {
                                //Debug
                                //Log.Message("Can cast abilityVerb=" + abilityVerb.ability.powerdef.defName);

                                if(ability.usedOnCaster)
                                {
                                    //Target self.
                                    if (ability.CanPawnUseThisAbility(caster, caster))
                                        return ability;
                                }
                                else if (ability.needEnemyTarget)
                                {
                                    //Enemy target specific.
                                    if (caster.mindState.enemyTarget != null && ability.CanPawnUseThisAbility(caster, caster.mindState.enemyTarget))
                                        return ability;
                                }
                                else
                                {
                                    //Need no target.
                                    if (ability.CanPawnUseThisAbility(caster, LocalTargetInfo.Invalid))
                                        return ability;
                                }
                            }
                            else
                            {
                                //Debug
                                //Log.Message("Can't cast abilityVerb=" + abilityVerb.ability.powerdef.defName + ", with reason: " + reason.Translate(abilityVerb.caster.ToString()));
                            }
                        }
                    }
                }
            }

            return null;
        }

        public override bool CanContinueTraversing(Pawn caster)
        {
            return false;
        }
    }
}
