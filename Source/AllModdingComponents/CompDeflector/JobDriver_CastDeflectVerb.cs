using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CompDeflector
{
    public class JobDriver_CastDeflectVerb : JobDriver
    {
        private CompDeflector CompDeflector
        {
            get
            {
                var check = pawn.equipment.AllEquipmentListForReading.FirstOrDefault(x =>
                    x.TryGetComp<CompDeflector>() != null);
                if (check != null)
                    return check.GetComp<CompDeflector>();
                return null;
            }
        }

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
            ////Log.Message("DeflecVErbcalls");
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            //Toil getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
            //yield return getInRangeToil;
            var verb = pawn.CurJob.verbToUse as Verb_Deflected;

            //Find.Targeter.targetingVerb = verb;
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
        }
    }
}