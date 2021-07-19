using RimWorld;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class JobGiver_Capture : ThinkNode_JobGiver
    {
        private float targetAcquireRadius; // set via reflection

        protected override Job TryGiveJob(Pawn pawn)
        {
            bool Validator(Thing t)
            {
                if (t == null)
                    return false;
                var p = (Pawn)t;
                var hostileTo = p.Faction == null || p.Faction.HostileTo(Faction.OfPlayer) || p.Faction.def.hidden;
                return p.Downed && hostileTo && !p.InBed() && pawn.CanReserve(p) && !p.IsForbidden(pawn);
            }

            var victim = (Pawn)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(pawn),
                targetAcquireRadius, Validator);
            if (victim == null)
            {
                return null;
            }

            var buildingBed = RestUtility.FindBedFor(victim, pawn, true, false) ??
                                       RestUtility.FindBedFor(victim, pawn, true, false, true);
            if (buildingBed == null)
            {
                return null;
            }

            var job = JobMaker.MakeJob(JobDefOf.Capture, victim, buildingBed);
            job.count = 1;
            return job;
        }
    }
}
