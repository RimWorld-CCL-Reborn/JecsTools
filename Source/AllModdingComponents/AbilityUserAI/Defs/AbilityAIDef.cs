using System;
using System.Collections.Generic;
using AbilityUser;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-20
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Defines how the AI can interact with a ability.
    /// </summary>
    public class AbilityAIDef : Def
    {
        /// <summary>
        ///     Ability to interact with.
        /// </summary>
        public AbilityDef ability;

        /// <summary>
        ///     Ability radius for Area of Effect and beam-like abilities.
        /// </summary>
        public float abilityRadius = 1f;

        /// <summary>
        ///     Abilities with radius need to be able to "see" their targets from the epicenter in order to be used well.
        /// </summary>
        public bool abilityRadiusNeedSight = true;

        /// <summary>
        ///     Won't cast this ability if the pawn already got one of these applied on themselves.
        /// </summary>
        public List<HediffDef> appliedHediffs = new List<HediffDef>();

        /// <summary>
        ///     Can this ability target a ally?
        /// </summary>
        public bool canTargetAlly = false;

        /// <summary>
        ///     Internal worker class implementation.
        /// </summary>
        private AbilityWorker intWorkerClass;

        /// <summary>
        ///     Maximum allowed range to use this ability in.
        /// </summary>
        public float maxRange = 9999.0f;

        /// <summary>
        ///     Minimum allowed range to use this ability in.
        /// </summary>
        public float minRange = 0.0f;

        /// <summary>
        ///     Do this ability need a target at all to be used?
        /// </summary>
        public bool needEnemyTarget = true;

        /// <summary>
        ///     Do this ability need to see the target to be used?
        /// </summary>
        public bool needSeeingTarget = true;

        /// <summary>
        ///     Relative power this ability got in comparison to other.
        /// </summary>
        public float power = 1.0f;

        /// <summary>
        ///     Tags to use in the AIAbilityWorker to do decision making.
        /// </summary>
        public List<string> tags = new List<string>();

        /// <summary>
        ///     Is this ability used on the caster?
        /// </summary>
        public bool usedOnCaster = false;

        /// <summary>
        ///     Worker class for this profile. Will use default implementation if nothing else is specified.
        /// </summary>
        public Type workerClass = typeof(AbilityWorker);

        /// <summary>
        ///     Worker object for this Ability. Default implementation is only taking in account for single target abilities.
        /// </summary>
        public AbilityWorker Worker
        {
            get
            {
                //Instantiate if null.
                if (intWorkerClass == null)
                    intWorkerClass = (AbilityWorker) Activator.CreateInstance(workerClass);

                return intWorkerClass;
            }
        }

        /// <summary>
        ///     Can the caster use this ability at all?
        /// </summary>
        /// <param name="caster">Caster wanting to use ability.</param>
        /// <param name="target">Target if any to use ability on.</param>
        /// <returns>True if they can, false if not.</returns>
        public bool CanPawnUseThisAbility(Pawn caster, LocalTargetInfo target)
        {
            //if (!appliedHediffs.NullOrEmpty())
            //    return false;

            if (appliedHediffs.Count > 0 &&
                !appliedHediffs.Any(hediffDef => caster.health.hediffSet.HasHediff(hediffDef)))
                return false;

            if (!Worker.CanPawnUseThisAbility(this, caster, target))
                return false;

            if (!needEnemyTarget)
                return true;

            if (!usedOnCaster && target.IsValid)
            {
                var distance = Math.Abs(caster.Position.DistanceTo(target.Cell));
                //Log.Message("CanPawnUseThisAbility.distance=" + distance);

                if (distance < minRange || distance > maxRange)
                    return false;

                //if (needSeeingTarget && !GenSight.LineOfSight(caster.Position, target.Cell, caster.Map))
                if (needSeeingTarget && !AbilityUtility.LineOfSightLocalTarget(caster, target, true))
                    return false;
            }

            //Valid ability to use.
            return true;
        }
    }
}