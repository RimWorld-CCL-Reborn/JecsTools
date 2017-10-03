using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class CaravanJobQueue : IExposable
    {
        private List<QueuedCaravanJob> jobs = new List<QueuedCaravanJob>();

        public int Count
        {
            get
            {
                return this.jobs.Count;
            }
        }

        public QueuedCaravanJob this[int index]
        {
            get
            {
                return this.jobs[index];
            }
        }

        public bool AnyPlayerForced
        {
            get
            {
                for (int i = 0; i < this.jobs.Count; i++)
                {
                    if (this.jobs[i].job.playerForced)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look<QueuedCaravanJob>(ref this.jobs, "jobs", LookMode.Deep, new object[0]);
        }

        public void EnqueueFirst(CaravanJob j, JobTag? tag = null)
        {
            this.jobs.Insert(0, new QueuedCaravanJob(j, tag));
        }

        public void EnqueueLast(CaravanJob j, JobTag? tag = null)
        {
            this.jobs.Add(new QueuedCaravanJob(j, tag));
        }

        public QueuedCaravanJob Dequeue()
        {
            if (this.jobs.NullOrEmpty<QueuedCaravanJob>())
            {
                return null;
            }
            QueuedCaravanJob result = this.jobs[0];
            this.jobs.RemoveAt(0);
            return result;
        }

        public QueuedCaravanJob Peek()
        {
            return this.jobs[0];
        }

        public void Clear()
        {
            this.jobs.Clear();
        }
    }
}
