using Verse;

/* 
 * Author: ChJees
 * Created: 2017-10-19
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Helps the JobGiver_AIAbilityUser pick out a eligible AbilityUserAIProfileDef to use.
    /// </summary>
    public class AbilityProfileWorker
    {
        /// <summary>
        ///     Checks whether this Profile is valid for the Pawn or not. Returns true if it eligible for use. Default
        ///     implementation only cares about checking for matching Traits.
        /// </summary>
        /// <param name="profileDef">Profile Def to check for.</param>
        /// <param name="pawn">Pawn to check for.</param>
        /// <returns>True if its eligible. False if not.</returns>
        public virtual bool ValidProfileFor(AbilityUserAIProfileDef profileDef, Pawn pawn)
        {
            //Default implementation only cares about checking for matching Traits.
            return profileDef.matchingTraits.Count <= 0 || profileDef.matchingTraits.Count > 0 &&
                   profileDef.matchingTraits.Any(traitDef => pawn.story.traits.HasTrait(traitDef));
        }

        /// <summary>
        ///     First check on whether a Ability can be used or not. Default implementation have no special criterias.
        /// </summary>
        /// <param name="profileDef">Profile Def to check for.</param>
        /// <param name="pawn">Pawn to check for.</param>
        /// <param name="abilityDef">Ability Def to check for.</param>
        /// <returns>True if Ability can be used. False if not.</returns>
        public virtual bool CanUseAbility(AbilityUserAIProfileDef profileDef, Pawn pawn, AbilityAIDef abilityDef)
        {
            return true;
        }
    }
}