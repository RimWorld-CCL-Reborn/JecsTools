using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace JecsTools
{
    public class CaravanJobGiver : WorldComponent
    {
        public List<Caravan_JobTracker> jobTrackers = new List<Caravan_JobTracker>();
        //public Dictionary<Caravan, Caravan_JobTracker> jobTrackerSave = new Dictionary<Caravan, Caravan_JobTracker>();

        public CaravanJobGiver(World world) : base(world)
        {
        }

        public Caravan_JobTracker Tracker(Caravan caravan)
        {
            //Log.Message("JecsTools :: CaravanJobGiver :: Tracker Called");
            foreach (var t in jobTrackers)
            {
                if (t.Caravan == caravan)
                    return t;
            }
            var newTracker = new Caravan_JobTracker(caravan);
            jobTrackers.Add(newTracker);
            return newTracker;
        }

        public CaravanJob CurJob(Caravan caravan)
        {
            //Log.Message("JecsTools :: CaravanJobGiver :: CurJob Called");
            return Tracker(caravan).curJob;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            jobTrackers.RemoveAll(t => t.Caravan == null || !t.Caravan.Spawned);
            foreach (var t in jobTrackers)
                t.JobTrackerTick();
        }

        //private List<Caravan> jobTrackersKeysWorkingList;
        //private List<Caravan_JobTracker> jobTrackersValuesWorkingList;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref jobTrackers, "jobTrackers", LookMode.Deep);
            //if (Scribe.mode == LoadSaveMode.Saving)
            //{
            //    jobTrackerSave.Clear();
            //    jobTrackerSave = new Dictionary<Caravan, Caravan_JobTracker>();
            //    if (!jobTrackers.NullOrEmpty())
            //    {
            //        foreach (Caravan_JobTracker j in jobTrackers)
            //        {
            //            jobTrackerSave.Add(j.Caravan, j);
            //        }
            //    }
            //}
            //Scribe_Collections.Look(ref this.jobTrackerSave, "jobTrackerSave", LookMode.Reference, LookMode.Deep,
            //    ref this.jobTrackersKeysWorkingList, ref this.jobTrackersValuesWorkingList);
            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            //{
            //    if (jobTrackerSave != null && jobTrackerSave.Count > 0)
            //    {
            //        foreach (KeyValuePair<Caravan, Caravan_JobTracker> pair in jobTrackerSave)
            //        {
            //            jobTrackers.Add()
            //        }
            //    }
            //}
        }
    }
}
