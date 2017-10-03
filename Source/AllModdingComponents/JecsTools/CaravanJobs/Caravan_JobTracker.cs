using RimWorld;
using System;
using System.Collections.Generic;
using Verse.AI.Group;
using Verse.AI;
using Verse;
using RimWorld.Planet;

namespace JecsTools
{
    public class Caravan_JobTracker : IExposable
    {
        private const int ConstantThinkTreeJobCheckIntervalTicks = 30;

        private const int RecentJobQueueMaxLength = 10;

        private const int MaxRecentJobs = 10;

        private const int DamageCheckMinInterval = 180;

        protected Caravan caravan;

        public Caravan Caravan => caravan;

        public CaravanJob curJob;

        public CaravanJobDriver curDriver;

        public CaravanJobQueue jobQueue = new CaravanJobQueue();

        private int jobsGivenThisTick;

        private string jobsGivenThisTickTextual = string.Empty;

        private int lastJobGivenAtFrame = -1;

        private List<int> jobsGivenRecentTicks = new List<int>(10);

        private List<string> jobsGivenRecentTicksTextual = new List<string>(10);

        public bool debugLog;

        private bool startingErrorRecoverJob;
        
        public Caravan_JobTracker()
        {

        }

        public Caravan_JobTracker(Caravan newCaravan)
        {
            Log.Message("JecsTools :: CaravanJobTracker :: JobTracker Created");
            this.caravan = newCaravan;
        }

        public virtual void ExposeData()
        {
            Scribe_References.Look<Caravan>(ref this.caravan, "caravan");
            Scribe_Deep.Look<CaravanJob>(ref this.curJob, "curJob", new object[0]);
            Scribe_Deep.Look<CaravanJobDriver>(ref this.curDriver, "curDriver", new object[0]);
            Scribe_Deep.Look<CaravanJobQueue>(ref this.jobQueue, "jobQueue", new object[0]);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (this.curDriver != null)
                {
                    this.curDriver.caravan = this.caravan;
                }
                //BackCompatibility.JobTrackerPostLoadInit(this);
            }
        }

        public virtual void JobTrackerTick()
        {
            this.jobsGivenThisTick = 0;
            this.jobsGivenThisTickTextual = string.Empty;
            if (this.caravan.IsHashIntervalTick(30))
            {
                //ThinkResult thinkResult = this.DetermineNextConstantThinkTreeJob();
                //if (thinkResult.IsValid && this.ShouldStartJobFromThinkTree(thinkResult))
                //{
                //    this.CheckLeaveJoinableLordBecauseJobIssued(thinkResult);
                //    this.StartJob(thinkResult.Job, JobCondition.InterruptForced, thinkResult.SourceNode, false, false, null, null); //this.caravan.thinker.ConstantThinkTree, thinkResult.Tag);
                //}
            }
            if (this.curDriver != null)
            {
                //if (this.curJob.expiryInterval > 0 && (Find.TickManager.TicksGame - this.curJob.startTick) % this.curJob.expiryInterval == 0 && Find.TickManager.TicksGame != this.curJob.startTick)
                //{
                //    if (!this.curJob.expireRequiresEnemiesNearby || CaravanUtility.EnemiesAreNearby(this.caravan, 25, false))
                //    {
                //        if (this.debugLog)
                //        {
                //            this.DebugLogEvent("Job expire");
                //        }
                //        if (!this.curJob.checkOverrideOnExpire)
                //        {
                //            this.EndCurrentJob(JobCondition.Succeeded, true);
                //        }
                //        else
                //        {
                //            this.CheckForJobOverride();
                //        }
                //        this.FinalizeTick();
                //        return;
                //    }
                //    if (this.debugLog)
                //    {
                //        this.DebugLogEvent("Job expire skipped because there are no enemies nearby");
                //    }
                //}
                this.curDriver.DriverTick();
            }
            if (this.curJob == null && //!this.caravan.Dead && this.caravan.mindState.Active && 
                this.CanDoAnyJob())
            {
                if (this.debugLog)
                {
                    this.DebugLogEvent("Starting job from Tick because curJob == null.");
                }
                this.TryFindAndStartJob();
            }
            this.FinalizeTick();
        }

