using System;
using System.Linq;
using Verse;

namespace CompDelayedSpawner
{
    public class CompDelayedSpawner : ThingComp
    {
        private bool isSpawning;
        private int ticksLeft = -999;


        public CompProperties_DelayedSpawner Props =>
            props as CompProperties_DelayedSpawner;

        public Map Map => parent.MapHeld;
        public IntVec3 Position => parent.PositionHeld;


        public override void CompTick()
        {
            base.CompTick();
            //Log.Message("Tick");
            if (parent.Spawned && !isSpawning && Find.TickManager.TicksGame % Props.tickRate == 0)
            {
                //Log.Message("Tick2");

                if (ticksLeft == -999)
                    ticksLeft = Props.ticksUntilSpawning;

                if (ticksLeft <= 0)
                    Spawn();

                ticksLeft--;
            }
        }

        public void Spawn()
        {
            isSpawning = true;

            if (!Props.spawnList.NullOrEmpty())
                foreach (var info in Props.spawnList)
                    if (info.pawnKind != null)
                        SpawnPawns(info);

                    else if (info.thing != null)
                        SpawnThings(info);

                    else
                        Log.Error(
                            "JecsTools :: CompDelayedSpawner :: pawnToSpawn and thingToSpawn are both set to null.");
            else
                Log.Error("JecsTools :: CompDelayedSpawner :: spawnList is null or empty");

            ResolveDestroySettings();
        }


        private void SpawnThings(SpawnInfo info)
        {
            var thing = ThingMaker.MakeThing(info.thing, null);
            thing.stackCount = Math.Min(info.num, info.thing.stackLimit);
            GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near);
        }

        private void SpawnPawns(SpawnInfo info)
        {
            var spawnPosition = Position;
            if ((from cell in GenAdj.CellsAdjacent8Way(new TargetInfo(Position, Map))
                where Position.Walkable(Map)
                select cell).TryRandomElement(out spawnPosition))
            {
                var pawn = PawnGenerator.GeneratePawn(info.pawnKind,
                    Find.FactionManager.FirstFactionOfDef(info.faction) ?? null);
                if (GenPlace.TryPlaceThing(pawn, spawnPosition, Map, ThingPlaceMode.Near, null))
                {
                    GiveMentalState(info, pawn);
                    GiveHediffs(info, pawn);
                    PostSpawnEvents(pawn);
                }
            }
        }

        private void GiveMentalState(SpawnInfo info, Pawn pawn)
        {
            if (info.withMentalState != null)
                pawn.mindState.mentalStateHandler.TryStartMentalState(info.withMentalState);
        }

        private void GiveHediffs(SpawnInfo info, Pawn pawn)
        {
            if (!info.withHediffs.NullOrEmpty())
                foreach (var hediff in info.withHediffs)
                    if (HediffMaker.MakeHediff(hediff, pawn, null) is Hediff tempHediff)
                        pawn.health.AddHediff(tempHediff, null, null);

                    else
                        Log.Error("JecsTools :: CompDelayedSpawner :: Tried to apply non-hediff");
        }

        public virtual void PostSpawnEvents(Pawn pawnSpawned)
        {
        }


        public void ResolveDestroySettings()
        {
            if (Props.destroyAfterSpawn)
            {
                parent.Destroy(DestroyMode.Vanish);
                return;
            }

            if (!Props.spawnsOnce)
            {
                ticksLeft = -999;
                isSpawning = false;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isSpawning, "isSpawning", false);
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", -999);
        }
    }
}