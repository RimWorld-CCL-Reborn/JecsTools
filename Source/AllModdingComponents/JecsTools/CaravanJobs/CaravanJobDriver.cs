using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public enum LayingDownState
    {
        NotLaying,
        LayingSurface,
        LayingInBed
    }
    
    public abstract class CaravanJobDriver : ICaravanJobEndable, IExposable
    {
        public bool asleep;
        public Caravan caravan;

        private ToilCompleteMode curToilCompleteMode;

        private int curToilIndex = -1;

        public int debugTicksSpentThisToil;

        public bool ended;

        public List<Func<JobCondition>> globalFailConditions = new List<Func<JobCondition>>();

        public List<Action> globalFinishActions = new List<Action>();

        public LayingDownState layingDown;

        public TargetIndex rotateToFace = TargetIndex.A;

        public int startTick = -1;

        public int ticksLeftThisToil = 99999;

        private readonly List<CaravanToil> toils = new List<CaravanToil>();

        public float uninstallWorkLeft;

        private bool wantBeginNextToil;

        public CaravanToil CurToil
        {
            get
            {
                if (curToilIndex < 0)
                    return null;
                if (curToilIndex >= toils.Count)
                {
                    Log.Error(string.Concat(caravan, " with job ", CurJob, " tried to get CurToil with curToilIndex=",
                        curToilIndex, " but only has ", toils.Count, " toils."));
                    return null;
                }
                return toils[curToilIndex];
            }
        }

        public bool HaveCurToil => curToilIndex >= 0 && curToilIndex < toils.Count;

        private bool CanStartNextToilInBusyStance
        {
            get
            {
                var num = curToilIndex + 1;
                return num < toils.Count && toils[num].atomicWithPrevious;
            }
        }

        public virtual PawnPosture Posture =>
            layingDown == LayingDownState.NotLaying ? PawnPosture.Standing : PawnPosture.LayingOnGroundNormal;

        public int CurToilIndex => curToilIndex;

        public bool HandlingFacing => CurToil != null && CurToil.handlingFacing;

        public CaravanJob CurJob => Find.World.GetComponent<CaravanJobGiver>().CurJob(caravan);

        public GlobalTargetInfo TargetA => CurJob.targetA;

        public GlobalTargetInfo TargetB => CurJob.targetB;

        public GlobalTargetInfo TargetC => CurJob.targetC;

        public Thing TargetThingA
        {
            get => CurJob.targetA.Thing;
            set => CurJob.targetA = value;
        }

        public Thing TargetThingB
        {
            get => CurJob.targetB.Thing;
            set => CurJob.targetB = value;
        }

        public IntVec3 TargetLocA => CurJob.targetA.Cell;

        public int TargetTileA => CurJob.targetA.Tile;


        public int TargetTileB => CurJob.targetB.Tile;


        public int TargetTileC => CurJob.targetC.Tile;

        public Caravan GetActor()
        {
            return caravan;
        }

        public void AddEndCondition(Func<JobCondition> newEndCondition)
        {
            globalFailConditions.Add(newEndCondition);
        }

        public virtual void ExposeData()
        {
            Scribe_References.Look(ref caravan, "caravan");
            Scribe_Values.Look(ref ended, "ended", false, false);
            Scribe_Values.Look(ref curToilIndex, "curToilIndex", 0, true);
            Scribe_Values.Look(ref ticksLeftThisToil, "ticksLeftThisToil", 0, false);
            Scribe_Values.Look(ref wantBeginNextToil, "wantBeginNextToil", false, false);
            Scribe_Values.Look(ref curToilCompleteMode, "curToilCompleteMode", ToilCompleteMode.Undefined, false);
            Scribe_Values.Look(ref startTick, "startTick", 0, false);
            Scribe_Values.Look(ref rotateToFace, "rotateToFace", TargetIndex.A, false);
            Scribe_Values.Look(ref layingDown, "layingDown", LayingDownState.NotLaying, false);
            Scribe_Values.Look(ref asleep, "asleep", false, false);
            Scribe_Values.Look(ref uninstallWorkLeft, "uninstallWorkLeft", 0f, false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                SetupToils();
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
            return ReportStringProcessed(CurJob.def.reportString);
        }

        public string ReportStringProcessed(string str)
        {
            var curJob = CurJob;
            if (curJob.targetA.HasThing)
                str = str.Replace("TargetA", curJob.targetA.Thing.LabelShort);
            else
                str = str.Replace("TargetA", "AreaLower".Translate());
            if (curJob.targetB.HasThing)
                str = str.Replace("TargetB", curJob.targetB.Thing.LabelShort);
            else
                str = str.Replace("TargetB", "AreaLower".Translate());
            if (curJob.targetC.HasThing)
                str = str.Replace("TargetC", curJob.targetC.Thing.LabelShort);
            else
                str = str.Replace("TargetC", "AreaLower".Translate());
            return str;
        }

        public abstract IEnumerable<CaravanToil> MakeNewToils();

        public void Cleanup(JobCondition condition)
        {
            for (var i = 0; i < globalFinishActions.Count; i++)
                try
                {
                    globalFinishActions[i]();
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat("Pawn ", caravan,
                        " threw exception while executing a global finish action (", i, "), jobDriver=", GetType(),
                        ": ", ex));
                }
            if (HaveCurToil)
                CurToil.Cleanup();
        }

        internal void SetupToils()
        {
            try
            {
                toils.Clear();
                foreach (var current in MakeNewToils())
                {
                    if (current.defaultCompleteMode == ToilCompleteMode.Undefined)
                    {
                        Log.Error("Toil has undefined complete mode.");
                        current.defaultCompleteMode = ToilCompleteMode.Instant;
                    }
                    current.actor = caravan;
                    toils.Add(current);
                }
            }
            catch (Exception ex)
            {
                Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).StartErrorRecoverJob(
                    string.Concat("Exception in SetupToils (pawn=", caravan, ", job=", CurJob, "): ", ex));
            }
        }

        public void DriverTick()
        {
            try
            {
                ticksLeftThisToil--;
                debugTicksSpentThisToil++;
                if (CurToil == null)
                {
                    //if (!this.caravan.stances.FullBodyBusy || this.CanStartNextToilInBusyStance)
                    //{
                    ReadyForNextToil();
                    //}
                }
                else if (!CheckCurrentToilEndOrFail())
                {
                    if (curToilCompleteMode == ToilCompleteMode.Delay)
                    {
                        if (ticksLeftThisToil <= 0)
                        {
                            ReadyForNextToil();
                            return;
                        }
                    }
                    else if (curToilCompleteMode == ToilCompleteMode.FinishedBusy
                    ) //&& !this.caravan.stances.FullBodyBusy)
                    {
                        ReadyForNextToil();
                        return;
                    }
                    if (wantBeginNextToil)
                    {
                        TryActuallyStartNextToil();
                    }
                    else if (curToilCompleteMode == ToilCompleteMode.Instant && debugTicksSpentThisToil > 300)
                    {
                        Log.Error(string.Concat(caravan, " had to be broken from frozen state. He was doing job ",
                            CurJob, ", toilindex=", curToilIndex));
                        ReadyForNextToil();
                    }
                    else
                    {
                        var curJob = CurJob;
                        if (CurToil.preTickActions != null)
                        {
                            var curToil = CurToil;
                            for (var i = 0; i < curToil.preTickActions.Count; i++)
                            {
                                curToil.preTickActions[i]();
                                if (CurJob != curJob)
                                    return;
                                if (CurToil != curToil || wantBeginNextToil)
                                    return;
                            }
                        }
                        if (CurToil.tickAction != null)
                            CurToil.tickAction();
                    }
                }
            }
            catch (Exception ex)
            {
                Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).StartErrorRecoverJob(
                    string.Concat("Exception in Tick (pawn=", caravan, ", job=", CurJob, ", CurToil=", curToilIndex,
                        "): ", ex));
            }
        }

        public void ReadyForNextToil()
        {
            wantBeginNextToil = true;
            TryActuallyStartNextToil();
        }

        private void TryActuallyStartNextToil()
        {
            if (!caravan.Spawned)
                return;
            if (HaveCurToil)
                CurToil.Cleanup();
            curToilIndex++;
            wantBeginNextToil = false;
            if (!HaveCurToil)
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }
            debugTicksSpentThisToil = 0;
            ticksLeftThisToil = CurToil.defaultDuration;
            curToilCompleteMode = CurToil.defaultCompleteMode;
            if (!CheckCurrentToilEndOrFail())
            {
                var num = CurToilIndex;
                if (CurToil.preInitActions != null)
                    for (var i = 0; i < CurToil.preInitActions.Count; i++)
                    {
                        CurToil.preInitActions[i]();
                        if (CurToilIndex != num)
                            break;
                    }
                if (CurToilIndex == num)
                {
                    if (CurToil.initAction != null)
                        try
                        {
                            CurToil.initAction();
                        }
                        catch (Exception ex)
                        {
                            Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan)
                                .StartErrorRecoverJob(string.Concat("JobDriver threw exception in initAction. Pawn=",
                                    caravan, ", Job=", CurJob, ", Exception: ", ex));
                            return;
                        }
                    if (CurToilIndex == num && !ended && curToilCompleteMode == ToilCompleteMode.Instant)
                        ReadyForNextToil();
                }
            }
        }

        public void EndJobWith(JobCondition condition)
        {
            if (condition == JobCondition.Ongoing)
                Log.Warning("Ending a job with Ongoing as the condition. This makes no sense.");
            if (caravan.Spawned)
                Find.World.GetComponent<CaravanJobGiver>().Tracker(caravan).EndCurrentJob(condition, true);
        }

        private bool CheckCurrentToilEndOrFail()
        {
            var curToil = CurToil;
            if (globalFailConditions != null)
                for (var i = 0; i < globalFailConditions.Count; i++)
                {
                    var jobCondition = globalFailConditions[i]();
                    if (jobCondition != JobCondition.Ongoing)
                    {
                        EndJobWith(jobCondition);
                        return true;
                    }
                }
            if (curToil != null && curToil.endConditions != null)
                for (var j = 0; j < curToil.endConditions.Count; j++)
                {
                    var jobCondition2 = curToil.endConditions[j]();
                    if (jobCondition2 != JobCondition.Ongoing)
                    {
                        EndJobWith(jobCondition2);
                        return true;
                    }
                }
            return false;
        }

        private void SetNextToil(CaravanToil to)
        {
            curToilIndex = toils.IndexOf(to) - 1;
        }

        public void JumpToToil(CaravanToil to)
        {
            SetNextToil(to);
            ReadyForNextToil();
        }

        public virtual void Notify_Starting()
        {
            startTick = Find.TickManager.TicksGame;
        }

        public virtual void Notify_LastPosture(PawnPosture posture, LayingDownState layingDown)
        {
        }

        public virtual void Notify_PatherArrived()
        {
            if (curToilCompleteMode == ToilCompleteMode.PatherArrival)
                ReadyForNextToil();
        }

        public virtual void Notify_PatherFailed()
        {
            EndJobWith(JobCondition.ErroredPather);
        }

        public virtual void Notify_StanceChanged()
        {
        }

        public void AddFailCondition(Func<bool> newFailCondition)
        {
            globalFailConditions.Add(delegate
            {
                if (newFailCondition())
                    return JobCondition.Incompletable;
                return JobCondition.Ongoing;
            });
        }

        public void AddFinishAction(Action newAct)
        {
            globalFinishActions.Add(newAct);
        }

        public virtual bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            return false;
        }

        public virtual RandomSocialMode DesiredSocialMode()
        {
            if (CurToil != null)
                return CurToil.socialMode;
            return RandomSocialMode.Normal;
        }

        public virtual bool IsContinuation(Job j)
        {
            return true;
        }
    }
}