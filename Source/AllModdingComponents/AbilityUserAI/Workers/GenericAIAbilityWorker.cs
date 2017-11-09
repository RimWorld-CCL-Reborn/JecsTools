using AbilityUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-20
 */

namespace AbilityUserAI
{
    /// <summary>
    /// Most generic implementation of the AI ability worker. Only cares if it can cast the ability.
    /// 
    /// Respects:
    /// desiredTags
    /// </summary>
    public class GenericAIAbilityWorker : AIAbilityWorker
    {
        public override float AbilityScore(Pawn caster, AbilityAIDef abilityAIDef, List<string> desiredTags)
        {
            //Our ability contains All of the desired tags.
            if (abilityAIDef.tags.All(tag => desiredTags.Contains(tag)))
            {
                //Set initial score.
                float score = -1f;

                //Get our ability user.
                CompAbilityUser abilityUser = caster.Abilities();
                if(abilityUser != null)
                {
                    //Get our ability.
                    PawnAbility ability = abilityUser.AllPowers.FirstOrDefault(power => power.powerdef == abilityAIDef.ability);

                    //Its null!? Do not pick us.
                    if (ability == null)
                        return -1f;

                    //Get our ability verb.
                    Verb_UseAbility abilityVerb = abilityUser.AbilityVerbs.FirstOrDefault(verb => verb.ability == ability);

                    //Its null!? Do not pick us.
                    if (abilityVerb == null)
                        return -1f;

                    //Can we even cast it?
                    string reason = "";
                    if(abilityUser.CanCastPowerCheck(abilityVerb, out reason))
                    {
                        //We can cast it. Set our score.
                        score = 10f;
                    }

                    //Return our score.
                    return score * abilityAIDef.power;
                }
            }

            //Our score is negative. So do not pick us.
            return -1f;
        }
    }
}
