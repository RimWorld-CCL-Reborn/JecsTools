using System.Collections.Generic;
using System.Linq;
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

        public void NullHandler(Caravan caravan)
        {
            if (jobTrackers.FirstOrDefault(x => x.Caravan == caravan) == null)
                jobTrackers.Add(new Caravan_JobTracker(caravan));
        }

        public Caravan_JobTracker Tracker(Caravan caravan)
        {
            NullHandler(caravan);
            //Log.Message("JecsTools :: CaravanJobGiver :: Tracker Called");
            return jobTrackers.FirstOrDefault(x => x.Caravan == caravan);
        }

        public CaravanJob CurJob(Caravan caravan)
        {
            NullHandler(caravan);
            //Log.Message("JecsTools :: CaravanJobGiver :: CurJob Called");
            return jobTrackers.FirstOrDefault(x => x.Caravan == caravan).curJob;
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (jobTrackers != null && jobTrackers.Count > 0)
            {
                Caravan_JobTracker toRemove = null;
                foreach (var t in jobTrackers)
                    if (t.Caravan == null || !t.Caravan.Spawned) toRemove = t;
                    else t.JobTrackerTick();
                if (toRemove != null) jobTrackers.Remove(toRemove);
            }
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
            //Scribe_Collections.Look<Caravan, Caravan_JobTracker>(ref this.jobTrackerSave, "jobTrackerSave", LookMode.Reference, LookMode.Deep,
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