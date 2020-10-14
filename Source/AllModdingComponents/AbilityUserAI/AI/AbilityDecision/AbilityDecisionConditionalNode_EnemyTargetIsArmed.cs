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
            if (!(caster.mindState.enemyTarget is Pawn enemyPawn))
                return invert;

            if (enemyPawn.AnimalOrWildMan())
                return invert;

            return (enemyPawn.equipment.Primary != null) ^ invert;
        }
    }
}
