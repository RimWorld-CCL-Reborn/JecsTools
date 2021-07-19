using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace CompDeflector
{
    public class JobDriver_CastDeflectVerb : JobDriver
    {
        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            //Toil getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
            //yield return getInRangeToil;
            //var verb = pawn.CurJob.verbToUse as Verb_Deflected;
            //Find.Targeter.targetingVerb = verb;
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
        }
    }
}
