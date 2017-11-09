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
    /// Checks the amount of cover the enemy target is in.
    /// </summary>
    public class AbilityDecisionConditionalNode_EnemyTargetCover : AbilityDecisionNode
    {
        /// <summary>
        /// Minimum amount of cover to return true at.
        /// </summary>
        public float minCover = 0.0f;

        /// <summary>
        /// Maximum amount of cover to return true at.
        /// </summary>
        public float maxCover = 1.0f;

        public override bool CanContinueTraversing(Pawn caster)
        {
            if (caster.mindState.enemyTarget == null)
                return false;

            float cover = CoverUtility.CalculateOverallBlockChance(caster.mindState.enemyTarget.Position, caster.Position, caster.Map);

            bool result = cover >= minCover && cover < maxCover;

            if (invert)
                return !result;

            return result;
        }
    }
}
