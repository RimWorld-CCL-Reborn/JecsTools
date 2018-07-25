using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public static class FirelessTrashUtility
    {
        // RimWorld.TrashUtility
        private static readonly IntRange TrashJobCheckOverrideInterval = new IntRange(450, 500);

        // RimWorld.TrashUtility
        public static Job TrashJob(Pawn pawn, Thing t)
        {
#pragma warning disable IDE0019 // Use pattern matching
            var plant = t as Plant;
#pragma warning restore IDE0019 // Use pattern matching
            if (plant != null)
            {
                var job = new Job(JobDefOf.AttackMelee, t);
                FinalizeTrashJob(job);
                return job;
            }
            if (pawn.equipment != null && Rand.Value < 0.7f)
                foreach (var current in pawn.equipment.AllEquipmentVerbs)
                    if (current.verbProps.ai_IsBuildingDestroyer)
                    {
                        var job2 = new Job(JobDefOf.UseVerbOnThing, t);
                        job2.verbToUse = current;
                        FinalizeTrashJob(job2);
                        return job2;
                    }
            var value = Rand.Value;
            var job3 = new Job(JobDefOf.AttackMelee, t);
            FinalizeTrashJob(job3);
            return job3;
        }

        // RimWorld.TrashUtility
        private static void FinalizeTrashJob(Job job)
        {
            job.expiryInterval = TrashJobCheckOverrideInterval.RandomInRange;
            job.checkOverrideOnExpire = true;
            job.expireRequiresEnemiesNearby = true;
        }


        // RimWorld.TrashUtility
        public static bool ShouldTrashBuilding(Pawn pawn, Building b)
        {
            if (!b.def.useHitPoints)
                return false;
            if (b.def.building.isInert || b.def.building.isTrap)
            {
                var num = GenLocalDate.HourOfDay(pawn) / 3;
                var specialSeed = (b.GetHashCode() * 612361) ^ (pawn.GetHashCode() * 391) ^ (num * 734273247);
                if (!Rand.ChanceSeeded(0.008f, specialSeed))
                    return false;
            }
            return (!b.def.building.isTrap && pawn.HostileTo(b));
        }


        // RimWorld.TrashUtility
        public static bool ShouldTrashPlant(Pawn pawn, Plant p)
        {
            if (!p.sown || p.def.plant.IsTree || !p.FlammableNow || !CanTrash(pawn, p))
                return false;
            var iterator = CellRect.CenteredOn(p.Position, 2).ClipInsideMap(p.Map).GetIterator();
            while (!iterator.Done())
            {
                var current = iterator.Current;
                if (current.InBounds(p.Map) && current.ContainsStaticFire(p.Map))
                    return false;
                iterator.MoveNext();
            }
            return p.Position.Roofed(p.Map) || p.Map.weatherManager.RainRate <= 0.25f;
        }

        // RimWorld.TrashUtility
        private static bool CanTrash(Pawn pawn, Thing t)
        {
            return pawn.CanReach(t, PathEndMode.Touch, Danger.Some, false, TraverseMode.ByPawn) && !t.IsBurning();
        }
    }
}