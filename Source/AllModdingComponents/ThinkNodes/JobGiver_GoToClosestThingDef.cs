using RimWorld;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class JobGiver_GoToClosestThingDef : ThinkNode_JobGiver
    {
        // Set via reflection
        private Danger maxDanger = Verse.Danger.Some;
        private ThingDef thingDef = ThingDefOf.PartySpot;

        protected override Job TryGiveJob(Pawn pawn)
        {
            var thing = ThingToDo(pawn);

            if (thing != null)
            {
                if (!thing.Position.IsValid)
                    return null;
                if (!pawn.CanReach(thing.Position, PathEndMode.ClosestTouch, maxDanger))
                {
                    return null;
                }

                if (IntVec3Utility.DistanceTo(thing.Position, pawn.Position) < 10f)
                {
                    return null;
                }

                var job = JobMaker.MakeJob(JobDefOf.Goto, thing.Position);
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                return job;
            }
            else
                return null;
        }

        private Thing ThingToDo(Pawn pawn)
        {
            var singleDef = WhatDef();

            var thingRequest = ThingRequest.ForDef(singleDef);
            var closestPosition = ClosestPosition(pawn, thingRequest);
            return closestPosition;
        }

        private Thing ClosestPosition(Pawn pawn, ThingRequest thingRequest)
        {
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                thingRequest, PathEndMode.Touch,
                Danger(pawn),
                200f);
        }

        private TraverseParms Danger(Pawn pawn)
        {
            return TraverseParms.For(pawn, maxDanger);
        }

        protected virtual ThingDef WhatDef()
        {
            return thingDef;
        }
    }
}
