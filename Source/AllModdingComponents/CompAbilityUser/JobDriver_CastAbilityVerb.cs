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
                if (this.CompAbilityUsers is List<CompAbilityUser> users && !users.NullOrEmpty())
                {
                    foreach (CompAbilityUser u in users)
                    {
                        u.PostAbilityAttempt(this.pawn, verb.ability.powerdef);
                    }
                }
                //this.CompAbilityUser.ShotFired = true;
            });
        }
    }
}
