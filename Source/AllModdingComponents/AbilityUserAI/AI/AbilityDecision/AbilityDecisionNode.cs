using System.Collections.Generic;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-23
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Helps the AI in decision making over which ability to pick.
    /// </summary>
    public class AbilityDecisionNode
    {
        /// <summary>
        ///     If true it inverts the CanContinueTraversing result.
        /// </summary>
        public bool invert = false;

        /// <summary>
        ///     Parent node this belongs to.
        /// </summary>
        public AbilityDecisionNode parent;

        /// <summary>
        ///     Profile def this belongs to.
        /// </summary>
        public AbilityUserAIProfileDef profileDef;

        /// <summary>
        ///     Sub nodes to iterate through if this one do not return a ability to use.
        /// </summary>
        public List<AbilityDecisionNode> subNodes = new List<AbilityDecisionNode>();

        /// <summary>
        ///     Recursively resolves the entire decision tree. (The lazy non stack friendly way, the trees are not supposed to get
        ///     giant anyway.)
        /// </summary>
        /// <param name="def">Def to set.</param>
        public void Resolve(AbilityUserAIProfileDef def)
        {
            profileDef = def;

            //Debug
            //Log.Message("Resolving for '" + GetType().ToString() + "' for def '" + def.defName + "'");

            foreach (var node in subNodes)
            {
                node.parent = this;
                node.Resolve(def);
            }
        }

        /// <summary>
        ///     Try picking a ability. If it returns null, continue searching.
        /// </summary>
        /// <param name="caster">Pawn attempting to cast a ability.</param>
        /// <returns>Ability Def to use or none if we can't.</returns>
        public virtual AbilityAIDef TryPickAbility(Pawn caster)
        {
            return null;
        }

        /// <summary>
        ///     Can we continue traversing down the decision tree?
        /// </summary>
        /// <param name="caster">Pawn attempting to cast a ability.</param>
        /// <returns>True if we can.</returns>
        public virtual bool CanContinueTraversing(Pawn caster)
        {
            if (invert)
                return false;

            return true;
        }

        /// <summary>
        ///     Recursively travels through this decision node sub nodes. Not stack friendly.
        /// </summary>
        /// <param name="caster">Caster to recurse for.</param>
        /// <returns>Ability if found. Null if none is found.</returns>
        public virtual AbilityAIDef RecursivelyGetAbility(Pawn caster)
        {
            var result = TryPickAbility(caster);

            if (result != null)
                return result;

            if (CanContinueTraversing(caster))
                foreach (var node in subNodes)
                {
                    result = node.RecursivelyGetAbility(caster);

                    if (result != null)
                        return result;
                }

            return null;
        }
    }
}