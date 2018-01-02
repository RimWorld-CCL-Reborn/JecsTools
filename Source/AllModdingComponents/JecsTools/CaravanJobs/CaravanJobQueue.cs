using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class CaravanJobQueue : IExposable
    {
        private List<QueuedCaravanJob> jobs = new List<QueuedCaravanJob>();

        public int Count => jobs.Count;

        public QueuedCaravanJob this[int index] => jobs[index];

        public bool AnyPlayerForced
        {
            get
            {
                for (var i = 0; i < jobs.Count; i++)
                    if (jobs[i].job.playerForced)
                        return true;
                return false;
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref jobs, "jobs", LookMode.Deep);
        }

        public void EnqueueFirst(CaravanJob j, JobTag? tag = null)
        {
            jobs.Insert(0, new QueuedCaravanJob(j, tag));
        }

        public void EnqueueLast(CaravanJob j, JobTag? tag = null)
        {
            jobs.Add(new QueuedCaravanJob(j, tag));
        }

        public QueuedCaravanJob Dequeue()
        {
            if (jobs.NullOrEmpty())
                return null;
            var result = jobs[0];
            jobs.RemoveAt(0);
            return result;
        }

        public QueuedCaravanJob Peek()
        {
            return jobs[0];
        }

        public void Clear()
        {
            jobs.Clear();
        }
    }
}