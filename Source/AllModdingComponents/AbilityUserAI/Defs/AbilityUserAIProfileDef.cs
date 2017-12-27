using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

/* 
 * Author: ChJees
 * Created: 2017-09-22
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Defines a AI profile for a CompAbilityUser class.
    /// </summary>
    public class AbilityUserAIProfileDef : Def
    {
        /// <summary>
        ///     Abilities this profile are allowed to use.
        /// </summary>
        public List<AbilityAIDef> abilities = new List<AbilityAIDef>();

        /// <summary>
        ///     Ability user class this profile fits for.
        /// </summary>
        public Type compAbilityUserClass;

        /// <summary>
        ///     The decision tree to use when deciding the ability to use.
        /// </summary>
        public AbilityDecisionNode decisionTree;

        /// <summary>
        ///     Internal worker class implementation.
        /// </summary>
        private AbilityProfileWorker intWorkerClass;

        /// <summary>
        ///     Traits that must match in order for this profile to be valid. If its empty or null the requirement is ignored.
        /// </summary>
        public List<TraitDef> matchingTraits = new List<TraitDef>();

        /// <summary>
        ///     If multiplie valid ability users are present pick the one with the highest priority.
        /// </summary>
        public int priority = 0;

        /// <summary>
        ///     Worker class for this profile. Will use default implementation if nothing else is specified.
        /// </summary>
        public Type workerClass = typeof(AbilityProfileWorker);

        /// <summary>
        ///     Worker object for this Profile. If workerClass is not specified it will use the default implementation which only
        ///     checks for Traits.
        /// </summary>
        public AbilityProfileWorker Worker
        {
            get
            {
                //Instantiate if null.
                if (intWorkerClass == null)
                    intWorkerClass = (AbilityProfileWorker) Activator.CreateInstance(workerClass);

                return intWorkerClass;
            }
        }

        /// <summary>
        ///     All tag weights. The higher weight the better score.
        /// </summary>
        //public List<TagWeight> tagWeights = new List<TagWeight>();
        public override void ResolveReferences()
        {
            base.ResolveReferences();

            //Resolve the decision tree.
            if (decisionTree != null)
                decisionTree.Resolve(this);
        }
    }
}