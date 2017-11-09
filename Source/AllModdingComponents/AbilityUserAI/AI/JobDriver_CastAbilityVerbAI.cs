using AbilityUser;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

/* 
 * Author: ChJees
 * Created: 2017-09-24
 */

namespace AbilityUserAI
{
    /// <summary>
    /// AI version of JobDriver_CastAbilityVerb.
    /// </summary>
    public class JobDriver_CastAbilityVerbAI : JobDriver
    {
        private List<CompAbilityUser> CompAbilityUsers
        {
            get
            {
                List<CompAbilityUser> results = new List<CompAbilityUser>();
                var allCompAbilityUsers = this.pawn.GetComps<CompAbilityUser>();
                if (allCompAbilityUsers.TryRandomElement<CompAbilityUser>(out CompAbilityUser comp))
                {
                    foreach (CompAbilityUser compy in allCompAbilityUsers)
                    {
                        results.Add(compy);
                    }
                }
                return results;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            //yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Verb_UseAbility verb = this.pawn.CurJob.verbToUse as Verb_UseAbility;
            if (this.TargetA.HasThing)
            {
                Toil getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
                yield return getInRangeToil;
            }

            //Find.Targeter.targetingVerb = verb;
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            
            this.AddFinishAction(() =>
            {
                if (this.CompAbilityUsers is List<CompAbilityUser> users && !users.NullOrEmpty())
                {
                    foreach (CompAbilityUser u in users)
                    {
                        u.PostAbilityAttempt(this.pawn, verb.ability.powerdef);
                    }
                }
            });
        }
    }
}
