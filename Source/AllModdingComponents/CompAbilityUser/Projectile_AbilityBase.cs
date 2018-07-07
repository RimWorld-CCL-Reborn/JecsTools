using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace AbilityUser
{
    public class Projectile_AbilityBase : Projectile
    {
        public List<ExtraDamage> extraDamages;
        public AbilityDef localAbilityDef;
        public List<ApplyHediffs> localApplyHediffs;
        public List<ApplyMentalStates> localApplyMentalStates;
        public List<SpawnThings> localSpawnThings;
        public Thing selectedTarget;

        public Vector3 targetVec;
        public Pawn Caster => launcher as Pawn;

        public ProjectileDef_Ability Mpdef
        {
            get
            {
                ProjectileDef_Ability mpdef = null;
                if (def is ProjectileDef_Ability)
                    mpdef = def as ProjectileDef_Ability;
                return mpdef;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref extraDamages, "projAbilityExtraDamages", LookMode.Deep);
            Scribe_Collections.Look(ref localApplyMentalStates, "projAbilityLocalApplyMentalStates", LookMode.Deep);
            Scribe_Collections.Look(ref localApplyHediffs, "projAbilityLocalApplyHediffs", LookMode.Deep);
            Scribe_Defs.Look(ref localAbilityDef, "projAbilityLocalAbilityDef");
        }

        // Verse.Projectile
        public override void Tick()
        {
            //Log.Message("Tick");
            if (landed)
                return;
            ticksToImpact--;
            if (ticksToImpact <= 0)
            {
                if (DestinationCell.InBounds(Map))
                    Position = DestinationCell;
                ImpactSomething();
                return;
            }
            ticksToImpact++;
            base.Tick();
        }


        /// <summary>
        ///     Applies damage on a collateral pawn or an object.
        /// </summary>
        protected void ApplyDamage(Thing hitThing)
        {
            //Log.Message("ApplyDamage");
            if (hitThing != null)
                Impact(hitThing);
            else
                ImpactSomething();
        }

        /// <summary>
        ///     Computes what should be impacted in the DestinationCell.
        /// </summary>
        protected void ImpactSomething()
        {
            // Check impact on a thick mountain.
            if (def.projectile.flyOverhead)
            {
                var roofDef = Map.roofGrid.RoofAt(DestinationCell);
                if (roofDef != null && roofDef.isThickRoof)
                    if (def.projectile != null)
                        if (def.projectile.soundHitThickRoof != null)
                        {
                            var info = SoundInfo.InMap(new TargetInfo(DestinationCell, Map, false),
                                MaintenanceType.None);
                            def.projectile.soundHitThickRoof.PlayOneShot(info);
                            return;
                        }
            }

            // Impact the initial targeted pawn.
            if (intendedTarget != null)
            {
                if (intendedTarget.Thing is Pawn pawn && pawn.Downed && (origin - destination).magnitude > 5f &&
                    Rand.Value < 0.2f)
                {
                    Impact(null);
                    return;
                }
                Impact(intendedTarget.Thing);
            }
            else
            {
                // Impact a pawn in the destination cell if present.
                var thing = Map.thingGrid.ThingAt(DestinationCell, ThingCategory.Pawn);
                if (thing != null)
                {
                    Impact(thing);
                    return;
                }
                // Impact any cover object.
                foreach (var current in Map.thingGrid.ThingsAt(DestinationCell))
                    if (current.def.fillPercent > 0f || current.def.passability != Traversability.Standable)
                    {
                        Impact(current);
                        return;
                    }
                Impact(null);
            }
        }


        public virtual bool CanOverpower(Pawn caster, Thing hitThing)
        {
            return true;
        }

        public void ApplyHediffsAndMentalStates(Pawn victim)
        {
            try
            {
                //Log.Message("ApplyHediffsAndMentalStates");
                if (localApplyMentalStates != null)
                    if (localApplyMentalStates.Count > 0)
                        foreach (var mentalStateGiver in localApplyMentalStates)
                        {
                            var success = false;
                            var checkValue = Rand.Value;
                            var str = localAbilityDef.LabelCap + " (" + Caster.LabelShort + ")";
                            if (checkValue <= mentalStateGiver.applyChance)
                                if (mentalStateGiver.mentalStateDef == MentalStateDefOf.Berserk &&
                                    victim.RaceProps.intelligence < Intelligence.Humanlike)
                                {
                                    if (Caster == victim || CanOverpower(Caster, victim))
                                    {
                                        success = true;
                                        victim.mindState.mentalStateHandler.TryStartMentalState(
                                            MentalStateDefOf.Manhunter, str, true);
                                    }
                                }
                                else
                                {
                                    if (Caster == victim || CanOverpower(Caster, victim))
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
                if (localApplyHediffs != null)
                    if (localApplyHediffs.Count > 0)
                        foreach (var hediffs in localApplyHediffs)
                        {
                            var success = false;
                            if (Rand.Value <= hediffs.applyChance)
                                if (victim == Caster || CanOverpower(Caster, victim))
                                {
                                    HealthUtility.AdjustSeverity(victim, hediffs.hediffDef, hediffs.severity);
                                    //Hediff newHediff = HediffMaker.MakeHediff(hediffs.hediffDef, victim, null);
                                    //victim.health.AddHediff(newHediff, null, null);
                                    //newHediff.Severity = hediffs.severity;
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
            catch (NullReferenceException e)
            {
                Log.Message(e.ToString());
            }
        }

        public Faction ResolveFaction(SpawnThings spawnables)
        {
            var factionDefToAssign = FactionDefOf.PlayerColony;
            if (this?.Caster?.Faction is Faction f && f.IsPlayer == false) return f;
            if (spawnables.factionDef != null) factionDefToAssign = spawnables.factionDef;
            if (spawnables.kindDef != null)
                if (spawnables.kindDef.defaultFactionType != null)
                    factionDefToAssign = spawnables.kindDef.defaultFactionType;

            return FactionUtility.DefaultFactionFrom(factionDefToAssign);
        }

        public PawnSummoned SpawnPawn(SpawnThings spawnables, Faction faction)
        {
            var newPawn = (PawnSummoned) PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
            newPawn.Spawner = Caster;
            newPawn.Temporary = spawnables.temporary;
            if (newPawn.Faction != Faction.OfPlayerSilentFail && this?.Caster?.Faction is Faction f)
                newPawn.SetFaction(f);
            GenSpawn.Spawn(newPawn, PositionHeld, Find.CurrentMap);
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

        public void SingleSpawnLoop(SpawnThings spawnables)
        {
            //Log.Message("SingleSpawnLoops");
            if (spawnables.def != null)
            {
                //Log.Message("2");

                var factionToAssign = ResolveFaction(spawnables);
                if (spawnables.def.race != null)
                {
                    if (spawnables.kindDef == null)
                    {
                        Log.Error("Missing kinddef");
                        return;
                    }
                    Pawn p = SpawnPawn(spawnables, factionToAssign);
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
                    GenSpawn.Spawn(thing, PositionHeld, MapHeld, Rot4.Random);
                }
            }
        }

        public void SpawnSpawnables()
        {
            //Log.Message("SpawnSpawnables");
            if (localSpawnThings != null && localSpawnThings.Count > 0)
                foreach (var spawnables in localSpawnThings)
                    //Log.Message("2S");
                    if (spawnables.spawnCount == 1) SingleSpawnLoop(spawnables);
                    else
                        for (var i = 0; i < spawnables.spawnCount; i++)
                            //Log.Message("3S");
                            SingleSpawnLoop(spawnables);
        }

        public virtual void Impact_Override(Thing hitThing)
        {
            //Log.Message("ImpactOverride");
            if (hitThing != null)
                if (hitThing is Pawn victim)
                    if (Mpdef != null)
                    {
                        SpawnSpawnables();
                        ApplyHediffsAndMentalStates(victim);
                        return;
                    }
            SpawnSpawnables();
            ApplyHediffsAndMentalStates(Caster);
        }

        public void Launch(Thing launcher, AbilityDef abilityDef, Vector3 origin, LocalTargetInfo targ,
            ProjectileHitFlags hitFlags, Thing equipment = null, List<ApplyHediffs> applyHediffs = null,
            List<ApplyMentalStates> applyMentalStates = null, List<SpawnThings> spawnThings = null)
        {
            //Log.Message("Projectile_AbilityBase");
            localApplyHediffs = applyHediffs;
            localApplyMentalStates = applyMentalStates;
            localSpawnThings = spawnThings;
            localAbilityDef = abilityDef;
            base.Launch(launcher, targ, targ, hitFlags, equipment); //TODO
        }

        protected override void Impact(Thing hitThing)
        {
            //Log.Message("Impact");
            Impact_Override(hitThing);
            if (hitThing != null)
                if (extraDamages != null && extraDamages.Count > 0)
                    foreach (var damage in extraDamages)
                    {
                        var extraDinfo = new DamageInfo(damage.damageDef, damage.damage, this.def.projectile.GetArmorPenetration(1f), ExactRotation.eulerAngles.y,
                            launcher, null, equipmentDef);
                        hitThing.TakeDamage(extraDinfo);
                    }
            base.Impact(hitThing);
        }
    }
}