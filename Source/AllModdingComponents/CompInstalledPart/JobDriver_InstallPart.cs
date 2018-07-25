using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompInstalledPart
{
    /// <summary>
    ///     Target A = Part to install
    ///     Target B = Thing to install onto
    ///     Target C = spot to drop
    /// </summary>
    public class JobDriver_InstallPart : JobDriver
    {
        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;

        protected float ticksToNextRepair;

        protected float workLeft;

        protected CompInstalledPart InstallComp => PartToInstall.GetComp<CompInstalledPart>();

        protected ThingWithComps PartToInstall => (ThingWithComps) job.targetA.Thing;

        protected Thing InstallTarget => job.targetB.Thing;

        protected int WorkDone => TotalNeededWork - (int) workLeft;

        protected int TotalNeededWork
        {
            get
            {
                var value = InstallComp.Props.workToInstall;
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
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
                .FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, null, false);
            var repair = new Toil
            {
                initAction = delegate
                {
                    ticksToNextRepair = 80f;
                    workLeft = TotalNeededWork;
                },
                tickAction = delegate
                {
                    if (InstallTarget is Pawn pawnTarget) pawnTarget.pather.StopDead();
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
                            actor.records.Increment(RecordDefOf.ThingsInstalled);
                            InstallComp.Notify_Installed(actor, InstallTarget);
                            actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        }
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            repair.WithEffect(InstallComp.Props.workEffect, TargetIndex.B);
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