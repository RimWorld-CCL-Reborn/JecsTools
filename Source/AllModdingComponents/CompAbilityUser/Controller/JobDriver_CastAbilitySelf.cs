using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class JobDriver_CastAbilitySelf : JobDriver
    {
        public AbilityContext Context => job.count == 1 ? AbilityContext.Player : AbilityContext.AI;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            var verb = pawn.CurJob.verbToUse as Verb_UseAbility;
            Find.Targeter.targetingSource = verb;
            yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, canHitNonTargetPawns: false);
            yield return new Toil
            {
                initAction = verb.Ability.PostAbilityAttempt,
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
            yield return new Toil
            {
                initAction = () =>
                {
                    if (verb.UseAbilityProps.isViolent)
                    {
                        JobDriver_CastAbilityVerb.CheckForAutoAttack(pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
            };
        }
    }
}
