using System;
using System.Collections.Generic;
using AbilityUser;
using RimWorld;
using Verse;
using Verse.AI.Group;

static internal class AbilityEffectUtility
{
    public static Faction ResolveFaction(SpawnThings spawnables, Pawn caster)
    {
        var factionDefToAssign = FactionDefOf.PlayerColony;
        if (caster?.Faction is Faction f && f.IsPlayer == false) return f;
        if (spawnables.factionDef != null) factionDefToAssign = spawnables.factionDef;
        if (spawnables.kindDef != null)
            if (spawnables.kindDef.defaultFactionType != null)
                factionDefToAssign = spawnables.kindDef.defaultFactionType;

        return FactionUtility.DefaultFactionFrom(factionDefToAssign);
    }

    public static PawnSummoned SpawnPawn(SpawnThings spawnables, Faction faction, Pawn caster, IntVec3 positionHeld)
    {
        var newPawn = (PawnSummoned) PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
        newPawn.Spawner = caster;
        newPawn.Temporary = spawnables.temporary;
        if (newPawn.Faction != Faction.OfPlayerSilentFail && caster?.Faction is Faction f)
            newPawn.SetFaction(f);
        GenSpawn.Spawn(newPawn, positionHeld, Find.CurrentMap);
        if (faction != null && faction != Faction.OfPlayer)
        {
            Lord lord = null;
            if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any(p => p != newPawn))
            {
                Predicate<Thing> validator = p => p != newPawn && ((Pawn) p).GetLord() != null;
                var p2 = (Pawn) GenClosest.ClosestThing_Global(newPawn.Position,
                    newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator);
                lord = p2.GetLord();
            }
            if (lord == null)
            {
                var lordJob = new LordJob_DefendPoint(newPawn.Position);
                lord = LordMaker.MakeNewLord(faction, lordJob, Find.CurrentMap, null);
            }
            lord.AddPawn(newPawn);
        }
        return newPawn;
    }

    public static void SingleSpawnLoop(SpawnThings spawnables, IntVec3 positionHeld, Map mapHeld, Pawn caster)
    {
        //Log.Message("SingleSpawnLoops");
        if (spawnables.def != null)
        {
            //Log.Message("2");

            var factionToAssign = ResolveFaction(spawnables, caster);
            if (spawnables.def.race != null)
            {
                if (spawnables.kindDef == null)
                {
                    Log.Error("Missing kinddef");
                    return;
                }
                Pawn p = SpawnPawn(spawnables, factionToAssign, caster, positionHeld);
                //if (this?.Caster?.Faction is Faction f && Faction.OfPlayerSilentFail != f) p.SetFactionDirect(f);
            }
            else
            {
                //Log.Message("3b");
                var thingDef = spawnables.def;
                ThingDef stuff = null;
                if (thingDef.MadeFromStuff)
                    stuff = ThingDefOf.WoodLog;
                var thing = ThingMaker.MakeThing(thingDef, stuff);
                thing.SetFaction(factionToAssign, null);
                GenSpawn.Spawn(thing, positionHeld, mapHeld, Rot4.Random);
            }
        }
    }

    public static void SpawnSpawnables(List<SpawnThings> localSpawnThings, Pawn caster, Map mapHeld, IntVec3 positionHeld)
    {
        //Log.Message("SpawnSpawnables");
        if (localSpawnThings != null && localSpawnThings.Count > 0)
            foreach (var spawnables in localSpawnThings)
                //Log.Message("2S");
                if (spawnables.spawnCount == 1)
                    SingleSpawnLoop(spawnables,positionHeld, mapHeld, caster);
                else
                    for (var i = 0; i < spawnables.spawnCount; i++)
                        //Log.Message("3S");
                        SingleSpawnLoop(spawnables, positionHeld, mapHeld, caster);
    }

    public static void ApplyHediffs(Pawn victim, Pawn caster, List<ApplyHediffs> localApplyHediffs, Projectile_AbilityBase abilityProjectile)
    {
        if (localApplyHediffs != null)
            if (localApplyHediffs.Count > 0)
                foreach (var hediffs in localApplyHediffs)
                {
                    var success = false;
                    if (Rand.Value <= hediffs.applyChance)
                        if (victim == caster || abilityProjectile?.CanOverpower(caster, victim) != false)
                        {
                            HealthUtility.AdjustSeverity(victim, hediffs.hediffDef, hediffs.severity);
                            success = true;
                        }

                    if (success)
                    {
                        victim.Drawer.Notify_DebugAffected();
                        MoteMaker.ThrowText(victim.DrawPos, victim.Map,
                            hediffs.hediffDef.LabelCap + ": " + StringsToTranslate.AU_CastSuccess, -1f);
                    }
                    else
                    {
                        MoteMaker.ThrowText(victim.DrawPos, victim.Map, StringsToTranslate.AU_CastFailure, -1f);
                    }
                }
    }
    
    public static void ApplyMentalStates(Pawn victim, Pawn caster, List<ApplyMentalStates> localApplyMentalStates, AbilityUser.AbilityDef localAbilityDef, Projectile_AbilityBase abilityProjectile)
    {
        if (localApplyMentalStates != null)
            if (localApplyMentalStates.Count > 0)
                foreach (var mentalStateGiver in localApplyMentalStates)
                {
                    var success = false;
                    var checkValue = Rand.Value;
                    var str = localAbilityDef.LabelCap + " (" + caster.LabelShort + ")";
                    if (checkValue <= mentalStateGiver.applyChance)
                        if (mentalStateGiver.mentalStateDef == MentalStateDefOf.Berserk &&
                            victim.RaceProps.intelligence < Intelligence.Humanlike)
                        {
                            if (caster == victim || abilityProjectile?.CanOverpower(caster, victim) != false)
                            {
                                success = true;
                                victim.mindState.mentalStateHandler.TryStartMentalState(
                                    MentalStateDefOf.Manhunter, str, true);
                            }
                        }
                        else
                        {
                            if (caster == victim || abilityProjectile?.CanOverpower(caster, victim) != false)
                            {
                                success = true;
                                victim.mindState.mentalStateHandler.TryStartMentalState(
                                    mentalStateGiver.mentalStateDef, str, true);
                            }
                        }

                    if (success)
                    {
                        victim.Drawer.Notify_DebugAffected();
                        MoteMaker.ThrowText(victim.DrawPos, victim.Map,
                            mentalStateGiver.mentalStateDef.LabelCap + ": " + StringsToTranslate.AU_CastSuccess,
                            -1f);
                    }
                    else
                    {
                        MoteMaker.ThrowText(victim.DrawPos, victim.Map,
                            mentalStateGiver.mentalStateDef.LabelCap + ": " + StringsToTranslate.AU_CastFailure,
                            -1f);
                    }
                }
    }
}