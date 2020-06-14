using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class JobDriver_CastAbilitySelf : JobDriver
    {
        public AbilityContext Context => job.count == 1 ? AbilityContext.Player : AbilityContext.AI;

        private List<CompAbilityUser> CompAbilityUsers
        {
            get
            {
                var results = new List<CompAbilityUser>();
                var allCompAbilityUsers = pawn.GetComps<CompAbilityUser>();
                if (allCompAbilityUsers.TryRandomElement(out var comp))
                    foreach (var compy in allCompAbilityUsers)
                        results.Add(compy);
                return results;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var verb = pawn.CurJob.verbToUse as Verb_UseAbility;
            Find.Targeter.targetingSource = verb;
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            yield return new Toil
            {
                initAction = delegate { verb.Ability.PostAbilityAttempt(); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return new Toil
            {
                initAction = delegate
                {
                    if (verb.UseAbilityProps.isViolent)
                    {
                        JobDriver_CastAbilityVerb.CheckForAutoAttack(this.pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}