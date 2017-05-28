using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace CompInstalledPart
{
    /// <summary>
    /// Target A = Part to uninstall
    /// Target B = Thing to uninstall from 
    /// </summary>
    public class JobDriver_UninstallPart : JobDriver
    {
        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;

        protected float workLeft;

        protected float ticksToNextRepair;

        protected CompInstalledPart UninstallComp
        {
            get
            {
                return PartToUninstall.GetComp<CompInstalledPart>();
            }
        }

        protected ThingWithComps PartToUninstall
        {
            get
            {
                return (ThingWithComps)base.CurJob.targetA.Thing;
            }
        }

        protected Thing UninstallTarget
        {
            get
            {
                return base.CurJob.targetB.Thing;
            }
        }

        protected int TotalNeededWork
        {
            get
            {
                int value = UninstallComp.Props.workToInstall;
                return Mathf.Clamp(value, 20, 3000);
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            Toil repair = new Toil();
            repair.initAction = delegate
            {
                this.ticksToNextRepair = 80f;
                this.workLeft = TotalNeededWork;
            };
            repair.tickAction = delegate
            {
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
                        actor.records.Increment(RecordDefOf.ThingsUninstalled);
                        UninstallComp.Notify_Uninstalled(actor, UninstallTarget);
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch);
            repair.WithEffect(UninstallComp.Props.workEffect, TargetIndex.B);
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
