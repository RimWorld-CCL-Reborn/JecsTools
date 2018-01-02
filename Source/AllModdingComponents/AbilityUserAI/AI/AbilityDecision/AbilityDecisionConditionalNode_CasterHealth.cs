using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-24
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Compares the casters health.
    /// </summary>
    public class AbilityDecisionConditionalNode_CasterHealth : AbilityDecisionNode
    {
        public float maxHealth = 1.0f;
        public float minHealth = 0.0f;

        public override bool CanContinueTraversing(Pawn caster)
        {
            var result = caster.HealthScale >= minHealth &&
                         caster.health.summaryHealth.SummaryHealthPercent <= maxHealth;

            if (invert)
                return !result;

            return result;
        }
    }
}