using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;
using Verse.AI.Group;

namespace AbilityUser
{
    public class Projectile_AbilityBase : Projectile
    {
        public List<ExtraDamage> extraDamages = null;
        public List<ApplyHediffs> localApplyHediffs = null;
        public List<ApplyMentalStates> localApplyMentalStates = null;
        public List<SpawnThings> localSpawnThings = null;
        public AbilityDef localAbilityDef;

        public Vector3 targetVec;
        public Pawn Caster => base.launcher as Pawn;
        public Thing selectedTarget;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<ExtraDamage>(ref this.extraDamages, "extraDamages", LookMode.Deep);
            Scribe_Collections.Look<ApplyMentalStates>(ref this.localApplyMentalStates, "localApplyMentalStates", LookMode.Deep);
            Scribe_Collections.Look<ApplyHediffs>(ref this.localApplyHediffs, "localApplyHediffs", LookMode.Deep);
            Scribe_Defs.Look<AbilityDef>(ref this.localAbilityDef, "localAbilityDef");
        }

        // Verse.Projectile
        public override void Tick()
        {
            //Log.Message("Tick");
            if (this.landed)
            {
                return;
            }
            this.ticksToImpact--;
            if (this.ticksToImpact <= 0)
            {
                if (this.DestinationCell.InBounds(this.Map))
                {
                    this.Position = this.DestinationCell;
                }
                this.ImpactSomething();
                return;
            }
            this.ticksToImpact++;
            base.Tick();
        }

        
        /// <summary>
        /// Applies damage on a collateral pawn or an object.
        /// </summary>
        protected void ApplyDamage(Thing hitThing)
        {
            //Log.Message("ApplyDamage");
            if (hitThing != null)
            {
                // Impact collateral target.
                this.Impact(hitThing);
            }
            else
            {
                this.ImpactSomething();
            }
        }
        
        /// <summary>
        /// Computes what should be impacted in the DestinationCell.
        /// </summary>
        protected void ImpactSomething()
        {
            // Check impact on a thick mountain.
            if (this.def.projectile.flyOverhead)
            {
                RoofDef roofDef = this.Map.roofGrid.RoofAt(this.DestinationCell);
                if (roofDef != null && roofDef.isThickRoof)
                {
                    if (this.def.projectile != null)
                    {
                        if (this.def.projectile.soundHitThickRoof != null)
                        {
                            SoundInfo info = SoundInfo.InMap(new TargetInfo(this.DestinationCell, this.Map, false), MaintenanceType.None);
                            this.def.projectile.soundHitThickRoof.PlayOneShot(info);
                            return;
                        }
                    }
                }
            }

            // Impact the initial targeted pawn.
            if (this.assignedTarget != null)
            {
                if (this.assignedTarget is Pawn pawn && pawn.Downed && (this.origin - this.destination).magnitude > 5f && Rand.Value < 0.2f)
                {
                    this.Impact(null);
                    return;
                }
                this.Impact(this.assignedTarget);
                return;
            }
            else
            {
                // Impact a pawn in the destination cell if present.
                Thing thing = this.Map.thingGrid.ThingAt(this.DestinationCell, ThingCategory.Pawn);
                if (thing != null)
                {
                    this.Impact(thing);
                    return;
                }
                // Impact any cover object.
                foreach (Thing current in this.Map.thingGrid.ThingsAt(this.DestinationCell))
                {
                    if (current.def.fillPercent > 0f || current.def.passability != Traversability.Standable)
                    {
                        this.Impact(current);
                        return;
                    }
                }
                this.Impact(null);
                return;
            }
        }

        public ProjectileDef_Ability Mpdef
        {
            get
            {
                ProjectileDef_Ability mpdef = null;
                if (this.def is ProjectileDef_Ability)
                {
                    mpdef = this.def as ProjectileDef_Ability;
                }
                return mpdef;
            }
        }


        public virtual bool CanOverpower(Pawn caster, Thing hitThing) => true;

