using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
namespace AbilityUser
{
    public class JobDriver_CastAbilityVerb : JobDriver
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

        public override bool TryMakePreToilReservations()
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Verb_UseAbility verb = this.pawn.CurJob.verbToUse as Verb_UseAbility;
            if (this.TargetA.HasThing)
            {
                Toil getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
                yield return getInRangeToil;
            }

            Find.Targeter.targetingVerb = verb;
            yield return new Toil
            {
                initAction = delegate
                {
                    verb.Ability.PostAbilityAttempt();
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
        }
    }
}
