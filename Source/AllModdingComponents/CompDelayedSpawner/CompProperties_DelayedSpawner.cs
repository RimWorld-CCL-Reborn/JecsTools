using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace CompDelayedSpawner
{
    public class SpawnInfo
    {
        public ThingDef thing = null;
        public PawnKindDef pawnKind = null;
        public FactionDef faction = null;
        public MentalStateDef withMentalState = null;
        public List<HediffDef> withHediffs = new List<HediffDef>();
        public int num = 1;
    }

    public class CompProperties_DelayedSpawner : CompProperties
    {
        public List<SpawnInfo> spawnList = new List<SpawnInfo>();
        public int tickRate = 60; //1 second
        public int ticksUntilSpawning = 30; //30 seconds
        public bool spawnsOnce = true;
        public bool destroyAfterSpawn = true;

        public CompProperties_DelayedSpawner()
        {
            this.compClass = typeof(CompDelayedSpawner);
        }
    }
}
