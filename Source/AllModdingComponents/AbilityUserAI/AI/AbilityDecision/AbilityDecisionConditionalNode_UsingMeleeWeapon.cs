using Verse;

/*
 * Author: ChJees
 * Created: 2017-09-25
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Checks whether the caster is equipped with a melee weapon or not.
    /// </summary>
    public class AbilityDecisionConditionalNode_UsingMeleeWeapon : AbilityDecisionNode
    {
        public bool countUnarmed = true;

        public override bool CanContinueTraversing(Pawn caster)
        {
            return (!caster?.equipment.Primary?.def.IsRangedWeapon ?? countUnarmed) ^ invert;
        }
    }
}
