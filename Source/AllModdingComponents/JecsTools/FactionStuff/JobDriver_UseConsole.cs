using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class JobDriver_UseConsole : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell).FailOn(delegate(Toil to)
            {
                var building_CommsConsole = to.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing.TryGetComp<CompConsole>();
                return !building_CommsConsole.CanUseCommsNow;
            });
            Toil openComms = new Toil();
            openComms.initAction = delegate
            {
                Pawn actor = openComms.actor;
                var building_CommsConsole = actor.jobs.curJob.GetTarget(TargetIndex.A).Thing.TryGetComp<CompConsole>();
                if (building_CommsConsole.CanUseCommsNow)
                {
                    TryOpenComms(actor);
                }
            };
            yield return openComms;
            yield break;
        }

        private static void TryOpenComms(Pawn actor)
        {
            var curJobCommTarget = actor.jobs.curJob.commTarget;
            if (curJobCommTarget is Faction f)
            {
                var dialog_Negotiation = new Dialog_Negotiation(actor, f,
                    JecsToolsFactionDialogMaker.FactionDialogFor(actor, f), true);
                dialog_Negotiation.soundAmbient = SoundDefOf.RadioComms_Ambience;
                Find.WindowStack.Add(dialog_Negotiation);
                return;
            }
            if (!(curJobCommTarget is TradeShip ts)) return;
            if (!ts.CanTradeNow)
            {
                return;
            }
            Find.WindowStack.Add(new Dialog_Trade(actor, ts));
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.BuildOrbitalTradeBeacon, OpportunityType.Critical);
            var empty = TaggedString.Empty;
            var empty2 = TaggedString.Empty;
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(
                ts.Goods.OfType<Pawn>(), ref empty, ref empty2, "LetterRelatedPawnsTradeShip".Translate());
            if (!empty2.NullOrEmpty())
                Find.LetterStack.ReceiveLetter(empty, empty2, LetterDefOf.PositiveEvent, null);
            TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.TradeGoodsMustBeNearBeacon);                
        }
    }
}
