using System;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-23
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Use abilities constrained within this distance.
    /// </summary>
    public class AbilityDecisionConditionalNode_EnemyTargetDistance : AbilityDecisionNode
    {
        /// <summary>
        ///     Maximum distance at which this is true.
        /// </summary>
        public float maxDistance = 9999.0f;

        /// <summary>
        ///     Minimum distance at which this is true.
        /// </summary>
        public float minDistance = 0.0f;

        public override bool CanContinueTraversing(Pawn caster)
        {
            if (caster.mindState.enemyTarget == null)
                return false;

            var distance = Math.Abs(caster.Position.DistanceTo(caster.mindState.enemyTarget.Position));

            var result = distance >= minDistance && distance < maxDistance;

            if (invert)
                return !result;

            return result;
        }
    }
}