        private void FinalizeTick()
        {
            this.jobsGivenRecentTicks.Add(this.jobsGivenThisTick);
            this.jobsGivenRecentTicksTextual.Add(this.jobsGivenThisTickTextual);
            while (this.jobsGivenRecentTicks.Count > 10)
            {
                this.jobsGivenRecentTicks.RemoveAt(0);
                this.jobsGivenRecentTicksTextual.RemoveAt(0);
            }
            if (this.jobsGivenThisTick != 0)
            {
                int num = 0;
                for (int i = 0; i < this.jobsGivenRecentTicks.Count; i++)
                {
                    num += this.jobsGivenRecentTicks[i];
                }
                if (num >= 10)
                {
                    string text = GenText.ToCommaList(this.jobsGivenRecentTicksTextual, true);
                    this.jobsGivenRecentTicks.Clear();
                    this.jobsGivenRecentTicksTextual.Clear();
                    this.StartErrorRecoverJob(string.Concat(new object[]
                    {
                        this.caravan,
                        " started ",
                        10,
                        " jobs in ",
                        10,
                        " ticks. List: ",
                        text
                    }));
                }
            }
        }

        public void StartJob(CaravanJob newJob, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true, ThinkTreeDef thinkTree = null, JobTag? tag = null)
        {
            Log.Message("JecsTools :: CaravanJobTracker :: JobTracker StartJob :: " + newJob.def.label);
            if (!Find.TickManager.Paused || this.lastJobGivenAtFrame == RealTime.frameCount)
            {
                this.jobsGivenThisTick++;
                this.jobsGivenThisTickTextual = this.jobsGivenThisTickTextual + "(" + newJob.ToString() + ") ";
            }
            this.lastJobGivenAtFrame = RealTime.frameCount;
            if (this.jobsGivenThisTick > 10)
            {
                string text = this.jobsGivenThisTickTextual;
                this.jobsGivenThisTick = 0;
                this.jobsGivenThisTickTextual = string.Empty;
                this.StartErrorRecoverJob(string.Concat(new object[]
                {
                    this.caravan,
                    " started 10 jobs in one tick. newJob=",
                    newJob,
                    " jobGiver=",
                    jobGiver,
                    " jobList=",
                    text
                }));
                return;
            }
            if (this.debugLog)
            {
                this.DebugLogEvent(string.Concat(new object[]
                {
                    "StartJob [",
                    newJob,
                    "] lastJobEndCondition=",
                    lastJobEndCondition,
                    ", jobGiver=",
                    jobGiver,
                    ", cancelBusyStances=",
                    cancelBusyStances
                }));
            }
            if (this.curJob != null)
            {
                if (lastJobEndCondition == JobCondition.None)
                {
                    Log.Warning(string.Concat(new object[]
                    {
                        this.caravan,
                        " starting job ",
                        newJob,
                        " while already having job ",
                        this.curJob,
                        " without a specific job end condition."
                    }));
                    lastJobEndCondition = JobCondition.InterruptForced;
                }
                if (resumeCurJobAfterwards && this.curJob.def.suspendable)
                {
                    this.jobQueue.EnqueueFirst(this.curJob, null);
                    if (this.debugLog)
                    {
                        this.DebugLogEvent("   JobQueue EnqueueFirst curJob: " + this.curJob);
                    }
                }
                this.CleanupCurrentJob(lastJobEndCondition, !resumeCurJobAfterwards, cancelBusyStances);
            }
            if (newJob == null)
            {
                Log.Warning(this.caravan + " tried to start doing a null job.");
                return;
            }
            newJob.startTick = Find.TickManager.TicksGame;
            if (newJob.playerForced)
            {
                newJob.ignoreForbidden = true;
                newJob.ignoreDesignations = true;
            }
            this.curJob = newJob;
            this.curDriver = this.curJob.MakeDriver(this.caravan);
            this.curDriver.Notify_Starting();
            this.curDriver.SetupToils();
            this.curDriver.ReadyForNextToil();
        }

