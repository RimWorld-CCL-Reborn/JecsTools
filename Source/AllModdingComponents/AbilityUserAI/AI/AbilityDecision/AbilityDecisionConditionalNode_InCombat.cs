using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-23
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Is the pawn in combat or near hostiles?
    /// </summary>
    public class AbilityDecisionConditionalNode_InCombat : AbilityDecisionNode
    {
        public override bool CanContinueTraversing(Pawn caster)
        {
            var result = caster.mindState.anyCloseHostilesRecently;

            if (invert)
                return !result;

            return result;
        }
    }
}