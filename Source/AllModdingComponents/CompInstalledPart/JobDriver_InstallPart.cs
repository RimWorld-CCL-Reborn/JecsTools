using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace CompInstalledPart
{
    /// <summary>
    /// Target A = Part to install
    /// Target B = Thing to install onto
    /// Target C = spot to drop 
    /// </summary>
    public class JobDriver_InstallPart : JobDriver
    {
        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;

        protected float workLeft;

        protected float ticksToNextRepair;

        protected CompInstalledPart InstallComp => this.PartToInstall.GetComp<CompInstalledPart>();

        protected ThingWithComps PartToInstall => (ThingWithComps)this.CurJob.targetA.Thing;

        protected Thing InstallTarget => this.CurJob.targetB.Thing;

        protected int WorkDone
        {
            get
            {
                return TotalNeededWork - (int)workLeft;
            }
        }

        protected int TotalNeededWork
        {
            get
            {
                int value = this.InstallComp.Props.workToInstall;
                return Mathf.Clamp(value, 20, 3000);
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, null, false);
            Toil repair = new Toil()
            {
                initAction = delegate
                {
                    this.ticksToNextRepair = 80f;
                    this.workLeft = this.TotalNeededWork;
                },
                tickAction = delegate
                {
                    if (InstallTarget is Pawn pawnTarget) pawnTarget.pather.StopDead();
                    this.pawn.Drawer.rotator.FaceCell(this.TargetB.Cell);
                    Pawn actor = this.pawn;
                    actor.skills.Learn(SkillDefOf.Construction, 0.275f, false);
                    float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                    this.ticksToNextRepair -= statValue;
                    if (this.ticksToNextRepair <= 0f)
                    {
                        this.ticksToNextRepair += 20f;
                        this.workLeft -= 20 + actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                        if (this.workLeft <= 0)
                        {
                            actor.records.Increment(RecordDefOf.ThingsInstalled);
                            this.InstallComp.Notify_Installed(actor, this.InstallTarget);
                            actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        }
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            repair.WithEffect(this.InstallComp.Props.workEffect, TargetIndex.B);
            repair.WithProgressBar(TargetIndex.B, () => this.WorkDone / this.TotalNeededWork, false, -0.5f);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            yield return repair;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.workLeft, "workLeft", -1);
        }
    }
}
