using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace AbilityUser
{
    /// <summary>
    ///     A special version of a projectile.
    ///     This one "stores" a base object and "delivers" it.
    /// </summary>
    public class FlyingObject : ThingWithComps
    {
        protected Thing usedTarget;

        public bool damageLaunched = true;
        protected Vector3 destination;
        public bool explosion;
        protected Thing flyingThing;
        public DamageInfo? impactDamage;
        protected Thing launcher;
        protected Vector3 origin;
        protected float speed = 30.0f;
        protected int ticksToImpact;
        public int timesToDamage = 3;

        protected int StartingTicksToImpact
        {
            get
            {
                var num = Mathf.RoundToInt((origin - destination).magnitude / (speed / 100f));
                if (num < 1)
                    num = 1;
                return num;
            }
        }

        protected IntVec3 DestinationCell => new IntVec3(destination);

        public virtual Vector3 ExactPosition
        {
            get
            {
                var b = (destination - origin) * (1f - ticksToImpact / (float)StartingTicksToImpact);
                return origin + b + Vector3.up * def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation => Quaternion.LookRotation(destination - origin);

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref origin, nameof(origin));
            Scribe_Values.Look(ref destination, nameof(destination));
            Scribe_Values.Look(ref ticksToImpact, nameof(ticksToImpact));
            Scribe_Values.Look(ref timesToDamage, nameof(timesToDamage));
            Scribe_Values.Look(ref damageLaunched, nameof(damageLaunched), true);
            Scribe_Values.Look(ref explosion, nameof(explosion));
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

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing,
            DamageInfo? newDamageInfo = null)
        {
            //Despawn the object to fly
            if (flyingThing.Spawned)
                flyingThing.DeSpawn();

            this.launcher = launcher;
            this.origin = origin;
            impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            if (targ.Thing != null)
                usedTarget = targ.Thing;
            destination = targ.Cell.ToVector3Shifted() +
                          new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            ticksToImpact = StartingTicksToImpact;
            //Log.Message($"FlyingObject.Launch({this})");
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
            else
            {
                if (ticksToImpact <= 0)
                {
                    var destinationCell = DestinationCell;
                    Position = destinationCell.InBounds(Map) ? destinationCell : exactPosition.ToIntVec3();
                    ImpactSomething();
                }
                else
                {
                    Position = exactPosition.ToIntVec3();
                }
            }
        }

        public override void Draw()
        {
            if (flyingThing != null)
            {
                if (flyingThing is Pawn pawn)
                {
                    // Temp note: DrawPos can't be null and ToIntVec3().IsValid is always true. TODO: remove this comment
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
            // TODO: Should this hitThing fallback really required to be a Pawn?
            hitThing ??= Position.GetThingList(Map).FirstOrDefault(x => x == usedTarget) as Pawn;

            if (impactDamage != null)
            {
                for (var i = 0; i < timesToDamage; i++)
                    if (damageLaunched)
                        flyingThing.TakeDamage(impactDamage.Value);
                    else
                        hitThing.TakeDamage(impactDamage.Value);
                if (explosion)
                    GenExplosion.DoExplosion(Position, Map, 0.9f, DamageDefOf.Stun, this);
            }
            GenSpawn.Spawn(flyingThing, Position, Map);
            Destroy();
        }

        public override string ToString()
        {
            return $"{base.ToString()}(launcher={launcher}, flyingThing={flyingThing}, usedTarget={usedTarget}, " +
                $"origin={origin}, destination={destination}, pos={ExactPosition}, speed={speed}, ticksToImpact={ticksToImpact}, " +
                $"impactDamage={impactDamage}, damageLaunched={damageLaunched}, timesToDamage={timesToDamage}, explosion={explosion})";
        }
    }
}
