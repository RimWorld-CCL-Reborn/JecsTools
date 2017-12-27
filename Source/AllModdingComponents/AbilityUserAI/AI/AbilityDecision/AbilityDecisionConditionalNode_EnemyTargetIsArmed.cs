using Verse;

/* 
 * Author: ChJees
 * Created: 2017-11-05
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Checks if the current enemy target is armed.
    /// </summary>
    public class AbilityDecisionConditionalNode_EnemyTargetIsArmed : AbilityDecisionNode
    {
        public override bool CanContinueTraversing(Pawn caster)
        {
            if (caster.mindState.enemyTarget == null)
                return false;

            var enemyPawn = caster.mindState.enemyTarget as Pawn;

            if (enemyPawn == null)
                return false;

            var result = false;
            if (enemyPawn.AnimalOrWildMan())
                result = false;
            else
                result = enemyPawn?.equipment.Primary != null;

            if (invert)
                return !result;

            return result;
        }
    }
}