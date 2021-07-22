using System.Collections.Generic;
using UnityEngine;
using Verse;
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

        public ProjectileDef_Ability Mpdef => def as ProjectileDef_Ability;

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
            //Log.Message($"Projectile_AbilityBase.Tick({this})");
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
            //Log.Message($"Projectile_AbilityBase.ApplyDamage({this}, {hitThing})");
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
                if (roofDef != null && roofDef.isThickRoof &&
                    // TODO: Are these null checks necessary? Projectile.ImpactSomething doesn't have it.
                    def.projectile != null && def.projectile.soundHitThickRoof != null)
                {
                    def.projectile.soundHitThickRoof.PlayOneShot(SoundInfo.InMap(new TargetInfo(DestinationCell, Map)));
                    return;
                }
            }

            // Impact the initial targeted pawn.
            if (intendedTarget != null)
            {
                if (intendedTarget.Thing is Pawn pawn && pawn.Downed && (origin - destination).magnitude > 5f && Rand.Value < 0.2f)
                    Impact(null);
                else
                    Impact(intendedTarget.Thing);
            }
            else
            {
                // Impact a pawn in the destination cell if present.
                var thing = Map.thingGrid.ThingAt(DestinationCell, ThingCategory.Pawn);
                if (thing != null)
                {
                    Impact(thing);
                }
                else
                {
                    // Impact any cover object.
                    foreach (var current in Map.thingGrid.ThingsAt(DestinationCell))
                    {
                        if (current.def.fillPercent > 0f || current.def.passability != Traversability.Standable)
                        {
                            Impact(current);
                            return;
                        }
                    }
                    Impact(null);
                }
            }
        }


        public virtual bool CanOverpower(Pawn caster, Thing hitThing)
        {
            return true;
        }

        public void ApplyHediffsAndMentalStates(Pawn victim, Pawn caster, List<ApplyMentalStates> localApplyMentalStates, AbilityDef localAbilityDef)
        {
            //Log.Message($"Projectile_AbilityBase.ApplyHediffsAndMentalStates({this}, ...)");
            AbilityEffectUtility.ApplyMentalStates(victim, caster, localApplyMentalStates, localAbilityDef, this);
            AbilityEffectUtility.ApplyHediffs(victim, caster, localApplyHediffs, null);
        }

        public virtual void Impact_Override(Thing hitThing)
        {
            //Log.Message($"Projectile_AbilityBase.Impact_Override({this}, {hitThing})");
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

        public void Launch(Thing launcher, AbilityDef abilityDef, Vector3 origin, LocalTargetInfo targ, ProjectileHitFlags hitFlags,
            bool preventFriendlyFire = false, Thing equipment = null, List<ApplyHediffs> applyHediffs = null,
            List<ApplyMentalStates> applyMentalStates = null, List<SpawnThings> spawnThings = null)
        {
            //Log.Message($"Projectile_AbilityBase.Launch({this}, ...)");
            localApplyHediffs = applyHediffs;
            localApplyMentalStates = applyMentalStates;
            localSpawnThings = spawnThings;
            localAbilityDef = abilityDef;
            Launch(launcher, targ, targ, hitFlags, preventFriendlyFire, equipment); //TODO
        }

        protected override void Impact(Thing hitThing)
        {
            //Log.Message($"Projectile_AbilityBase.Impact({this}, {hitThing})");
            Impact_Override(hitThing);
            if (hitThing != null)
                if (extraDamages != null)
                    foreach (var damage in extraDamages)
                    {
                        var extraDinfo = new DamageInfo(damage.damageDef, damage.damage,
                            def.projectile.GetArmorPenetration(1f), ExactRotation.eulerAngles.y,
                            launcher, weapon: equipmentDef);
                        //Log.Message($"Projectile_AbilityBase.Impact({this}, {hitThing}) extraDinfo={extraDinfo}");
                        hitThing.TakeDamage(extraDinfo);
                    }
            base.Impact(hitThing);
        }
    }
}
