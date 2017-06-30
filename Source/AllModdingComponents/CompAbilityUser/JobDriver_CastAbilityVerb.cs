using System.Collections.Generic;

namespace AbilityUser
{
    public class JobDriver_CastAbilityVerb : JobDriver
    {
        private CompAbilityUser CompAbilityUser => this.pawn.TryGetComp<CompAbilityUser>();

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
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
            //CompAbilityUser.IsActive = true;
            this.AddFinishAction(() =>
            {
                //   //Log.Message("FinishACtion");
                //if (CompAbilityUser.IsActive)
                //{
                //PsykerUtility.PsykerShockEvents(CompAbilityUser, CompAbilityUser.curPower.PowerLevel);
                //}
                this.CompAbilityUser.PostAbilityAttempt(this.pawn, verb.ability.powerdef);
                //this.CompAbilityUser.ShotFired = true;
            });
        }
    }
}
