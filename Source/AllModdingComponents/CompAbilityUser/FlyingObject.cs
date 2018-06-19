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
                var b = (destination - origin) * (1f - ticksToImpact / (float) StartingTicksToImpact);
                return origin + b + Vector3.up * def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation => Quaternion.LookRotation(destination - origin);

        public override Vector3 DrawPos => ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref origin, "origin", default(Vector3), false);
            Scribe_Values.Look(ref destination, "destination", default(Vector3), false);
            Scribe_Values.Look(ref ticksToImpact, "ticksToImpact", 0, false);
            Scribe_Values.Look(ref timesToDamage, "timesToDamage", 0, false);
            Scribe_Values.Look(ref damageLaunched, "damageLaunched", true);
            Scribe_Values.Look(ref explosion, "explosion", false);
            Scribe_References.Look(ref usedTarget, "usedTarget", false);
            Scribe_References.Look(ref launcher, "launcher", false);
            Scribe_References.Look(ref flyingThing, "flyingThing");
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
            if (flyingThing.Spawned) flyingThing.DeSpawn();

            this.launcher = launcher;
            this.origin = origin;
            impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            if (targ.Thing != null)
                usedTarget = targ.Thing;
            destination = targ.Cell.ToVector3Shifted() +
                          new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            ticksToImpact = StartingTicksToImpact;
        }

        public override void Tick()
        {
            base.Tick();
            var exactPosition = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }

            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact <= 0)
            {
                if (DestinationCell.InBounds(Map))
                    Position = DestinationCell;
                ImpactSomething();
            }
        }

        public override void Draw()
        {
            if (flyingThing != null)
            {
                if (flyingThing is Pawn)
                {
                    if (DrawPos == null) return;
                    if (!DrawPos.ToIntVec3().IsValid) return;
                    var pawn = flyingThing as Pawn;
                    pawn.Drawer.DrawAt(DrawPos);
                    //Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.graphic.MatFront, 0);
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
            if (usedTarget != null)
            {
                var pawn = usedTarget as Pawn;
                if (pawn != null && pawn.GetPosture() != PawnPosture.Standing &&
                    (origin - destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f)
                {
                    Impact(null);
                    return;
                }
                Impact(usedTarget);
            }
            else
            {
                Impact(null);
            }
        }

        protected virtual void Impact(Thing hitThing)
        {
            if (hitThing == null)
                if (Position.GetThingList(Map).FirstOrDefault(x => x == usedTarget) is Pawn p)
                    hitThing = p;


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
            Destroy(DestroyMode.Vanish);
        }
    }
}