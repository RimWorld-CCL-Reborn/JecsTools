using System.Collections.Generic;
using AbilityUser;
using Verse;

/*
 * Author: ChJees
 * Created: 2017-09-23
 */

namespace AbilityUserAI
{
    /// <summary>
    ///     Attempt to pick a ability which match these tags.
    /// </summary>
    public class AbilityDecisionNode_PickAbilityWithMatchingTags : AbilityDecisionNode
    {
        /// <summary>
        ///     Tags to NEVER look for.
        /// </summary>
        public List<string> blacklistedTags = new List<string>();

        /// <summary>
        ///     Tags to look for.
        /// </summary>
        public List<string> tags = new List<string>();

        [Unsaved]
        private HashSet<string> blacklistedTagSet;
        private HashSet<string> tagSet;

        private List<AbilityAIDef> eligibleAbilities;

        public override void Resolve(AbilityUserAIProfileDef def)
        {
            //Cache all eligible abilities for given def.
            blacklistedTagSet ??= new HashSet<string>(blacklistedTags);
            tagSet ??= new HashSet<string>(tags);
            eligibleAbilities = new List<AbilityAIDef>();
            foreach (var validAbility in def.abilities)
            {
                if (tagSet.IsSubsetOf(validAbility.tags) &&
                    !blacklistedTagSet.Overlaps(validAbility.tags))
                    eligibleAbilities.Add(validAbility);
            }
            //Log.Message(this + " eligibleAbilities: " + eligibleAbilities.ToStringSafeEnumerable());
            base.Resolve(def);
        }

        public override AbilityAIDef TryPickAbility(Pawn caster)
        {
            if (eligibleAbilities.Count > 0)
            {
                //Filter for abilities the caster has.
                var compAbilityUser = caster.GetExactCompAbilityUser(profileDef.compAbilityUserClass);
                if (compAbilityUser != null)
                {
                    var powers = compAbilityUser.AbilityData.AllPowers;
                    var knownAbilities = new List<AbilityAIDef>();
                    foreach (var ability in eligibleAbilities)
                    {
                        // Can we cast the ability in the implementation of it?
                        if (profileDef.Worker.CanUseAbility(profileDef, caster, ability))
                        {
                            var pawnAbility = powers.Find(power => power.Def == ability.ability);
                            if (pawnAbility != null)
                            {
                                // TODO: Put back check after Ability Framework redesign.
                                if (compAbilityUser.CanCastPowerCheck(pawnAbility.Verb, out var _)) //&& !pawnAbility.NeedsCooldown
                                    knownAbilities.Add(ability);
                            }
                        }
                    }
                    // orderby ability.Worker.PowerScoreFor(ability, caster) descending
                    knownAbilities.Sort((x, y) => y.Worker.PowerScoreFor(y, caster).CompareTo(x.Worker.PowerScoreFor(x, caster)));
                    //Log.Message($"{this} for pawn {caster} knownAbilities: " + knownAbilities.ToStringSafeEnumerable());

                    foreach (var ability in knownAbilities)
                    {
                        if (ability.usedOnCaster)
                        {
                            //Target self.
                            if (ability.CanPawnUseThisAbility(caster, caster))
                                return ability;
                        }
                        else if (ability.needEnemyTarget)
                        {
                            //Enemy target specific.
                            var enemyTarget = caster.mindState.enemyTarget;
                            if (enemyTarget != null && ability.CanPawnUseThisAbility(caster, enemyTarget))
                                return ability;
                        }
                        else
                        {
                            //Need no target.
                            if (ability.CanPawnUseThisAbility(caster, LocalTargetInfo.Invalid))
                                return ability;
                        }
                    }
                }
            }

            return null;
        }

        public override bool CanContinueTraversing(Pawn caster)
        {
            return false;
        }
    }
}
