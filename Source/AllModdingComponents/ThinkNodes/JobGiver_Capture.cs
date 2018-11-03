using RimWorld;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class JobGiver_Capture : ThinkNode_JobGiver
    {
        private float targetAcquireRadius;

        protected override Job TryGiveJob(Pawn pawn)
        {
            bool Validator(Thing t)
            {
                Pawn pawn3 = (Pawn) t;
                if (pawn3 == null) return false;
                var hostileTo = pawn3.Faction == null || pawn3.Faction.HostileTo(Faction.OfPlayer) || pawn3.Faction.def.hidden;
                return pawn3.Downed && hostileTo && !pawn3.InBed() && pawn.CanReserve(pawn3) && !pawn3.IsForbidden(pawn);
            }

            Pawn victim = (Pawn)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, 
                ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(pawn),
                targetAcquireRadius, Validator);
            if (victim == null)
            {
                return null;
            }
            
            Building_Bed buildingBed = RestUtility.FindBedFor(victim, pawn, true, false) ??
                                       RestUtility.FindBedFor(victim, pawn, true, false, true);
            if (buildingBed == null)
            {
                return null;
            }

            var job = new Job(JobDefOf.Capture, victim, buildingBed) {count = 1};

            return job;
        }
    }
}