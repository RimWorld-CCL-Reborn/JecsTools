using Verse;

/*
 * Author: ChJees
 * Created: 2017-09-24
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Checks whether the caster is equipped with a ranged weapon or not.
    /// </summary>
    public class AbilityDecisionConditionalNode_UsingRangedWeapon : AbilityDecisionNode
    {
        public override bool CanContinueTraversing(Pawn caster)
        {
            return (caster?.equipment.Primary?.def.IsRangedWeapon ?? false) ^ invert;
        }
    }
}
