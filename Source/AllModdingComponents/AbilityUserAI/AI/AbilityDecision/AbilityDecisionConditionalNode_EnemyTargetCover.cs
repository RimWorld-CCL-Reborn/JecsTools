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
            var enemyTarget = caster.mindState.enemyTarget;
            if (enemyTarget == null)
                return invert;

            var cover = CoverUtility.CalculateOverallBlockChance(enemyTarget.Position, caster.Position, caster.Map);
            return (cover >= minCover && cover < maxCover) ^ invert;
        }
    }
}
