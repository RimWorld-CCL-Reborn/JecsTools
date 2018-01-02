using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-20
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Utility functions in assisting with handling abilities.
    /// </summary>
    public static class AbilityUtility
    {
        /// <summary>
        ///     Working list for LineOfSightLocalTarget.
        /// </summary>
        private static readonly List<IntVec3> tempSourceList = new List<IntVec3>();

        /// <summary>
        ///     Gets the first CompAbilityUser. Used for checking if we should bother doing a search for a abilities to cast at
        ///     all.
        /// </summary>
        /// <param name="pawn">Pawn to check.</param>
        /// <returns>Ability user if present. Null if none can be found.</returns>
        public static CompAbilityUser Abilities(this Pawn pawn)
        {
            return pawn.GetComp<CompAbilityUser>();
        }

        /// <summary>
        ///     Gets all profiles from the Def database.
        /// </summary>
        /// <returns>All Def Database profiles.</returns>
        public static IEnumerable<AbilityUserAIProfileDef> Profiles()
        {
            return DefDatabase<AbilityUserAIProfileDef>.AllDefs;
        }

        /// <summary>
        ///     Gets all AI profiles which are eligible for this pawn.
        /// </summary>
        /// <param name="pawn">Pawn to get for.</param>
        /// <returns>Matching profiles.</returns>
        public static IEnumerable<AbilityUserAIProfileDef> EligibleAIProfiles(this Pawn pawn)
        {
            IEnumerable<AbilityUserAIProfileDef> result =
                from matchingProfileDef in

                    //Initial filtering.
                    (from thingComp in pawn.AllComps
                        from profileDef in Profiles()
                        where thingComp.GetType() == profileDef.compAbilityUserClass
                        select profileDef)

                //Finer filtering.
                //where matchingProfileDef.matchingTraits.Count <= 0 || (matchingProfileDef.matchingTraits.Count > 0 && matchingProfileDef.matchingTraits.Any(traitDef => pawn.story.traits.HasTrait(traitDef)))
                where matchingProfileDef.Worker.ValidProfileFor(matchingProfileDef, pawn)
                orderby matchingProfileDef.priority descending
                select matchingProfileDef;

            return result;
        }

        /// <summary>
        ///     Gets all Pawns inside the supplied radius. If any.
        /// </summary>
        /// <param name="center">Radius center.</param>
        /// <param name="map">Map to look in.</param>
        /// <param name="radius">The radius from the center.</param>
        /// <param name="targetPredicate">Optional predicate on each candidate.</param>
        /// <returns>Matching Pawns inside the Radius.</returns>
        public static IEnumerable<Pawn> GetPawnsInsideRadius(LocalTargetInfo center, Map map, float radius,
            Predicate<Pawn> targetPredicate)
        {
            //With no predicate, just grab everything.
            if (targetPredicate == null)
                targetPredicate = thing => true;

            foreach (Pawn pawn in map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn))
                if (AbilityMaths.CircleIntersectionTest(pawn.Position.x, pawn.Position.y, 1f, center.Cell.x,
                        center.Cell.y, radius) && targetPredicate(pawn))
                    yield return pawn;
        }

        /// <summary>
        ///     Convenience function for checking whether the pawns are allies.
        /// </summary>
        /// <param name="first">Initiator pawn that is checking.</param>
        /// <param name="second">Second pawn to check with.</param>
        /// <returns>True if they are allies. False if not.</returns>
        public static bool AreAllies(Thing first, Thing second)
        {
            //If you are yourself, then you are definitely allies.
            if (first == second)
                return true;

            //Null factions are allies.
            if (first.Faction == null && second.Faction == null)
                return true;

            //Be allies if in the same Faction or if the goodwill with the other Faction is abouve 50%.
            if (second.Faction == null)
                return first.Faction == second.Faction;
            return first.Faction == second.Faction || first.Faction.GoodwillWith(second.Faction) >= 0.5f;
        }

        /// <summary>
        ///     LocalTargetInfo friendly variant of AttackTargetFinder.CanSee()
        /// </summary>
        /// <param name="caster">Line caster source.</param>
        /// <param name="target">Target we want to check whether we can see or not.</param>
        /// <param name="skipFirstCell">Skip the first cell from source?</param>
        /// <param name="validator">Validator for obstacles, presumably.</param>
        /// <returns>True if we got Line of Sight, false if not.</returns>
        public static bool LineOfSightLocalTarget(Thing caster, LocalTargetInfo target, bool skipFirstCell = false,
            Func<IntVec3, bool> validator = null)
        {
            //Use default function if we has a Thing.
            //To-do: Get this to work without null errors.
            /*if (target.HasThing && caster != null && target != null)
            {
                if (target.Thing == caster)
                    return true;

                //Log.Message("AttackTargetFinder.CanSee(" + caster?.Map.GetUniqueLoadID() + ", " + target.Thing?.Map.GetUniqueLoadID() + ")");

                return AttackTargetFinder.CanSee(caster, target.Thing);
            }*/


            //Use homebrewed cariant for just a position.
            ShootLeanUtility.LeanShootingSourcesFromTo(caster.Position, target.Cell, caster.Map, tempSourceList);

            //See if we can get target from any source cell.
            if (tempSourceList.Count > 0)
                foreach (var sourceCell in tempSourceList)
                    if (GenSight.LineOfSight(sourceCell, target.Cell, caster.Map, skipFirstCell, validator))
                        return true;

            return false;
        }
    }
}