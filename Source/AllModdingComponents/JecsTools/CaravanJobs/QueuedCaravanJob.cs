using System;
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
            Scribe_Deep.Look<CaravanJob>(ref this.job, "job", new object[0]);
            Scribe_Values.Look<JobTag?>(ref this.tag, "tag", null, false);
        }
    }
}
