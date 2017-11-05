//CompVehicle_LoadPassenger
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompVehicle
{
    public class JobDriver_AssembleVehicle : JobDriver
    {
        private TargetIndex PartsInd = TargetIndex.A;

        public override bool TryMakePreToilReservations()
        {
            return true;
        }

        private CompVehicleSpawner Spawner
        {
            get
            {
                Thing thing = this.job.GetTarget(this.PartsInd).Thing;
                if (thing == null)
                {
                    return null;
                }
                return thing.TryGetComp<CompVehicleSpawner>();
            }
        }

        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;

        protected float workLeft;

        protected float ticksToNextRepair;


        protected int WorkDone
        {
            get
            {
                return this.TotalNeededWork - (int)this.workLeft;
            }
        }

        protected int TotalNeededWork
        {
            get
            {
                int value = this.Spawner.Props.assemblyTime.SecondsToTicks();
                return Mathf.Clamp(value, 20, 3000);
            }
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(this.PartsInd);
            //this.FailOn(() => !this.<> f__this.Transporter.LoadingInProgressOrReadyToLaunch);
            yield return Toils_Reserve.Reserve(this.PartsInd, 1, -1, null);
            yield return Toils_Goto.GotoThing(this.PartsInd, PathEndMode.Touch);
            Toil repair = new Toil()
            {
                initAction = delegate
                {
                    this.ticksToNextRepair = 80f;
                    this.workLeft = this.Spawner.Props.assemblyTime.SecondsToTicks();
                },
                tickAction = delegate
                {
                    this.pawn.rotationTracker.FaceCell(this.TargetA.Cell);
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
                            actor.records.Increment(RecordDefOf.ThingsConstructed);
                            Spawner.Notify_Assembled(actor);
                            actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        }
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(Spawner.Props.workEffect, TargetIndex.A);
            repair.WithProgressBar(TargetIndex.A, () => this.WorkDone / this.TotalNeededWork, false, -0.5f);
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

