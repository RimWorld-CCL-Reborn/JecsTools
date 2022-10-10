using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CompDelayedSpawner
{
    public class SpawnInfo
    {
        public FactionDef faction = null;
        public int num = 1;
        public PawnKindDef pawnKind = null;
        public ThingDef thing = null;
        public List<HediffDef> withHediffs = new List<HediffDef>();
        public MentalStateDef withMentalState = null;
    }

    public class CompProperties_DelayedSpawner : CompProperties
    {
        public bool destroyAfterSpawn = true;
        public List<SpawnInfo> spawnList = new List<SpawnInfo>();
        public bool spawnsOnce = true;
        public int tickRate = GenTicks.TicksPerRealSecond;
        public int ticksUntilSpawning = 30; // in seconds

        public CompProperties_DelayedSpawner()
        {
            compClass = typeof(CompDelayedSpawner);
        }
    }
}
