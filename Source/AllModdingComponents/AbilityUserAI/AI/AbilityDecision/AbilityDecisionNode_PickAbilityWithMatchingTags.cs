using System.Collections.Generic;
using System.Linq;
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

        private AbilityAIDef[] eligibleAbilities;

        public override void Resolve(AbilityUserAIProfileDef def)
        {
            //Cache all eligible abilities for given def.
            blacklistedTagSet ??= new HashSet<string>(blacklistedTags);
            tagSet ??= new HashSet<string>(tags);
            eligibleAbilities =
            (
                from validAbilityDef in def.abilities
                //Initial filtering.
                where tagSet.IsSubsetOf(validAbilityDef.tags)
                //Blacklist filtering.
                where !blacklistedTagSet.Overlaps(validAbilityDef.tags)
                select validAbilityDef
            ).ToArray();
            //Log.Message(this + " eligibleAbilities: " + eligibleAbilities.ToStringSafeEnumerable());
            base.Resolve(def);
        }

        public override AbilityAIDef TryPickAbility(Pawn caster)
        {
            if (eligibleAbilities.Length > 0)
            {
                //Filter for abilities the caster has.
                var compAbilityUser = caster.GetExactCompAbilityUser(profileDef.compAbilityUserClass);
                if (compAbilityUser != null)
                {
                    var powers = compAbilityUser.AbilityData.AllPowers;
                    var knownAbilities =
                        from abilityAIDef in eligibleAbilities
                        join abilityUserAbility in powers
                        on abilityAIDef.ability equals abilityUserAbility.Def
                        where profileDef.Worker.CanUseAbility(profileDef, caster, abilityAIDef)
                        orderby abilityAIDef.Worker.PowerScoreFor(abilityAIDef, caster) descending
                        select abilityAIDef;
                    //Log.Message($"{this} for pawn {caster} knownAbilities: " + knownAbilities.ToStringSafeEnumerable());

                    foreach (var ability in knownAbilities)
                    {
                        //Can we cast the ability in the implementation of it?
                        var pawnAbility = powers.Find(power => power.Def == ability.ability);
                        var abilityVerb = pawnAbility.Verb;

                        //Log.Message("abilityVerb=" + abilityVerb.ability.powerdef.defName);
                        if (compAbilityUser.CanCastPowerCheck(abilityVerb,
                                out var reason) /*&& !pawnAbility.NeedsCooldown*/
                        ) //To-Do: Put back check after Ability Framework redesign.
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
