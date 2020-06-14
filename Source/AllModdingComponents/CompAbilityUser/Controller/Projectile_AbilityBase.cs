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

        public void ApplyHediffsAndMentalStates(Pawn victim, Pawn caster, List<ApplyMentalStates> localApplyMentalStates, AbilityDef localAbilityDef)
        {
            try
            {
                //Log.Message("ApplyHediffsAndMentalStates");
                AbilityEffectUtility.ApplyMentalStates(victim, caster, localApplyMentalStates, localAbilityDef, this);
                AbilityEffectUtility.ApplyHediffs(victim, caster, localApplyHediffs, null);
            }
            catch (NullReferenceException e)
            {
                Log.Message(e.ToString());
            }
        }

        public virtual void Impact_Override(Thing hitThing)
        {
            //Log.Message("ImpactOverride");
            if (hitThing != null)
                if (hitThing is Pawn victim)
                    if (Mpdef != null)
                    {
                        AbilityEffectUtility.SpawnSpawnables(localSpawnThings, Caster, MapHeld, PositionHeld);
                        ApplyHediffsAndMentalStates(victim, Caster, localApplyMentalStates, localAbilityDef);
                        return;
                    }

            AbilityEffectUtility.SpawnSpawnables(localSpawnThings, Caster, MapHeld, PositionHeld);
            ApplyHediffsAndMentalStates(Caster, Caster, localApplyMentalStates, localAbilityDef);
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