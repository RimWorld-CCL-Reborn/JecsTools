using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public abstract class CaravanJobDriver : ICaravanJobEndable, IExposable
    {
        public Caravan caravan;

        private List<CaravanToil> toils = new List<CaravanToil>();

        public List<Func<JobCondition>> globalFailConditions = new List<Func<JobCondition>>();

        public List<Action> globalFinishActions = new List<Action>();

        public bool ended;

        private int curToilIndex = -1;

        private ToilCompleteMode curToilCompleteMode;

        public int ticksLeftThisToil = 99999;

        private bool wantBeginNextToil;

        public int startTick = -1;

        public TargetIndex rotateToFace = TargetIndex.A;

        public LayingDownState layingDown;

        public bool asleep;

        public float uninstallWorkLeft;

        public int debugTicksSpentThisToil;

        public CaravanToil CurToil
        {
            get
            {
                if (this.curToilIndex < 0)
                {
                    return null;
                }
                if (this.curToilIndex >= this.toils.Count)
                {
                    Log.Error(string.Concat(new object[]
                    {
                        this.caravan,
                        " with job ",
                        CurJob,
                        " tried to get CurToil with curToilIndex=",
                        this.curToilIndex,
                        " but only has ",
                        this.toils.Count,
                        " toils."
                    }));
                    return null;
                }
                return this.toils[this.curToilIndex];
            }
        }

        public bool HaveCurToil
        {
            get
            {
                return this.curToilIndex >= 0 && this.curToilIndex < this.toils.Count;
            }
        }

        private bool CanStartNextToilInBusyStance
        {
            get
            {
                int num = this.curToilIndex + 1;
                return num < this.toils.Count && this.toils[num].atomicWithPrevious;
            }
        }

        public virtual PawnPosture Posture
        {
            get
            {
                return (this.layingDown == LayingDownState.NotLaying) ? PawnPosture.Standing : PawnPosture.LayingAny;
            }
        }

        public int CurToilIndex
        {
            get
            {
                return this.curToilIndex;
            }
        }

        public bool HandlingFacing
        {
            get
            {
                return this.CurToil != null && this.CurToil.handlingFacing;
            }
        }

        public CaravanJob CurJob
        {
            get
            {
                return (Find.World.GetComponent<CaravanJobGiver>().CurJob(this.caravan));
            }
        }

        public GlobalTargetInfo TargetA
        {
            get
            {
                return CurJob.targetA;
            }
        }

        public GlobalTargetInfo TargetB
        {
            get
            {
                return CurJob.targetB;
            }
        }

        public GlobalTargetInfo TargetC
        {
            get
            {
                return CurJob.targetC;
            }
        }

        public Thing TargetThingA
        {
            get
            {
                return CurJob.targetA.Thing;
            }
            set
            {
                CurJob.targetA = value;
            }
        }

        public Thing TargetThingB
        {
            get
            {
                return CurJob.targetB.Thing;
            }
            set
            {
                CurJob.targetB = value;
            }
        }

        public IntVec3 TargetLocA
        {
            get
            {
                return CurJob.targetA.Cell;
            }
        }

        public int TargetTileA
        {
            get
            {
                return CurJob.targetA.Tile;
            }
        }


        public int TargetTileB
        {
            get
            {
                return CurJob.targetB.Tile;
            }
        }


        public int TargetTileC
        {
            get
            {
                return CurJob.targetC.Tile;
            }
        }

        //public Map Map
        //{
        //    get
        //    {
        //        return this.caravan.Map;
        //    }
        //}

        public virtual string GetReport()
        {
            return this.ReportStringProcessed(this.CurJob.def.reportString);
        }

        public string ReportStringProcessed(string str)
        {
            CaravanJob curJob = this.CurJob;
            if (curJob.targetA.HasThing)
            {
                str = str.Replace("TargetA", curJob.targetA.Thing.LabelShort);
            }
            else
            {
                str = str.Replace("TargetA", "AreaLower".Translate());
            }
            if (curJob.targetB.HasThing)
            {
                str = str.Replace("TargetB", curJob.targetB.Thing.LabelShort);
            }
            else
            {
                str = str.Replace("TargetB", "AreaLower".Translate());
            }
            if (curJob.targetC.HasThing)
            {
                str = str.Replace("TargetC", curJob.targetC.Thing.LabelShort);
            }
            else
            {
                str = str.Replace("TargetC", "AreaLower".Translate());
            }
            return str;
        }

        public abstract IEnumerable<CaravanToil> MakeNewToils();

        public virtual void ExposeData()
        {
            Scribe_References.Look<Caravan>(ref this.caravan, "caravan");
            Scribe_Values.Look<bool>(ref this.ended, "ended", false, false);
            Scribe_Values.Look<int>(ref this.curToilIndex, "curToilIndex", 0, true);
            Scribe_Values.Look<int>(ref this.ticksLeftThisToil, "ticksLeftThisToil", 0, false);
            Scribe_Values.Look<bool>(ref this.wantBeginNextToil, "wantBeginNextToil", false, false);
            Scribe_Values.Look<ToilCompleteMode>(ref this.curToilCompleteMode, "curToilCompleteMode", ToilCompleteMode.Undefined, false);
            Scribe_Values.Look<int>(ref this.startTick, "startTick", 0, false);
            Scribe_Values.Look<TargetIndex>(ref this.rotateToFace, "rotateToFace", TargetIndex.A, false);
            Scribe_Values.Look<LayingDownState>(ref this.layingDown, "layingDown", LayingDownState.NotLaying, false);
            Scribe_Values.Look<bool>(ref this.asleep, "asleep", false, false);
            Scribe_Values.Look<float>(ref this.uninstallWorkLeft, "uninstallWorkLeft", 0f, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.SetupToils();
            }
        }

        public void Cleanup(JobCondition condition)
        {
            for (int i = 0; i < this.globalFinishActions.Count; i++)
            {
                try
                {
                    this.globalFinishActions[i]();
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Pawn ",
                        this.caravan,
                        " threw exception while executing a global finish action (",
                        i,
                        "), jobDriver=",
                        base.GetType(),
                        ": ",
                        ex
                    }));
                }
            }
            if (this.HaveCurToil)
            {
                this.CurToil.Cleanup();
            }
        }

        internal void SetupToils()
        {
            try
            {
                this.toils.Clear();
                foreach (CaravanToil current in this.MakeNewToils())
                {
                    if (current.defaultCompleteMode == ToilCompleteMode.Undefined)
                    {
                        Log.Error("Toil has undefined complete mode.");
                        current.defaultCompleteMode = ToilCompleteMode.Instant;
                    }
                    current.actor = this.caravan;
                    this.toils.Add(current);
                }
            }
            catch (Exception ex)
            {
                Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).StartErrorRecoverJob(string.Concat(new object[]
                {
                    "Exception in SetupToils (pawn=",
                    this.caravan,
                    ", job=",
                    this.CurJob,
                    "): ",
                    ex
                }));
            }
        }

        public void DriverTick()
        {
            try
            {
                this.ticksLeftThisToil--;
                this.debugTicksSpentThisToil++;
                if (this.CurToil == null)
                {
                    //if (!this.caravan.stances.FullBodyBusy || this.CanStartNextToilInBusyStance)
                    //{
                        this.ReadyForNextToil();
                    //}
                }
                else if (!this.CheckCurrentToilEndOrFail())
                {
                    if (this.curToilCompleteMode == ToilCompleteMode.Delay)
                    {
                        if (this.ticksLeftThisToil <= 0)
                        {
                            this.ReadyForNextToil();
                            return;
                        }
                    }
                    else if (this.curToilCompleteMode == ToilCompleteMode.FinishedBusy) //&& !this.caravan.stances.FullBodyBusy)
                    {
                        this.ReadyForNextToil();
                        return;
                    }
                    if (this.wantBeginNextToil)
                    {
                        this.TryActuallyStartNextToil();
                    }
                    else if (this.curToilCompleteMode == ToilCompleteMode.Instant && this.debugTicksSpentThisToil > 300)
                    {
                        Log.Error(string.Concat(new object[]
                        {
                            this.caravan,
                            " had to be broken from frozen state. He was doing job ",
                            this.CurJob,
                            ", toilindex=",
                            this.curToilIndex
                        }));
                        this.ReadyForNextToil();
                    }
                    else
                    {
                        CaravanJob curJob = this.CurJob;
                        if (this.CurToil.preTickActions != null)
                        {
                            CaravanToil curToil = this.CurToil;
                            for (int i = 0; i < curToil.preTickActions.Count; i++)
                            {
                                curToil.preTickActions[i]();
                                if (this.CurJob != curJob)
                                {
                                    return;
                                }
                                if (this.CurToil != curToil || this.wantBeginNextToil)
                                {
                                    return;
                                }
                            }
                        }
                        if (this.CurToil.tickAction != null)
                        {
                            this.CurToil.tickAction();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).StartErrorRecoverJob(string.Concat(new object[]
                {
                    "Exception in Tick (pawn=",
                    this.caravan,
                    ", job=",
                    this.CurJob,
                    ", CurToil=",
                    this.curToilIndex,
                    "): ",
                    ex
                }));
            }
        }

        public void ReadyForNextToil()
        {
            this.wantBeginNextToil = true;
            this.TryActuallyStartNextToil();
        }

        private void TryActuallyStartNextToil()
        {
            if (!this.caravan.Spawned)
            {
                return;
            }
            if (this.HaveCurToil)
            {
                this.CurToil.Cleanup();
            }
            this.curToilIndex++;
            this.wantBeginNextToil = false;
            if (!this.HaveCurToil)
            {
                this.EndJobWith(JobCondition.Succeeded);
                return;
            }
            this.debugTicksSpentThisToil = 0;
            this.ticksLeftThisToil = this.CurToil.defaultDuration;
            this.curToilCompleteMode = this.CurToil.defaultCompleteMode;
            if (!this.CheckCurrentToilEndOrFail())
            {
                int num = this.CurToilIndex;
                if (this.CurToil.preInitActions != null)
                {
                    for (int i = 0; i < this.CurToil.preInitActions.Count; i++)
                    {
                        this.CurToil.preInitActions[i]();
                        if (this.CurToilIndex != num)
                        {
                            break;
                        }
                    }
                }
                if (this.CurToilIndex == num)
                {
                    if (this.CurToil.initAction != null)
                    {
                        try
                        {
                            this.CurToil.initAction();
                        }
                        catch (Exception ex)
                        {
                            Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).StartErrorRecoverJob(string.Concat(new object[]
                            {
                                "JobDriver threw exception in initAction. Pawn=",
                                this.caravan,
                                ", Job=",
                                this.CurJob,
                                ", Exception: ",
                                ex
                            }));
                            return;
                        }
                    }
                    if (this.CurToilIndex == num && !this.ended && this.curToilCompleteMode == ToilCompleteMode.Instant)
                    {
                        this.ReadyForNextToil();
                    }
                }
            }
        }

        public void EndJobWith(JobCondition condition)
        {
            if (condition == JobCondition.Ongoing)
            {
                Log.Warning("Ending a job with Ongoing as the condition. This makes no sense.");
            }
            if (this.caravan.Spawned)
            {
                Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).EndCurrentJob(condition, true);
            }
        }

        private bool CheckCurrentToilEndOrFail()
        {
            CaravanToil curToil = this.CurToil;
            if (this.globalFailConditions != null)
            {
                for (int i = 0; i < this.globalFailConditions.Count; i++)
                {
                    JobCondition jobCondition = this.globalFailConditions[i]();
                    if (jobCondition != JobCondition.Ongoing)
                    {
                        this.EndJobWith(jobCondition);
                        return true;
                    }
                }
            }
            if (curToil != null && curToil.endConditions != null)
            {
                for (int j = 0; j < curToil.endConditions.Count; j++)
                {
                    JobCondition jobCondition2 = curToil.endConditions[j]();
                    if (jobCondition2 != JobCondition.Ongoing)
                    {
                        this.EndJobWith(jobCondition2);
                        return true;
                    }
                }
            }
            return false;
        }

        private void SetNextToil(CaravanToil to)
        {
            this.curToilIndex = this.toils.IndexOf(to) - 1;
        }

        public void JumpToToil(CaravanToil to)
        {
            this.SetNextToil(to);
            this.ReadyForNextToil();
        }

        public virtual void Notify_Starting()
        {
            this.startTick = Find.TickManager.TicksGame;
        }

        public virtual void Notify_LastPosture(PawnPosture posture, LayingDownState layingDown)
        {
        }

        public virtual void Notify_PatherArrived()
        {
            if (this.curToilCompleteMode == ToilCompleteMode.PatherArrival)
            {
                this.ReadyForNextToil();
            }
        }

        public virtual void Notify_PatherFailed()
        {
            this.EndJobWith(JobCondition.ErroredPather);
        }

        public virtual void Notify_StanceChanged()
        {
        }

        public Caravan GetActor()
        {
            return this.caravan;
        }

        public void AddEndCondition(Func<JobCondition> newEndCondition)
        {
            this.globalFailConditions.Add(newEndCondition);
        }

        public void AddFailCondition(Func<bool> newFailCondition)
        {
            this.globalFailConditions.Add(delegate
            {
                if (newFailCondition())
                {
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            });
        }

        public void AddFinishAction(Action newAct)
        {
            this.globalFinishActions.Add(newAct);
        }

        public virtual bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            return false;
        }

        public virtual RandomSocialMode DesiredSocialMode()
        {
            if (this.CurToil != null)
            {
                return this.CurToil.socialMode;
            }
            return RandomSocialMode.Normal;
        }

        public virtual bool IsContinuation(Job j)
        {
            return true;
        }
    }
}
