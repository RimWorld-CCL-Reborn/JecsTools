using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    /// <summary>
    ///     A special version of a projectile.
    ///     This one "stores" a base object and "delivers" it.
    /// </summary>
    // Based off Projectile (and parts of Projectile_Explosive and Bullet).
    public class FlyingObject : ThingWithComps
    {
        // TODO: Deprecate/move these settings into a CompProperties_FlyingObject (derived from ProjectileProperties)?
        public bool damageLaunched = true;
        [Obsolete("Use Props.speed")]
        protected float speed = 30f;
        public int timesToDamage = 3;
        public float accuracyRadius = 0.3f;
        [Obsolete("Use Props.extraDamages")]
        public DamageInfo? impactDamage; // this can also be set via Launch method
        [Obsolete("Use Props.explosionRadius > 0f")]
        public bool explosion;
        private ProjectileProperties props;

        // These are set by the Launch method.
        protected Vector3 origin;
        protected Vector3 destination;
        protected int ticksToImpact;
        protected Thing usedTarget; // TODO: should be a LocalTargetInfo?
        protected Thing flyingThing;
        protected ThingDef equipmentDef;
        protected Thing launcher;

        // TODO: should be CompProperties_FlyingObject (derived from ProjectileProperties)?
        public ProjectileProperties Props
        {
            get
            {
                if (props == null)
                {
                    props = new ProjectileProperties()
                    // Legacy defaults
#pragma warning disable CS0618 // Type or member is obsolete
                    {
                        speed = speed,
                    };
                    if (impactDamage is DamageInfo dinfo)
                    {
                        props.extraDamages = DamageInfoToExtraDamages(dinfo);
                        if (explosion)
#pragma warning restore CS0618 // Type or member is obsolete
                        {
                            props.damageDef = DamageDefOf.Stun;
                            props.explosionRadius = 0.9f;
                        }
                    };
                }
                return props;
            }
        }

        protected float StartingTicksToImpact
        {
            get
            {
                var ticks = (origin - destination).magnitude / Props.SpeedTilesPerTick;
                if (ticks <= 0f)
                    ticks = 0.001f;
                return ticks;
            }
        }

        protected IntVec3 DestinationCell => new IntVec3(destination);

        public virtual Vector3 ExactPosition
        {
            get
            {
                var b = (destination - origin).Yto0() * Mathf.Clamp01(1f - ticksToImpact / StartingTicksToImpact);
                return origin.Yto0() + b + Vector3.up * def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation => Quaternion.LookRotation((destination - origin).Yto0());

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref origin, nameof(origin));
            Scribe_Values.Look(ref destination, nameof(destination));
            Scribe_Values.Look(ref ticksToImpact, nameof(ticksToImpact));
            Scribe_Values.Look(ref timesToDamage, nameof(timesToDamage));
            Scribe_Values.Look(ref damageLaunched, nameof(damageLaunched), true);
#pragma warning disable CS0618 // Type or member is obsolete
            Scribe_Values.Look(ref explosion, nameof(explosion));
#pragma warning restore CS0618 // Type or member is obsolete
            Scribe_Deep.Look(ref props, nameof(props));
            Scribe_References.Look(ref usedTarget, nameof(usedTarget));
            Scribe_References.Look(ref launcher, nameof(launcher));
            Scribe_References.Look(ref flyingThing, nameof(flyingThing));
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing, impactDamage);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing)
        {
            Launch(launcher, Position.ToVector3Shifted(), targ, flyingThing);
        }

        // TODO: New Launch overload that corresponds to latest Projectile.Launch signature?
        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing,
            DamageInfo? newDamageInfo = null)
        {
            //Despawn the object to fly
            if (flyingThing.Spawned)
                flyingThing.DeSpawn();

            this.launcher = launcher;
            this.origin = origin;
            if (newDamageInfo is DamageInfo impactDamage)
            {
                Props.extraDamages = DamageInfoToExtraDamages(impactDamage);
                equipmentDef = impactDamage.Weapon;
            }
            this.flyingThing ??= flyingThing;
            usedTarget = targ.Thing;
            destination = targ.Cell.ToVector3Shifted();
            if (accuracyRadius > 0f)
            {
                destination.x += Rand.Range(-accuracyRadius, accuracyRadius);
                destination.z += Rand.Range(-accuracyRadius, accuracyRadius);
            }
            ticksToImpact = Math.Min(1, Mathf.CeilToInt(StartingTicksToImpact));
            //Log.Message($"FlyingObject.Launch({this})");
        }

        private static List<Verse.ExtraDamage> DamageInfoToExtraDamages(DamageInfo dinfo)
        {
            return new List<Verse.ExtraDamage>
            {
                new Verse.ExtraDamage
                {
                    def = dinfo.Def,
                    amount = dinfo.Amount,
                    armorPenetration = dinfo.ArmorPenetrationInt,
                }
            };
        }

        public override void Tick()
        {
            //if (ticksToImpact % 10 == 0) Log.Message($"FlyingObject.Tick({this})");
            base.Tick();
            ticksToImpact--;
            var exactPosition = ExactPosition;
            if (!exactPosition.InBounds(Map))
            {
                ticksToImpact++;
                exactPosition = ExactPosition;
                Position = exactPosition.ToIntVec3();
                Destroy();
            }
            else if (ticksToImpact <= 0)
            {
                var destinationCell = DestinationCell;
                Position = destinationCell.InBounds(Map) ? destinationCell : exactPosition.ToIntVec3();
                ImpactSomething();
            }
            else
            {
                // TODO: There should be an option to check for impact when entering a new cell.
                Position = exactPosition.ToIntVec3();
            }
        }

        public override void Draw()
        {
            if (flyingThing != null)
            {
                if (flyingThing is Pawn pawn)
                {
                    pawn.Drawer.DrawAt(DrawPos);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, DrawPos, ExactRotation, flyingThing.def.DrawMatSingle, 0);
                }
                Comps_PostDraw();
            }
        }

        private void ImpactSomething()
        {
            //Log.Message($"FlyingObject.ImpactSomething({this})");
            if (usedTarget != null)
            {
                if (usedTarget is Pawn pawn && pawn.GetPosture() != PawnPosture.Standing &&
                    (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f)
                    Impact(null);
                else
                    Impact(usedTarget);
            }
            else
            {
                Impact(null);
            }
        }

        protected virtual void Impact(Thing hitThing)
        {
            var map = Map;
            var pos = Position;
            var props = Props;

            if (damageLaunched)
                hitThing = flyingThing;
            else if (hitThing == null && usedTarget != null && pos.GetThingList(map).Contains(usedTarget))
                hitThing = usedTarget;

            if (!props.extraDamages.NullOrEmpty())
            {
                // Based off Bullet.
                for (var i = 0; i < timesToDamage; i++)
                {
                    foreach (var extraDamage in props.extraDamages)
                    {
                        var impactDamage = new DamageInfo(extraDamage.def, extraDamage.amount, extraDamage.AdjustedArmorPenetration(),
                            ExactRotation.eulerAngles.y, instigator: this, weapon: equipmentDef, intendedTarget: usedTarget);
                        hitThing.TakeDamage(impactDamage);
                    }
                }
            }

            if (props.explosionRadius > 0f)
            {
                // Based off Projectile_Explosive.
                var explosionEffect = props.explosionEffect;
                if (explosionEffect != null)
                {
                    Effecter effecter = explosionEffect.Spawn();
                    var target = new TargetInfo(pos, map);
                    effecter.Trigger(target, target);
                    effecter.Cleanup();
                }
                GenExplosion.DoExplosion(
                    center: pos, 
                    map,
                    props.explosionRadius,
                    props.damageDef,
                    instigator: this, 
                    props.GetDamageAmount(1f),
                    props.GetArmorPenetration(1f),
                    props.soundExplode, 
                    weapon: null, 
                    projectile: null, 
                    intendedTarget: null,
                    props.postExplosionSpawnThingDef,
                    props.postExplosionSpawnChance,
                    props.postExplosionSpawnThingCount,
                    props.postExplosionGasType,
                    props.applyDamageToExplosionCellsNeighbors,
                    props.preExplosionSpawnThingDef, 
                    props.preExplosionSpawnChance,
                    props.preExplosionSpawnThingCount, 
                    props.explosionChanceToStartFire,
                    props.explosionDamageFalloff, 
                    direction: null, 
                    ignoredThings: null);
            }

            GenSpawn.Spawn(flyingThing, pos, map);
            Destroy();
        }

        public override string ToString()
        {
            var props = Props;
            var propsStr = Gen.GetNonNullFieldsDebugInfo(props);
            if (props.extraDamages != null)
            {
                var impactDamageStrs = new List<string>();
                foreach (var extraDamage in props.extraDamages)
                    impactDamageStrs.Add($"({Gen.GetNonNullFieldsDebugInfo(extraDamage)})");
                propsStr = propsStr.Replace(props.extraDamages.ToStringSafe(), "{" + impactDamageStrs.ToStringSafeEnumerable() + "}");
            }
            return $"{base.ToString()}(flyingThing={flyingThing.ToStringSafe()}, usedTarget={usedTarget?.ToStringSafe()}, " +
                $"equipmentDef={equipmentDef.ToStringSafe()}, launcher={launcher.ToStringSafe()}, " +
                $"origin={origin}, destination={destination}, pos={ExactPosition}, ticksToImpact={ticksToImpact}, " +
                $"damageLaunched={damageLaunched}, timesToDamage={timesToDamage}, accuracyRadius={accuracyRadius}, " +
                $"props={propsStr})";
        }
    }
}
