using Verse;
using Verse.AI;

namespace JecsTools
{
    public class QueuedCaravanJob : IExposable
    {
        public CaravanJob job;

        public JobTag? tag;

        public QueuedCaravanJob()
        {
        }

        public QueuedCaravanJob(CaravanJob job, JobTag? tag)
        {
            this.job = job;
            this.tag = tag;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref job, "job");
            Scribe_Values.Look(ref tag, "tag", null, false);
        }
    }
}