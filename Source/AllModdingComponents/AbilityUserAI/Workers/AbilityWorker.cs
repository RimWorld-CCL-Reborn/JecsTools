using Verse;
using Verse.AI;

/* 
 * Author: ChJees
 * Created: 2017-10-19
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Helps to calculate the final 'power score' of the Ability and do targeting when used.
    /// </summary>
    public class AbilityWorker
    {
        /// <summary>
        ///     Calculates the final power score for this Ability taking the condition of the Pawn in account. Default
        ///     implementation just returns the power score.
        /// </summary>
        /// <param name="abilityDef">Ability Def for the AI.</param>
        /// <param name="pawn">Pawn to take in account.</param>
        /// <returns>Final calculated score.</returns>
        public virtual float PowerScoreFor(AbilityAIDef abilityDef, Pawn pawn)
        {
            return abilityDef.power;
        }

        /// <summary>
        ///     Figures out the best location to use this Ability at. Default implementation returns the enemy target, closest ally
        ///     or the caster.
        /// </summary>
        /// <param name="abilityDef">Ability Def for the AI.</param>
        /// <param name="pawn">Pawn to take in account.</param>
        /// <returns>Targeting location or Pawn.</returns>
        public virtual LocalTargetInfo TargetAbilityFor(AbilityAIDef abilityDef, Pawn pawn)
        {
            if (abilityDef.usedOnCaster)
                return pawn;
            if (abilityDef.canTargetAlly)
            {
                return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell,
                    TraverseParms.For(TraverseMode.NoPassClosedDoors), abilityDef.maxRange,
                    thing => AbilityUtility.AreAllies(pawn, thing));
            }
            if (pawn.mindState.enemyTarget != null && pawn.mindState.enemyTarget is Pawn targetPawn)
            {
                if (!targetPawn.Dead)
                    return pawn.mindState.enemyTarget;
            }
            else
            {
                if (pawn.mindState.enemyTarget != null && !(pawn.mindState.enemyTarget is Corpse))
                    return pawn.mindState.enemyTarget;
            }

            return null;
        }

        /// <summary>
        ///     Final check to say whether the Pawn can use this Ability on the target or self. Default implementation returns true
        ///     for co-ordinates and true if the enemy is NOT Downed.
        /// </summary>
        /// <param name="abilityDef">Ability Def for the AI.</param>
        /// <param name="pawn">Pawn to take in account.</param>
        /// <param name="target">Target this ability is aiming at.</param>
        /// <returns>True if we can use this Ability. False if not.</returns>
        public virtual bool CanPawnUseThisAbility(AbilityAIDef abilityDef, Pawn pawn, LocalTargetInfo target)
        {
            if (target.HasThing)
            {
                var targetPawn = target.Thing as Pawn;

                if (!abilityDef.canTargetAlly)
                    return !targetPawn.Downed;
            }


            return true;
        }
    }
}