using System.Collections.Generic;
using Verse;
using Verse.AI;

/* 
 * Author: ChJees
 * Created: 2017-11-05
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Helps calculating the final Ability power and optimal ally to heal.
    /// </summary>
    public class AbilityWorker_HealAlly : AbilityWorker
    {
        public override LocalTargetInfo TargetAbilityFor(AbilityAIDef abilityDef, Pawn pawn)
        {
            var bestPawn = PickBestClosestPawn(abilityDef, pawn);

            if (bestPawn == null)
                return base.TargetAbilityFor(abilityDef, pawn);

            return bestPawn;
        }

        public override bool CanPawnUseThisAbility(AbilityAIDef abilityDef, Pawn pawn, LocalTargetInfo target)
        {
            //If no best pawn was found, then we should not bother using ability.
            var bestPawn = PickBestClosestPawn(abilityDef, pawn);

            if (bestPawn == null)
                return false;

            return base.CanPawnUseThisAbility(abilityDef, pawn, target);
        }

        /// <summary>
        ///     Picks the best candidate Pawn out of up to 10 other.
        /// </summary>
        /// <param name="abilityDef">Ability Def to optionally take in account.</param>
        /// <param name="pawn">Pawn using the Ability.</param>
        /// <returns>Candidate Pawn if found, null if not.</returns>
        public virtual Pawn PickBestClosestPawn(AbilityAIDef abilityDef, Pawn pawn)
        {
            Pawn bestPawn = null;
            var bestHealth = 1f;

            var checkedThings = new List<Thing>();

            //Check 10 closest for optimal target.
            for (var i = 0; i < 10; i++)
            {
                var foundThing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell,
                    TraverseParms.For(TraverseMode.NoPassClosedDoors), abilityDef.maxRange,
                    thing => pawn != thing && !checkedThings.Contains(thing) && AbilityUtility.AreAllies(pawn, thing));

                //Found no valid candidate.
                if (foundThing == null)
                    break;

                checkedThings.Add(foundThing);

                var foundPawn = foundThing as Pawn;

                if (foundPawn != null)
                    if (foundPawn.health.summaryHealth.SummaryHealthPercent < bestHealth)
                    {
                        bestPawn = foundPawn;
                        bestHealth = foundPawn.health.summaryHealth.SummaryHealthPercent;
                    }
            }

            return bestPawn;
        }
    }
}