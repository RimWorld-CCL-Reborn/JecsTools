using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace AbilityUser
{
   public class JobDriver_CastAbilityVerb : JobDriver
    {
        private CompAbilityUser CompAbilityUser
        {
            get
            {
                return this.pawn.TryGetComp<CompAbilityUser>();
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {

            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);

            Verb_UseAbility verb = pawn.CurJob.verbToUse as Verb_UseAbility;
            if (TargetA.HasThing)
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
                CompAbilityUser.PostAbilityAttempt(pawn, verb.ability.powerdef);
                CompAbilityUser.ShotFired = true;
            });
        }
    }
}
