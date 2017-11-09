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
    /// Assists the AI in selecting the most appropiate ability for the ocassion.
    /// </summary>
    public abstract class AIAbilityWorker
    {
        /// <summary>
        /// Checks whether this ability is a viable candidate to use in casting.
        /// </summary>
        /// <param name="caster">Pawn attempting to use a ability.</param>
        /// <param name="abilityAIDef">Def calling this worker.</param>
        /// <param name="desiredTags">The desired tags the caster want. Can be ignored.</param>
        /// <returns>Ability score calculated from how much good this ability would do right now.</returns>
        public abstract float AbilityScore(Pawn caster, AbilityAIDef abilityAIDef, List<string> desiredTags);
    }
}