        public void EndCurrentJob(JobCondition condition, bool startNewJob = true)
        {
            if (this.debugLog)
            {
                this.DebugLogEvent(string.Concat(new object[]
                {
                    "EndCurrentJob ",
                    (this.curJob == null) ? "null" : this.curJob.ToString(),
                    " condition=",
                    condition,
                    " curToil=",
                    (this.curDriver == null) ? "null_driver" : this.curDriver.CurToilIndex.ToString()
                }));
            }
            CaravanJob job = this.curJob;
            this.CleanupCurrentJob(condition, true, true);
            if (startNewJob)
            {
                if (condition == JobCondition.ErroredPather || condition == JobCondition.Errored)
                {
                    //this.StartJob(new CaravanJob(JobDefOf.Wait, 250, false), JobCondition.None, null, false, true, null, null);
                    return;
                }
                if (condition == JobCondition.Succeeded && job != null && !this.caravan.pather.Moving) //&& job.def != JobDefOf.WaitMaintainPosture )
                {
                    //this.StartJob(new CaravanJob(JobDefOf.WaitMaintainPosture, 1, false), JobCondition.None, null, false, false, null, null);
                }
                else
                {
                    this.TryFindAndStartJob();
                }
            }
        }

        private void CleanupCurrentJob(JobCondition condition, bool releaseReservations, bool cancelBusyStancesSoft = true)
        {
            if (this.debugLog)
            {
                this.DebugLogEvent(string.Concat(new object[]
                {
                    "CleanupCurrentJob ",
                    (this.curJob == null) ? "null" : this.curJob.def.ToString(),
                    " condition ",
                    condition
                }));
            }
            if (this.curJob == null)
            {
                return;
            }
            this.curDriver.ended = true;
            this.curDriver.Cleanup(condition);
            this.curDriver = null;
            this.curJob = null;
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
            if (this.debugLog)
            {
                this.DebugLogEvent("CheckForJobOverride");
            }
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
            if (ifLayingKeepLaying && this.curJob != null && this.curDriver.layingDown != LayingDownState.NotLaying)
            {
                return;
            }
            this.CleanupCurrentJob(JobCondition.InterruptForced, true, true);
            this.jobQueue.Clear();
        }

