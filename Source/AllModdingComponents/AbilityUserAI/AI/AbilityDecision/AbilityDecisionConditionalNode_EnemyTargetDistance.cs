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
            var enemyTarget = caster.mindState.enemyTarget;
            if (enemyTarget == null)
                return invert;

            var distance = Math.Abs(caster.Position.DistanceTo(enemyTarget.Position));
            return (distance >= minDistance && distance < maxDistance) ^ invert;
        }
    }
}
