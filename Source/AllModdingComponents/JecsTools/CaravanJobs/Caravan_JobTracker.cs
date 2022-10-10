//#define DEBUGLOG

using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace JecsTools
{
    // Based off Pawn_JobTracker.
    public class Caravan_JobTracker : IExposable
    {
        //private const int ConstantThinkTreeJobCheckIntervalTicks = 30;

        private const int RecentJobQueueMaxLength = 10;

        private const int MaxRecentJobs = 10;

        protected Caravan caravan;

        public CaravanJobDriver curDriver;

        public CaravanJob curJob;

        public CaravanJobQueue jobQueue = new CaravanJobQueue();

        private readonly List<int> jobsGivenRecentTicks = new List<int>(RecentJobQueueMaxLength);

        private readonly List<string> jobsGivenRecentTicksTextual = new List<string>(RecentJobQueueMaxLength);

        private int jobsGivenThisTick;

        private string jobsGivenThisTickTextual = "";

        private int lastJobGivenAtFrame = -1;

        //private bool startingErrorRecoverJob;

        public Caravan_JobTracker()
        {
        }

        public Caravan_JobTracker(Caravan newCaravan)
        {
            //Log.Message("JecsTools :: CaravanJobTracker :: JobTracker Created");
            caravan = newCaravan;
        }

        public Caravan Caravan => caravan;

        public virtual void ExposeData()
        {
            Scribe_References.Look(ref caravan, nameof(caravan));
            Scribe_Deep.Look(ref curJob, nameof(curJob));
            Scribe_Deep.Look(ref curDriver, nameof(curDriver));
            Scribe_Deep.Look(ref jobQueue, nameof(jobQueue));
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                if (curDriver != null)
                    curDriver.caravan = caravan;
        }

        public virtual void JobTrackerTick()
        {
            jobsGivenThisTick = 0;
            jobsGivenThisTickTextual = "";
            //if (caravan.IsHashIntervalTick(ConstantThinkTreeJobCheckIntervalTicks))
            //{
            //    var thinkResult = DetermineNextConstantThinkTreeJob();
            //    if (thinkResult.IsValid && ShouldStartJobFromThinkTree(thinkResult))
            //    {
            //        CheckLeaveJoinableLordBecauseJobIssued(thinkResult);
            //        StartJob(thinkResult.Job, JobCondition.InterruptForced, thinkResult.SourceNode, false, false); //caravan.thinker.ConstantThinkTree, thinkResult.Tag);
            //    }
            //}
            curDriver?.DriverTick();
            if (curJob == null && //!caravan.Dead && caravan.mindState.Active &&
                CanDoAnyJob())
            {
                DebugLogEvent("Starting job from Tick because curJob == null.");
                TryFindAndStartJob();
            }
            FinalizeTick();
        }

        private void FinalizeTick()
        {
            jobsGivenRecentTicks.Add(jobsGivenThisTick);
            jobsGivenRecentTicksTextual.Add(jobsGivenThisTickTextual);
            while (jobsGivenRecentTicks.Count > RecentJobQueueMaxLength)
            {
                jobsGivenRecentTicks.RemoveAt(0);
                jobsGivenRecentTicksTextual.RemoveAt(0);
            }
            if (jobsGivenThisTick != 0)
            {
                var num = 0;
                for (var i = 0; i < jobsGivenRecentTicks.Count; i++)
                    num += jobsGivenRecentTicks[i];
                if (num >= MaxRecentJobs)
                {
                    var text = GenText.ToCommaList(jobsGivenRecentTicksTextual, true);
                    jobsGivenRecentTicks.Clear();
                    jobsGivenRecentTicksTextual.Clear();
                    StartErrorRecoverJob($"{caravan} started {MaxRecentJobs} jobs in {RecentJobQueueMaxLength} ticks. List: {text}");
                }
            }
        }

        public void StartJob(CaravanJob newJob, JobCondition lastJobEndCondition = JobCondition.None,
            ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true,
            ThinkTreeDef thinkTree = null, JobTag? tag = null)
        {
            //Log.Message("JecsTools :: CaravanJobTracker :: JobTracker StartJob :: " + newJob.def.label);
            if (!Find.TickManager.Paused || lastJobGivenAtFrame == RealTime.frameCount)
            {
                jobsGivenThisTick++;
                jobsGivenThisTickTextual += "(" + newJob + ") ";
            }
            lastJobGivenAtFrame = RealTime.frameCount;
            if (jobsGivenThisTick > MaxRecentJobs)
            {
                var text = jobsGivenThisTickTextual;
                jobsGivenThisTick = 0;
                jobsGivenThisTickTextual = "";
                StartErrorRecoverJob($"{caravan} started {MaxRecentJobs} jobs in one tick. newJob={newJob} jobGiver={jobGiver} jobList={text}");
                return;
            }
            DebugLogEvent($"StartJob [{newJob}] lastJobEndCondition={lastJobEndCondition}, jobGiver={jobGiver}, cancelBusyStances={cancelBusyStances}");
            if (curJob != null)
            {
                if (lastJobEndCondition == JobCondition.None)
                {
                    Log.Warning($"{caravan} starting job {newJob} while already having job {curJob} without a specific job end condition.");
                    lastJobEndCondition = JobCondition.InterruptForced;
                }
                if (resumeCurJobAfterwards && curJob.def.suspendable)
                {
                    jobQueue.EnqueueFirst(curJob, null);
                    DebugLogEvent("   JobQueue EnqueueFirst curJob: " + curJob);
                }
                //CleanupCurrentJob(lastJobEndCondition, !resumeCurJobAfterwards, cancelBusyStances);
                CleanupCurrentJob(lastJobEndCondition);
            }
            if (newJob == null)
            {
                Log.Warning(caravan + " tried to start doing a null job.");
                return;
            }
            newJob.startTick = Find.TickManager.TicksGame;
            if (newJob.playerForced)
            {
                newJob.ignoreForbidden = true;
                newJob.ignoreDesignations = true;
            }
            curJob = newJob;
            curDriver = curJob.MakeDriver(caravan);
            curDriver.Notify_Starting();
            curDriver.SetupToils();
            curDriver.ReadyForNextToil();
        }

        public void EndCurrentJob(JobCondition condition, bool startNewJob = true)
        {
            DebugLogEvent($"EndCurrentJob {curJob?.ToString() ?? "null"} condition={condition} " +
                $"curToil={curDriver?.CurToilIndex.ToString() ?? "null_driver"}");
            var job = curJob;
            //CleanupCurrentJob(condition, true, true);
            CleanupCurrentJob(condition);
            if (startNewJob)
            {
                switch (condition)
                {
                    case JobCondition.Errored:
                    case JobCondition.ErroredPather:
                        //StartJob(JobMaker.MakeJob(JobDefOf.Wait, 250));
                        return;
                    case JobCondition.Succeeded:
                        if (job != null && !caravan.pather.Moving) //&& job.def != JobDefOf.WaitMaintainPosture
                        {
                            //StartJob(JobMaker.MakeJob(JobDefOf.WaitMaintainPosture, 1), JobCondition.None, null, false, false);
                            return;
                        }
                        break;
                }
                TryFindAndStartJob();
            }
        }

        //private void CleanupCurrentJob(JobCondition condition, bool releaseReservations, bool cancelBusyStancesSoft = true)
        private void CleanupCurrentJob(JobCondition condition)
        {
            DebugLogEvent($"CleanupCurrentJob {curJob?.def.ToString() ?? "null"} condition {condition}");
            if (curJob == null)
                return;
            curDriver.ended = true;
            curDriver.Cleanup(condition);
            curDriver = null;
            curJob = null;
            //if (releaseReservations)
            //{
            //    caravan.ClearReservations(false);
            //}
            //if (cancelBusyStancesSoft)
            //{
            //    caravan.stances.CancelBusyStanceSoft();
            //}
            //if (!caravan.Destroyed && caravan.carryTracker != null && caravan.carryTracker.CarriedThing != null)
            //{
            //    Thing thing;
            //    caravan.carryTracker.TryDropCarriedThing(caravan.Position, ThingPlaceMode.Near, out thing, null);
            //}
        }

        public void CheckForJobOverride()
        {
            DebugLogEvent("CheckForJobOverride");
            //ThinkResult thinkResult = DetermineNextJob(out var thinkTree);
            //if (ShouldStartJobFromThinkTree(thinkResult))
            //{
            //    CheckLeaveJoinableLordBecauseJobIssued(thinkResult);
            //    StartJob(thinkResult.Job, JobCondition.InterruptOptional, thinkResult.SourceNode, false, false, thinkTree, thinkResult.Tag);
            //}
        }

        public void StopAll(bool ifLayingKeepLaying = false)
        {
            if (ifLayingKeepLaying && curJob != null && curDriver.layingDown == LayingDownState.LayingInBed)
                return;
            //CleanupCurrentJob(JobCondition.InterruptForced, true, true);
            CleanupCurrentJob(JobCondition.InterruptForced);
            jobQueue.Clear();
        }

        private void TryFindAndStartJob()
        {
            //if (caravan.thinker == null)
            //{
            //    Log.ErrorOnce(caravan + " did TryFindAndStartJob but had no thinker.", 8573261);
            //    return;
            //}
            if (curJob != null)
                Log.Warning(caravan + " doing TryFindAndStartJob while still having job " + curJob);
            DebugLogEvent("TryFindAndStartJob");
            if (!CanDoAnyJob())
            {
                DebugLogEvent("   CanDoAnyJob is false. Clearing queue and returning");
                jobQueue?.Clear();
                return;
            }
            //ThinkResult result = DetermineNextJob(out var thinkTreeDef);
            //if (result.IsValid)
            //{
            //    CheckLeaveJoinableLordBecauseJobIssued(result);
            //    ThinkNode sourceNode = result.SourceNode;
            //    ThinkTreeDef thinkTree = thinkTreeDef;
            //    StartJob(result.Job, JobCondition.None, sourceNode, false, false, thinkTree, result.Tag);
            //}

            //ThinkTreeDef thinkTreeDef;
            //Log.Message("JecsTools :: CaravanJobTracker :: JobTracker TryFindStartNextJob");
            var result = DetermineNextJob();
            if (result != null && result.CanBeginNow(caravan))
            {
                //Log.Message("JecsTools :: CaravanJobTracker :: JobTracker StartJob :: " + result.def.label);
                StartJob(result, JobCondition.None, null, false, false, null, null);
            }
        }

        private CaravanJob DetermineNextJob()
        {
            if (jobQueue != null)
            {
                while (jobQueue.Count > 0 && !jobQueue.Peek().job.CanBeginNow(caravan))
                {
                    var queuedJob = jobQueue.Dequeue();
                    DebugLogEvent("   Throwing away queued job that I cannot begin now: " + queuedJob.job);
                }
                if (jobQueue.Count > 0)
                {
                    var queuedJob2 = jobQueue.Dequeue();
                    DebugLogEvent("   Returning queued job: " + queuedJob2.job);
                    return queuedJob2.job;
                }
            }
            return null;
        }

        public void StartErrorRecoverJob(string message)
        {
            var text = message; //+ " lastJobGiver=" + caravan.mindState.lastJobGiver;
            if (curJob != null)
                text += ", curJob.def=" + curJob.def.defName;
            if (curDriver != null)
                text += ", curDriver=" + curDriver.GetType();
            Log.Error(text);
            if (curJob != null)
                EndCurrentJob(JobCondition.Errored, false);
            //if (startingErrorRecoverJob)
            //{
            //    Log.Error(
            //        "An error occurred while starting an error recover job. We have to stop now to avoid infinite loops. " +
            //        "This means that the Caravan is now jobless which can cause further bugs. Caravan=" +
            //        caravan.ToStringSafe());
            //}
            //else
            //{
            //    startingErrorRecoverJob = true;
            //    try
            //    {
            //        StartJob(JobMaker.MakeJob(JobDefOf.Wait, 150));
            //    }
            //    finally
            //    {
            //        startingErrorRecoverJob = false;
            //    }
            //}
        }

        //private void CheckLeaveJoinableLordBecauseJobIssued(ThinkResult result)
        //{
        //    if (!result.IsValid || result.SourceNode == null)
        //    {
        //        return;
        //    }
        //    var lord = caravan.GetLord();
        //    if (lord == null || !(lord.LordJob is LordJob_VoluntarilyJoinable))
        //    {
        //        return;
        //    }
        //    var flag = false;
        //    var thinkNode = result.SourceNode;
        //    do
        //    {
        //        if (thinkNode.leaveJoinableLordIfIssuesJob)
        //        {
        //            flag = true;
        //            break;
        //        }
        //        thinkNode = thinkNode.parent;
        //    }
        //    while (thinkNode == null);
        //    if (flag)
        //    {
        //        lord.Notify_CaravanLost(caravan, CaravanLostCondition.LeftVoluntarily);
        //    }
        //}

        private bool CanDoAnyJob()
        {
            return caravan.Spawned;
        }

        //private bool ShouldStartJobFromThinkTree(ThinkResult thinkResult)
        //{
        //    return curJob == null ||
        //        thinkResult.Job.def != curJob.def ||
        //        thinkResult.SourceNode != caravan.mindState.lastJobGiver ||
        //        !curDriver.IsContinuation(thinkResult.Job);
        //}

        public bool IsCurrentJobPlayerInterruptible()
        {
            return curJob == null || curJob.def.playerInterruptible; //&& !caravan.HasAttachment(ThingDefOf.Fire);
        }

        public bool TryTakeOrderedJobPrioritizedWork(CaravanJob job, WorkGiver giver, IntVec3 cell)
        {
            return TryTakeOrderedJob(job, giver.def.tagToGive);
        }

        public bool TryTakeOrderedJob(CaravanJob job, JobTag tag = JobTag.Misc)
        {
            DebugLogEvent("TakeOrderedJob " + job);
            job.playerForced = true;
            if (curJob != null && curJob.JobIsSameAs(job))
                return true;
            caravan.pather.StopDead();
            //caravan.Map.CaravanDestinationManager.UnreserveAllFor(caravan);
            //if (job.def == CaravanJobDefOf.Goto)
            //{
            //    //caravan.Map.CaravanDestinationManager.ReserveDestinationFor(caravan, job.targetA.Cell);
            //}
            DebugLogEvent("    Queueing job");
            jobQueue.Clear();
            jobQueue.EnqueueFirst(job, tag);
            if (IsCurrentJobPlayerInterruptible())
                if (curJob != null)
                    curDriver.EndJobWith(JobCondition.InterruptForced);
                else
                    CheckForJobOverride();
            return true;
        }

        public void Notify_PathInterrupted()
        {
            EndCurrentJob(JobCondition.InterruptForced, false);
        }

        [Conditional("DEBUGLOG")]
        private void DebugLogEvent(string s)
        {
            Log.Message($"{Find.TickManager.TicksGame} {caravan}: {s}");
        }
    }
}