        public void ApplyHediffsAndMentalStates(Pawn victim)
        {
            try
            {
                //Log.Message("ApplyHediffsAndMentalStates");
                if (this.localApplyMentalStates != null)
                {
                    if (this.localApplyMentalStates.Count > 0)
                    {
                        foreach (ApplyMentalStates mentalStateGiver in this.localApplyMentalStates)
                        {
                            bool success = false;
                            float checkValue = Rand.Value;
                            string str = localAbilityDef.LabelCap + " (" + this.Caster.LabelShort + ")";
                            if (checkValue <= mentalStateGiver.applyChance)
                            {
                                if (mentalStateGiver.mentalStateDef == MentalStateDefOf.Berserk && victim.RaceProps.intelligence < Intelligence.Humanlike)
                                {
                                    if (this.Caster == victim || CanOverpower(this.Caster, victim))
                                    {
                                        success = true;
                                        victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, str, true);

                                    }
                                }
                                else
                                {
                                    if (this.Caster == victim || CanOverpower(this.Caster, victim))
                                    {
                                        success = true;
                                        victim.mindState.mentalStateHandler.TryStartMentalState(mentalStateGiver.mentalStateDef, str, true);
                                    }
                                }
                            }
                            if (success)
                            {
                                victim.Drawer.Notify_DebugAffected();
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, mentalStateGiver.mentalStateDef.LabelCap + ": " + StringsToTranslate.AU_CastSuccess, -1f);
                            }
                            else
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, mentalStateGiver.mentalStateDef.LabelCap + ": " + StringsToTranslate.AU_CastFailure, -1f);
                        }
                    }
                }
                if (this.localApplyHediffs != null)
                {
                    if (this.localApplyHediffs.Count > 0)
                    {
                        foreach (ApplyHediffs hediffs in this.localApplyHediffs)
                        {
                            bool success = false;
                            if (Rand.Value <= hediffs.applyChance)
                            {
                                if (victim == this.Caster || CanOverpower(this.Caster, victim))
                                {
                                    HealthUtility.AdjustSeverity(victim, hediffs.hediffDef, hediffs.severity);
                                    //Hediff newHediff = HediffMaker.MakeHediff(hediffs.hediffDef, victim, null);
                                    //victim.health.AddHediff(newHediff, null, null);
                                    //newHediff.Severity = hediffs.severity;
                                    success = true;
                                }
                            }
                            if (success)
                            {
                                victim.Drawer.Notify_DebugAffected();
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, hediffs.hediffDef.LabelCap + ": " + StringsToTranslate.AU_CastSuccess, -1f);
                            }
                            else
                                MoteMaker.ThrowText(victim.DrawPos, victim.Map, StringsToTranslate.AU_CastFailure, -1f);
                        }
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
            FactionDef factionDefToAssign = FactionDefOf.PlayerColony;
            if (this?.Caster?.Faction is Faction f && f.IsPlayer == false) return f;
            if (spawnables.factionDef != null) factionDefToAssign = spawnables.factionDef;
            if (spawnables.kindDef != null)
            {
                if (spawnables.kindDef.defaultFactionType != null) factionDefToAssign = spawnables.kindDef.defaultFactionType;
            }

            return FactionUtility.DefaultFactionFrom(factionDefToAssign);
        }

        public PawnSummoned SpawnPawn(SpawnThings spawnables, Faction faction)
        {
            PawnSummoned newPawn = (PawnSummoned)PawnGenerator.GeneratePawn(spawnables.kindDef, faction);
            newPawn.Spawner = this.Caster;
            newPawn.Temporary = spawnables.temporary;
            if (newPawn.Faction != Faction.OfPlayerSilentFail && this?.Caster?.Faction is Faction f) newPawn.SetFaction(f);
            GenSpawn.Spawn(newPawn, this.PositionHeld, Find.VisibleMap);
            if (faction != null && faction != Faction.OfPlayer)
            {
                Lord lord = null;
                if (newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction).Any((Pawn p) => p != newPawn))
                {
                    Predicate<Thing> validator = (Thing p) => p != newPawn && ((Pawn)p).GetLord() != null;
                    Pawn p2 = (Pawn)GenClosest.ClosestThing_Global(newPawn.Position, newPawn.Map.mapPawns.SpawnedPawnsInFaction(faction), 99999f, validator);
                    lord = p2.GetLord();
                }
                if (lord == null)
                {
                    LordJob_DefendPoint lordJob = new LordJob_DefendPoint(newPawn.Position);
                    lord = LordMaker.MakeNewLord(faction, lordJob, Find.VisibleMap, null);
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

                Faction factionToAssign = ResolveFaction(spawnables);
                if (spawnables.def.race != null)
                {
                    if (spawnables.kindDef == null) { Log.Error("Missing kinddef"); return; }
                    Pawn p = SpawnPawn(spawnables, factionToAssign);
                    //if (this?.Caster?.Faction is Faction f && Faction.OfPlayerSilentFail != f) p.SetFactionDirect(f);
                }
                else
                {
                    //Log.Message("3b");
                    ThingDef thingDef = spawnables.def;
                    ThingDef stuff = null;
                    if (thingDef.MadeFromStuff)
                    {
                        stuff = ThingDefOf.WoodLog;
                    }
                    Thing thing = ThingMaker.MakeThing(thingDef, stuff);
                    thing.SetFaction(factionToAssign, null);
                    GenSpawn.Spawn(thing, this.PositionHeld, this.MapHeld, Rot4.Random);
                }
            }

        }

        public void SpawnSpawnables()
        {
            //Log.Message("SpawnSpawnables");
            if (this.localSpawnThings != null && this.localSpawnThings.Count > 0)
            {

                //Log.Message("1S");
                foreach (SpawnThings spawnables in this.localSpawnThings)
                {

                    //Log.Message("2S");
                    if (spawnables.spawnCount == 1) SingleSpawnLoop(spawnables);
                    else
                    {
                        for (int i = 0; i < spawnables.spawnCount; i++)
                        {

                            //Log.Message("3S");
                            SingleSpawnLoop(spawnables);
                        }
                    }
                }
            }
        }

        public virtual void Impact_Override(Thing hitThing)
        {
            //Log.Message("ImpactOverride");
            if (hitThing != null)
            {
                if (hitThing is Pawn victim)
                {
                    if (this.Mpdef != null)
                    {
                        SpawnSpawnables();
                        ApplyHediffsAndMentalStates(victim);
                        return;
                    }
                }

            }
            SpawnSpawnables();
            ApplyHediffsAndMentalStates(this.Caster);
        }
        
        public void Launch(Thing launcher, AbilityDef abilityDef, Vector3 origin, LocalTargetInfo targ, Thing equipment = null, List<ApplyHediffs> applyHediffs = null, List<ApplyMentalStates> applyMentalStates = null, List<SpawnThings> spawnThings = null)
        {
            //Log.Message("Projectile_AbilityBase");
            this.localApplyHediffs = applyHediffs;
            this.localApplyMentalStates = applyMentalStates;
            this.localSpawnThings = spawnThings;
            this.localAbilityDef = abilityDef;
            base.Launch(launcher, origin, targ, equipment);
        }

        protected override void Impact(Thing hitThing)
        {
            //Log.Message("Impact");
            Impact_Override(hitThing);
            if (hitThing != null)
            {
                if (this.extraDamages != null && this.extraDamages.Count > 0)
                {
                    foreach (ExtraDamage damage in this.extraDamages)
                    {
                        DamageInfo extraDinfo = new DamageInfo(damage.damageDef, damage.damage, this.ExactRotation.eulerAngles.y, this.launcher, null, this.equipmentDef);
                        hitThing.TakeDamage(extraDinfo);
                    }
                }
            }
            base.Impact(hitThing);
        }

    }
}
