using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

/* 
 * Author: ChJees
 * Created: 2017-11-04
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Helps calculating the final Ability power and targeting location which take in account the density of targeted
    ///     pawns. Is friendly to being extended.
    /// </summary>
    public class AbilityWorker_AreaOfEffect : AbilityWorker
    {
        /// <summary>
        ///     Maximum amount of targets to check near the caster pawn.
        /// </summary>
        public virtual int MaxTargetsToCheck => 30;

        public override float PowerScoreFor(AbilityAIDef abilityDef, Pawn pawn)
        {
            var baseScore = abilityDef.power;

            //Grab enemies \ allies
            var potentionalTargets = new List<Thing>(GrabTargets(abilityDef, pawn,
                CustomGrabTargetsPredicate(abilityDef, pawn), MaxTargetsToCheck));

            //Add self if can target allies.
            if (abilityDef.canTargetAlly)
                potentionalTargets.Add(pawn);

            //Get the highest intersecting target.
            var targetInfos = new List<LocalTargetInfo>();
            foreach (var target in potentionalTargets)
                targetInfos.Add(new LocalTargetInfo(target));

            var bestTarget = AbilityMaths.PickMostRadialIntersectingTarget(targetInfos, abilityDef.abilityRadius);

            //If we found no valid target, return negative power.
            if (bestTarget == LocalTargetInfo.Invalid)
                return -abilityDef.power;

            //Calculate final score from best target.
            var finalScore = baseScore;

            foreach (var targetPawn in AbilityUtility.GetPawnsInsideRadius(bestTarget, pawn.Map,
                abilityDef.abilityRadius,
                predPawn => abilityDef.abilityRadiusNeedSight &&
                            GenSight.LineOfSight(pawn.Position, predPawn.Position, pawn.Map, true) ||
                            abilityDef.abilityRadiusNeedSight == false))
                if (targetPawn.HostileTo(pawn) || targetPawn.AnimalOrWildMan()
                ) //Hostile pawns or animals increase score.
                    finalScore += abilityDef.power;
                else //Friendly pawns decrease score.
                    finalScore -= abilityDef.power;

            //Log.Message("AbilityWorker_AreaOfEffect, finalScore=" + finalScore);
            return finalScore;
        }

        public override LocalTargetInfo TargetAbilityFor(AbilityAIDef abilityDef, Pawn pawn)
        {
            //Grab enemies \ allies
            var potentionalTargets = new List<Thing>(GrabTargets(abilityDef, pawn,
                CustomGrabTargetsPredicate(abilityDef, pawn), MaxTargetsToCheck));

            //Add self if can target allies.
            if (abilityDef.canTargetAlly)
                potentionalTargets.Add(pawn);

            //Get the highest intersecting target.
            var targetInfos = new List<LocalTargetInfo>();
            foreach (var target in potentionalTargets)
                targetInfos.Add(new LocalTargetInfo(target));

            var bestTarget = AbilityMaths.PickMostRadialIntersectingTarget(targetInfos, abilityDef.abilityRadius);

            return bestTarget;
        }

        /// <summary>
        ///     Custom overridable Predicate for classes expanding on this algorithm.
        /// </summary>
        /// <param name="abilityDef">Ability Def to take in account.</param>
        /// <param name="pawn">Caster Pawn.</param>
        /// <returns>Optional predicate.</returns>
        public virtual Predicate<Thing> CustomGrabTargetsPredicate(AbilityAIDef abilityDef, Pawn pawn)
        {
            return null;
        }

        /// <summary>
        ///     Grab all viable target candidates.
        /// </summary>
        /// <param name="abilityDef">Ability Def to take in account.</param>
        /// <param name="pawn">Caster Pawn.</param>
        /// <param name="customPredicate">If set it overrides the default predicate.</param>
        /// <param name="pawnsToTest">How many pawns to test at max before stopping. Default is 30.</param>
        /// <returns>Things that are viable.</returns>
        public virtual IEnumerable<Thing> GrabTargets(AbilityAIDef abilityDef, Pawn pawn,
            Predicate<Thing> customPredicate = null, int pawnsToTest = 30)
        {
            //Make a list of candidates.
            var potentionalTargets = new List<Thing>();
            Predicate<Thing> pawnPredicate = null;

            if (customPredicate != null)
                pawnPredicate = customPredicate;
            else if (abilityDef.canTargetAlly)
                pawnPredicate = delegate(Thing thing)
                {
                    //Count own faction and faction whose goodwill they got above 50% as allies.
                    if (AbilityUtility.AreAllies(pawn, thing))
                        return true;
                    return false;
                };
            else
                pawnPredicate = delegate(Thing thing)
                {
                    var thingPawn = thing as Pawn;

                    //Count anything hostile as a target.
                    if (thingPawn != null)
                        if (!thingPawn.Downed && thing.HostileTo(pawn))
                            return true;
                        else if (thing.HostileTo(pawn))
                            return true;

                    return false;
                };

            //Max 'pawnsToTest' shall we grab.
            for (var i = 0; i < pawnsToTest; i++)
            {
                var grabResult = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell,
                    TraverseParms.For(TraverseMode.NoPassClosedDoors),
                    abilityDef.maxRange,
                    thing => pawn != thing && !potentionalTargets.Contains(thing) &&
                             (thing.Position - pawn.Position).LengthHorizontal >= abilityDef.minRange &&
                             pawnPredicate(thing));

                //If found nothing, then break.
                if (grabResult == null)
                    break;

                potentionalTargets.Add(grabResult);

                yield return grabResult;
            }
        }
    }
}