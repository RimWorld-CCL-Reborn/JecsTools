using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace CompDeflector
{
    public class JobDriver_CastDeflectVerb : JobDriver
    {
        private CompDeflector CompDeflector
        {
            get
            {
                ThingWithComps check = this.pawn.equipment.AllEquipmentListForReading.FirstOrDefault((ThingWithComps x) => x.TryGetComp<CompDeflector>() != null);
                if (check != null)
                {
                    return check.GetComp<CompDeflector>();
                }
                return null;
            }
        }
        public override void ExposeData() => base.ExposeData();

        public override bool TryMakePreToilReservations()
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ////Log.Message("DeflecVErbcalls");
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            //Toil getInRangeToil = Toils_Combat.GotoCastPosition(TargetIndex.A, false);
            //yield return getInRangeToil;
            Verb_Deflected verb = this.pawn.CurJob.verbToUse as Verb_Deflected;

            //Find.Targeter.targetingVerb = verb;
            yield return Toils_Combat.CastVerb(TargetIndex.A, false);
        }
    }
}