        private void TryFindAndStartJob()
        {
            //if (this.caravan.thinker == null)
            //{
            //    Log.ErrorOnce(this.caravan + " did TryFindAndStartJob but had no thinker.", 8573261);
            //    return;
            //}
            if (this.curJob != null)
            {
                Log.Warning(this.caravan + " doing TryFindAndStartJob while still having job " + this.curJob);
            }
            if (this.debugLog)
            {
                this.DebugLogEvent("TryFindAndStartJob");
            }
            if (!this.CanDoAnyJob())
            {
                if (this.debugLog)
                {
                    this.DebugLogEvent("   CanDoAnyJob is false. Clearing queue and returning");
                }
                if (this.jobQueue != null)
                {
                    this.jobQueue.Clear();
                }
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
            CaravanJob result = this.DetermineNextJob();
            if (result != null && result.CanBeginNow(caravan))
            {
                Log.Message("JecsTools :: CaravanJobTracker :: JobTracker StartJob :: " + result.def.label);
                this.StartJob(result, JobCondition.None, null, false, false, null, null);
            }
        }

        private CaravanJob DetermineNextJob()
        {
            if (this.jobQueue != null)
            {
                while (this.jobQueue.Count > 0 && !this.jobQueue.Peek().job.CanBeginNow(this.caravan))
                {
                    QueuedCaravanJob queuedJob = this.jobQueue.Dequeue();
                    if (this.debugLog)
                    {
                        this.DebugLogEvent("   Throwing away queued job that I cannot begin now: " + queuedJob.job);
                    }
                }
                if (this.jobQueue.Count > 0)
                {
                    QueuedCaravanJob queuedJob2 = this.jobQueue.Dequeue();
                    if (this.debugLog)
                    {
                        this.DebugLogEvent("   Returning queued job: " + queuedJob2.job);
                    }
                    return queuedJob2.job;
                }
            }
            return null;
        }

        public void StartErrorRecoverJob(string message)
        {
            string text = message; //+ " lastJobGiver=" + this.caravan.mindState.lastJobGiver;
            if (this.curJob != null)
            {
                text = text + ", curJob.def=" + this.curJob.def.defName;
            }
            if (this.curDriver != null)
            {
                text = text + ", curDriver=" + this.curDriver.GetType();
            }
            Log.Error(text);
            if (this.curJob != null)
            {
                this.EndCurrentJob(JobCondition.Errored, false);
            }
            if (this.startingErrorRecoverJob)
            {
                Log.Error("An error occurred while starting an error recover job. We have to stop now to avoid infinite loops. This means that the Caravan is now jobless which can cause further bugs. Caravan=" + this.caravan.ToStringSafe<Caravan>());
            }
            else
            {
                this.startingErrorRecoverJob = true;
                try
                {
                    //this.StartJob(new Job(JobDefOf.Wait, 150, false), JobCondition.None, null, false, true, null, null);
                }
                finally
                {
                    this.startingErrorRecoverJob = false;
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
            return this.caravan.Spawned;
        }

        //private bool ShouldStartJobFromThinkTree(ThinkResult thinkResult)
        //{
        //    return this.curJob == null || (thinkResult.Job.def != this.curJob.def || thinkResult.SourceNode != this.caravan.mindState.lastJobGiver || !this.curDriver.IsContinuation(thinkResult.Job));
        //}

        public bool IsCurrentJobPlayerInterruptible()
        {
            return (this.curJob == null || this.curJob.def.playerInterruptible); //&& !this.caravan.HasAttachment(ThingDefOf.Fire);
        }

        public bool TryTakeOrderedJobPrioritizedWork(CaravanJob job, WorkGiver giver, IntVec3 cell)
        {
            if (this.TryTakeOrderedJob(job, giver.def.tagToGive))
            {
                //this.caravan.mindState.lastGivenWorkType = giver.def.workType;
                //if (giver.def.prioritizeSustains)
                //{
                //    this.caravan.mindState.priorityWork.Set(cell, giver.def.workType);
                //}
                return true;
            }
            return false;
        }

        public bool TryTakeOrderedJob(CaravanJob job, JobTag tag = JobTag.Misc)
        {
            if (this.debugLog)
            {
                this.DebugLogEvent("TakeOrderedJob " + job);
            }
            job.playerForced = true;
            if (this.curJob != null && this.curJob.JobIsSameAs(job))
            {
                return true;
            }
            this.caravan.pather.StopDead();
            //this.caravan.Map.CaravanDestinationManager.UnreserveAllFor(this.caravan);
            //if (job.def == CaravanJobDefOf.Goto)
            //{
            //    //this.caravan.Map.CaravanDestinationManager.ReserveDestinationFor(this.caravan, job.targetA.Cell);
            //}
            if (this.debugLog)
            {
                this.DebugLogEvent("    Queueing job");
            }
            this.jobQueue.Clear();
            this.jobQueue.EnqueueFirst(job, new JobTag?(tag));
            if (this.IsCurrentJobPlayerInterruptible())
            {
                if (this.curJob != null)
                {
                    this.curDriver.EndJobWith(JobCondition.InterruptForced);
                }
                else
                {
                    this.CheckForJobOverride();
                }
            }
            return true;
        }
        
        public void Notify_PathInterrupted()
        {
            this.EndCurrentJob(JobCondition.InterruptForced, false);
        }

        public void DebugLogEvent(string s)
        {
            if (this.debugLog)
            {
                Log.Message(string.Concat(new object[]
                {
                    Find.TickManager.TicksGame,
                    " ",
                    this.caravan,
                    ": ",
                    s
                }));
            }
        }
    }
}
