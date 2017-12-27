using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public interface ICaravanJobEndable
    {
        Caravan GetActor();

        void AddEndCondition(Func<JobCondition> newEndCondition);
    }

    //Duplicate of RimWorld's Toil -- made for Caravans
    public class CaravanToil : ICaravanJobEndable
    {
        public Caravan actor;

        public bool atomicWithPrevious;

        public ToilCompleteMode defaultCompleteMode = ToilCompleteMode.Instant;

        public int defaultDuration;

        public List<Func<JobCondition>> endConditions = new List<Func<JobCondition>>();

        public List<Action> finishActions;

        public bool handlingFacing;

        public Action initAction;

        public List<Action> preInitActions;

        public List<Action> preTickActions;

        public RandomSocialMode socialMode = RandomSocialMode.Normal;

        public Action tickAction;

        public Caravan GetActor()
        {
            return actor;
        }

        public void AddEndCondition(Func<JobCondition> newEndCondition)
        {
            endConditions.Add(newEndCondition);
        }

        public void Cleanup()
        {
            if (finishActions != null)
                for (var i = 0; i < finishActions.Count; i++)
                    try
                    {
                        finishActions[i]();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Concat("Pawn ", actor,
                            " threw exception while executing toil's finish action (", i, "), curJob=",
                            Find.World.GetComponent<CaravanJobGiver>().CurJob(actor), ": ", ex));
                    }
        }

        public void AddFailCondition(Func<bool> newFailCondition)
        {
            endConditions.Add(delegate
            {
                if (newFailCondition())
                    return JobCondition.Incompletable;
                return JobCondition.Ongoing;
            });
        }

        public void AddPreInitAction(Action newAct)
        {
            if (preInitActions == null)
                preInitActions = new List<Action>();
            preInitActions.Add(newAct);
        }

        public void AddPreTickAction(Action newAct)
        {
            if (preTickActions == null)
                preTickActions = new List<Action>();
            preTickActions.Add(newAct);
        }

        public void AddFinishAction(Action newAct)
        {
            if (finishActions == null)
                finishActions = new List<Action>();
            finishActions.Add(newAct);
        }
    }
}