using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class Caravan_JobTracker : IExposable
    {
        private const int ConstantThinkTreeJobCheckIntervalTicks = 30;

        private const int RecentJobQueueMaxLength = 10;

        private const int MaxRecentJobs = 10;

        private const int DamageCheckMinInterval = 180;

        protected Caravan caravan;

        public CaravanJobDriver curDriver;

        public CaravanJob curJob;

        public bool debugLog;

        public CaravanJobQueue jobQueue = new CaravanJobQueue();

        private readonly List<int> jobsGivenRecentTicks = new List<int>(10);

        private readonly List<string> jobsGivenRecentTicksTextual = new List<string>(10);

        private int jobsGivenThisTick;

        private string jobsGivenThisTickTextual = string.Empty;

        private int lastJobGivenAtFrame = -1;

        private bool startingErrorRecoverJob;

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
            Scribe_References.Look(ref caravan, "caravan");
            Scribe_Deep.Look(ref curJob, "curJob");
            Scribe_Deep.Look(ref curDriver, "curDriver");
            Scribe_Deep.Look(ref jobQueue, "jobQueue");
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                if (curDriver != null)
                    curDriver.caravan = caravan;
        }

        public virtual void JobTrackerTick()
        {
            jobsGivenThisTick = 0;
            jobsGivenThisTickTextual = string.Empty;
            if (caravan.IsHashIntervalTick(30))
            {
                //ThinkResult thinkResult = this.DetermineNextConstantThinkTreeJob();
                //if (thinkResult.IsValid && this.ShouldStartJobFromThinkTree(thinkResult))
                //{
                //    this.CheckLeaveJoinableLordBecauseJobIssued(thinkResult);
                //    this.StartJob(thinkResult.Job, JobCondition.InterruptForced, thinkResult.SourceNode, false, false, null, null); //this.caravan.thinker.ConstantThinkTree, thinkResult.Tag);
                //}
            }
            if (curDriver != null)
                curDriver.DriverTick();
            if (curJob == null && //!this.caravan.Dead && this.caravan.mindState.Active && 
                CanDoAnyJob())
            {
                if (debugLog)
                    DebugLogEvent("Starting job from Tick because curJob == null.");
                TryFindAndStartJob();
            }
            FinalizeTick();
        }

        private void FinalizeTick()
        {
            jobsGivenRecentTicks.Add(jobsGivenThisTick);
            jobsGivenRecentTicksTextual.Add(jobsGivenThisTickTextual);
            while (jobsGivenRecentTicks.Count > 10)
            {
                jobsGivenRecentTicks.RemoveAt(0);
                jobsGivenRecentTicksTextual.RemoveAt(0);
            }
            if (jobsGivenThisTick != 0)
            {
                var num = 0;
                for (var i = 0; i < jobsGivenRecentTicks.Count; i++)
                    num += jobsGivenRecentTicks[i];
                if (num >= 10)
                {
                    var text = GenText.ToCommaList(jobsGivenRecentTicksTextual, true);
                    jobsGivenRecentTicks.Clear();
                    jobsGivenRecentTicksTextual.Clear();
                    StartErrorRecoverJob(string.Concat(caravan, " started ", 10, " jobs in ", 10, " ticks. List: ",
                        text));
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
                jobsGivenThisTickTextual = jobsGivenThisTickTextual + "(" + newJob + ") ";
            }
            lastJobGivenAtFrame = RealTime.frameCount;
            if (jobsGivenThisTick > 10)
            {
                var text = jobsGivenThisTickTextual;
                jobsGivenThisTick = 0;
                jobsGivenThisTickTextual = string.Empty;
                StartErrorRecoverJob(string.Concat(caravan, " started 10 jobs in one tick. newJob=", newJob,
                    " jobGiver=", jobGiver, " jobList=", text));
                return;
            }
            if (debugLog)
                DebugLogEvent(string.Concat("StartJob [", newJob, "] lastJobEndCondition=", lastJobEndCondition,
                    ", jobGiver=", jobGiver, ", cancelBusyStances=", cancelBusyStances));
            if (curJob != null)
            {
                if (lastJobEndCondition == JobCondition.None)
                {
                    Log.Warning(string.Concat(caravan, " starting job ", newJob, " while already having job ", curJob,
                        " without a specific job end condition."));
                    lastJobEndCondition = JobCondition.InterruptForced;
                }
                if (resumeCurJobAfterwards && curJob.def.suspendable)
                {
                    jobQueue.EnqueueFirst(curJob, null);
                    if (debugLog)
                        DebugLogEvent("   JobQueue EnqueueFirst curJob: " + curJob);
                }
                CleanupCurrentJob(lastJobEndCondition, !resumeCurJobAfterwards, cancelBusyStances);
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
            if (debugLog)
                DebugLogEvent(string.Concat("EndCurrentJob ", curJob == null ? "null" : curJob.ToString(),
                    " condition=", condition, " curToil=",
                    curDriver == null ? "null_driver" : curDriver.CurToilIndex.ToString()));
            var job = curJob;
            CleanupCurrentJob(condition, true, true);
            if (startNewJob)
            {
                if (condition == JobCondition.ErroredPather || condition == JobCondition.Errored)
                    return;
                if (condition == JobCondition.Succeeded && job != null && !caravan.pather.Moving
                ) //&& job.def != JobDefOf.WaitMaintainPosture )
                {
                    //this.StartJob(new CaravanJob(JobDefOf.WaitMaintainPosture, 1, false), JobCondition.None, null, false, false, null, null);
                }
                else
                {
                    TryFindAndStartJob();
                }
            }
        }

        private void CleanupCurrentJob(JobCondition condition, bool releaseReservations,
            bool cancelBusyStancesSoft = true)
        {
            if (debugLog)
                DebugLogEvent(string.Concat("CleanupCurrentJob ", curJob == null ? "null" : curJob.def.ToString(),
                    " condition ", condition));
            if (curJob == null)
                return;
            curDriver.ended = true;
            curDriver.Cleanup(condition);
            curDriver = null;
            curJob = null;
            //if (releaseReservations)
            //{
            //    this.caravan.ClearReservations(false);
            //}
            //if (cancelBusyStancesSoft)
            //{
            //    this.caravan.stances.CancelBusyStanceSoft();
            //}
            //if (!this.caravan.Destroyed && this.caravan.carryTracker != null && this.caravan.carryTracker.CarriedThing != null)
            //{
            //    Thing thing;
            //    this.caravan.carryTracker.TryDropCarriedThing(this.caravan.Position, ThingPlaceMode.Near, out thing, null);
            //}
        }

        public void CheckForJobOverride()
        {
            if (debugLog)
                DebugLogEvent("CheckForJobOverride");
            //ThinkTreeDef thinkTree;
            //ThinkResult thinkResult = this.DetermineNextJob(out thinkTree);
            //if (this.ShouldStartJobFromThinkTree(thinkResult))
            //{
            //    this.CheckLeaveJoinableLordBecauseJobIssued(thinkResult);
            //    this.StartJob(thinkResult.Job, JobCondition.InterruptOptional, thinkResult.SourceNode, false, false, thinkTree, thinkResult.Tag);
            //}
        }

        public void StopAll(bool ifLayingKeepLaying = false)
        {
            if (ifLayingKeepLaying && curJob != null && curDriver.layingDown == LayingDownState.LayingInBed)
                return;
            CleanupCurrentJob(JobCondition.InterruptForced, true, true);
            jobQueue.Clear();
        }

        private void TryFindAndStartJob()
        {
            //if (this.caravan.thinker == null)
            //{
            //    Log.ErrorOnce(this.caravan + " did TryFindAndStartJob but had no thinker.", 8573261);
            //    return;
            //}
            if (curJob != null)
                Log.Warning(caravan + " doing TryFindAndStartJob while still having job " + curJob);
            if (debugLog)
                DebugLogEvent("TryFindAndStartJob");
            if (!CanDoAnyJob())
            {
                if (debugLog)
                    DebugLogEvent("   CanDoAnyJob is false. Clearing queue and returning");
                if (jobQueue != null)
                    jobQueue.Clear();
                return;
            }
            //ThinkTreeDef thinkTreeDef;
            //ThinkResult result = this.DetermineNextJob(out thinkTreeDef);
            //if (result.IsValid)
            //{
            //    this.CheckLeaveJoinableLordBecauseJobIssued(result);
            //    ThinkNode sourceNode = result.SourceNode;
            //    ThinkTreeDef thinkTree = thinkTreeDef;
            //    this.StartJob(result.Job, JobCondition.None, sourceNode, false, false, thinkTree, result.Tag);
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
                    if (debugLog)
                        DebugLogEvent("   Throwing away queued job that I cannot begin now: " + queuedJob.job);
                }
                if (jobQueue.Count > 0)
                {
                    var queuedJob2 = jobQueue.Dequeue();
                    if (debugLog)
                        DebugLogEvent("   Returning queued job: " + queuedJob2.job);
                    return queuedJob2.job;
                }
            }
            return null;
        }

        public void StartErrorRecoverJob(string message)
        {
            var text = message; //+ " lastJobGiver=" + this.caravan.mindState.lastJobGiver;
            if (curJob != null)
                text = text + ", curJob.def=" + curJob.def.defName;
            if (curDriver != null)
                text = text + ", curDriver=" + curDriver.GetType();
            Log.Error(text);
            if (curJob != null)
                EndCurrentJob(JobCondition.Errored, false);
            if (startingErrorRecoverJob)
            {
                Log.Error(
                    "An error occurred while starting an error recover job. We have to stop now to avoid infinite loops. This means that the Caravan is now jobless which can cause further bugs. Caravan=" +
                    caravan.ToStringSafe());
            }
            else
            {
                startingErrorRecoverJob = true;
                try
                {
                    //this.StartJob(new Job(JobDefOf.Wait, 150, false), JobCondition.None, null, false, true, null, null);
                }
                finally
                {
                    startingErrorRecoverJob = false;
                }
            }
        }

        //private void CheckLeaveJoinableLordBecauseJobIssued(ThinkResult result)
        //{
        //    if (!result.IsValid || result.SourceNode == null)
        //    {
        //        return;
        //    }
        //    Lord lord = this.caravan.GetLord();
        //    if (lord == null || !(lord.LordJob is LordJob_VoluntarilyJoinable))
        //    {
        //        return;
        //    }
        //    bool flag = false;
        //    ThinkNode thinkNode = result.SourceNode;
        //    while (!thinkNode.leaveJoinableLordIfIssuesJob)
        //    {
        //        thinkNode = thinkNode.parent;
        //        if (thinkNode == null)
        //        {
        //            IL_6F:
        //            if (flag)
        //            {
        //                lord.Notify_CaravanLost(this.caravan, CaravanLostCondition.LeftVoluntarily);
        //            }
        //            return;
        //        }
        //    }
        //    flag = true;
        //    goto IL_6F;
        //}

        private bool CanDoAnyJob()
        {
            return caravan.Spawned;
        }

        //private bool ShouldStartJobFromThinkTree(ThinkResult thinkResult)
        //{
        //    return this.curJob == null || (thinkResult.Job.def != this.curJob.def || thinkResult.SourceNode != this.caravan.mindState.lastJobGiver || !this.curDriver.IsContinuation(thinkResult.Job));
        //}

        public bool IsCurrentJobPlayerInterruptible()
        {
            return curJob == null || curJob.def.playerInterruptible; //&& !this.caravan.HasAttachment(ThingDefOf.Fire);
        }

        public bool TryTakeOrderedJobPrioritizedWork(CaravanJob job, WorkGiver giver, IntVec3 cell)
        {
            if (TryTakeOrderedJob(job, giver.def.tagToGive))
                return true;
            return false;
        }

        public bool TryTakeOrderedJob(CaravanJob job, JobTag tag = JobTag.Misc)
        {
            if (debugLog)
                DebugLogEvent("TakeOrderedJob " + job);
            job.playerForced = true;
            if (curJob != null && curJob.JobIsSameAs(job))
                return true;
            caravan.pather.StopDead();
            //this.caravan.Map.CaravanDestinationManager.UnreserveAllFor(this.caravan);
            //if (job.def == CaravanJobDefOf.Goto)
            //{
            //    //this.caravan.Map.CaravanDestinationManager.ReserveDestinationFor(this.caravan, job.targetA.Cell);
            //}
            if (debugLog)
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

        public void DebugLogEvent(string s)
        {
            if (debugLog)
                Log.Message(string.Concat(Find.TickManager.TicksGame, " ", caravan, ": ", s));
        }
    }
}