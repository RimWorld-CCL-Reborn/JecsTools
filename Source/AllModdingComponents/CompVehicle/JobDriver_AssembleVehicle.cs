//CompVehicle_LoadPassenger

using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompVehicle
{
    public class JobDriver_AssembleVehicle : JobDriver
    {
        private const float WarmupTicks = 80f;

        private const float TicksBetweenRepairs = 20f;
        private readonly TargetIndex PartsInd = TargetIndex.A;

        protected float ticksToNextRepair;

        protected float workLeft;

        private CompVehicleSpawner Spawner
        {
            get
            {
                var thing = job.GetTarget(PartsInd).Thing;
                if (thing == null)
                    return null;
                return thing.TryGetComp<CompVehicleSpawner>();
            }
        }


        protected int WorkDone => TotalNeededWork - (int) workLeft;

        protected int TotalNeededWork
        {
            get
            {
                var value = Spawner.Props.assemblyTime.SecondsToTicks();
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
            this.FailOnDespawnedOrNull(PartsInd);
            //this.FailOn(() => !this.<> f__this.Transporter.LoadingInProgressOrReadyToLaunch);
            yield return Toils_Reserve.Reserve(PartsInd, 1, -1, null);
            yield return Toils_Goto.GotoThing(PartsInd, PathEndMode.Touch);
            var repair = new Toil
            {
                initAction = delegate
                {
                    ticksToNextRepair = 80f;
                    workLeft = Spawner.Props.assemblyTime.SecondsToTicks();
                },
                tickAction = delegate
                {
                    pawn.rotationTracker.FaceCell(TargetA.Cell);
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
                            actor.records.Increment(RecordDefOf.ThingsConstructed);
                            Spawner.Notify_Assembled(actor);
                            actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        }
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(Spawner.Props.workEffect, TargetIndex.A);
            repair.WithProgressBar(TargetIndex.A, () => WorkDone / TotalNeededWork, false, -0.5f);
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