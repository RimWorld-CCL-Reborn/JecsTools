using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompInstalledPart
{
    /// <summary>
    ///     Target A = Part to uninstall
    ///     Target B = Thing to uninstall from
    /// </summary>
    public class JobDriver_UninstallPart : JobDriver
    {
        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;

        protected float ticksToNextRepair;

        protected float workLeft;

        protected CompInstalledPart UninstallComp => PartToUninstall.GetComp<CompInstalledPart>();

        protected ThingWithComps PartToUninstall => (ThingWithComps) job.targetA.Thing;

        protected Thing UninstallTarget => job.targetB.Thing;

        protected int WorkDone => TotalNeededWork - (int) workLeft;

        protected int TotalNeededWork
        {
            get
            {
                var value = UninstallComp.Props.workToInstall;
                return Mathf.Clamp(value, 20, 3000);
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            var repair = new Toil
            {
                initAction = delegate
                {
                    ticksToNextRepair = 80f;
                    workLeft = TotalNeededWork;
                },
                tickAction = delegate
                {
                    if (UninstallTarget is Pawn pawnTarget) pawnTarget.pather.StopDead();
                    pawn.rotationTracker.FaceCell(TargetB.Cell);
                    var actor = pawn;
                    actor.skills.Learn(SkillDefOf.Construction, 0.275f, false);
                    var statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                    ticksToNextRepair -= statValue;
                    if (ticksToNextRepair <= 0f)
                    {
                        ticksToNextRepair += 20f;
                        workLeft -= 20 + actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                        if (workLeft <= 0)
                        {
                            actor.records.Increment(RecordDefOf.ThingsUninstalled);
                            UninstallComp.Notify_Uninstalled(actor, UninstallTarget);
                            actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        }
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            repair.WithEffect(UninstallComp.Props.workEffect, TargetIndex.B);
            repair.WithProgressBar(TargetIndex.B, () => WorkDone / TotalNeededWork, false, -0.5f);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            yield return repair;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workLeft, "workLeft", -1);
        }
    }
}