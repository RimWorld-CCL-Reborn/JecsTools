using RimWorld.Planet;
using Verse.AI;

namespace JecsTools
{
    public static class CaravanToils_Goto
    {
        private static Caravan_JobTracker CurTracker(Caravan c)
        {
            return CaravanJobsUtility.GetCaravanJobGiver().Tracker(c);
        }

        private static CaravanJob CurJob(Caravan c)
        {
            return CaravanJobsUtility.GetCaravanJobGiver().CurJob(c);
        }

        public static CaravanToil Goto(TargetIndex ind, CaravanArrivalAction arrivalAction = null)
        {
            return GotoTile(ind, arrivalAction);
        }

        public static CaravanToil GotoObject(TargetIndex ind, CaravanArrivalAction arrivalAction = null)
        {
            var tileInt = -1;
            var toil = new CaravanToil();
            toil.initAction = () =>
            {
                //Log.Message("GoToObject1");
                tileInt = CurJob(toil.actor).GetTarget(ind).WorldObject.Tile;
                //Log.Message("GoToObject2");
                toil.actor.pather.StartPath(tileInt, arrivalAction, true);
                //Log.Message("GoToObject3");
            };
            toil.tickAction = () =>
            {
                if (tileInt < 0)
                    tileInt = CurJob(toil.actor).GetTarget(ind).WorldObject.Tile;
                if (toil.actor.Tile == tileInt)
                    CurTracker(toil.actor).curDriver.Notify_PatherArrived();
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            //toil.FailOnDespawnedOrNull(ind);
            return toil;
        }

        public static CaravanToil GotoTile(TargetIndex ind, CaravanArrivalAction arrivalAction = null)
        {
            var toil = new CaravanToil();
            toil.initAction = () =>
            {
                var actor = toil.actor;
                actor.pather.StartPath(CaravanJobsUtility.GetCaravanJobGiver().CurJob(actor).GetTarget(ind).Tile,
                    arrivalAction);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            //toil.FailOnDespawnedOrNull(ind);
            return toil;
        }
    }
}
