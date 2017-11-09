using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-24
 */

namespace AbilityUserAI
{
    /// <summary>
    /// Compares the casters health.
    /// </summary>
    public class AbilityDecisionConditionalNode_CasterHealth : AbilityDecisionNode
    {
        public float minHealth = 0.0f;
        public float maxHealth = 1.0f;

        public override bool CanContinueTraversing(Pawn caster)
        {
            bool result = caster.HealthScale >= minHealth && caster.health.summaryHealth.SummaryHealthPercent <= maxHealth;

            if (invert)
                return !result;

            return result;
        }
    }
}
