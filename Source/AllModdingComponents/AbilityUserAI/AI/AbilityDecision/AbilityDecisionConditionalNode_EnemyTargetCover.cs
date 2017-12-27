using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-24
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Checks the amount of cover the enemy target is in.
    /// </summary>
    public class AbilityDecisionConditionalNode_EnemyTargetCover : AbilityDecisionNode
    {
        /// <summary>
        ///     Maximum amount of cover to return true at.
        /// </summary>
        public float maxCover = 1.0f;

        /// <summary>
        ///     Minimum amount of cover to return true at.
        /// </summary>
        public float minCover = 0.0f;

        public override bool CanContinueTraversing(Pawn caster)
        {
            if (caster.mindState.enemyTarget == null)
                return false;

            var cover = CoverUtility.CalculateOverallBlockChance(caster.mindState.enemyTarget.Position, caster.Position,
                caster.Map);

            var result = cover >= minCover && cover < maxCover;

            if (invert)
                return !result;

            return result;
        }
    }
}