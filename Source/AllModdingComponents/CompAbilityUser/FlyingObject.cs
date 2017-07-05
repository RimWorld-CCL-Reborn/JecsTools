﻿using RimWorld;
using UnityEngine;
using Verse;
namespace AbilityUser
{
    /// <summary>
    /// A special version of a projectile.
    /// This one "stores" a base object and "delivers" it.
    /// </summary>
    public class FlyingObject : ThingWithComps
    {
        protected Vector3 origin;
        protected Vector3 destination;
        protected float speed = 30.0f;
        protected int ticksToImpact;
        protected Thing launcher;
        protected Thing assignedTarget;
        protected Thing flyingThing;
        public DamageInfo? impactDamage;

        protected int StartingTicksToImpact
        {
            get
            {
                int num = Mathf.RoundToInt((this.origin - this.destination).magnitude / (this.speed / 100f));
                if (num < 1)
                {
                    num = 1;
                }
                return num;
            }
        }


        protected IntVec3 DestinationCell => new IntVec3(this.destination);

        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 b = (this.destination - this.origin) * (1f - this.ticksToImpact / (float)this.StartingTicksToImpact);
                return this.origin + b + Vector3.up * this.def.Altitude;
            }
        }

        public virtual Quaternion ExactRotation => Quaternion.LookRotation(this.destination - this.origin);

        public override Vector3 DrawPos => this.ExactPosition;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<Vector3>(ref this.origin, "origin", default(Vector3), false);
            Scribe_Values.Look<Vector3>(ref this.destination, "destination", default(Vector3), false);
            Scribe_Values.Look<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_References.Look<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.Look<Thing>(ref this.launcher, "launcher", false);
            Scribe_References.Look<Thing>(ref this.flyingThing, "flyingThing");
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing, DamageInfo? impactDamage) => this.Launch(launcher, this.Position.ToVector3Shifted(), targ, flyingThing, impactDamage);

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing flyingThing) => this.Launch(launcher, this.Position.ToVector3Shifted(), targ, flyingThing);

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing flyingThing, DamageInfo? newDamageInfo = null)
        {
            //Despawn the object to fly
            if (flyingThing.Spawned) flyingThing.DeSpawn();

            this.launcher = launcher;
            this.origin = origin;
            this.impactDamage = newDamageInfo;
            this.flyingThing = flyingThing;
            if (targ.Thing != null)
            {
                this.assignedTarget = targ.Thing;
            }
            this.destination = targ.Cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            this.ticksToImpact = this.StartingTicksToImpact;
        }

        public override void Tick()
        {
            base.Tick();
            Vector3 exactPosition = this.ExactPosition;
            this.ticksToImpact--;
            if (!this.ExactPosition.InBounds(this.Map))
            {
                this.ticksToImpact++;
                this.Position = this.ExactPosition.ToIntVec3();
                this.Destroy(DestroyMode.Vanish);
                return;
            }

            this.Position = this.ExactPosition.ToIntVec3();
            if (this.ticksToImpact <= 0)
            {
                if (this.DestinationCell.InBounds(this.Map))
                {
                    this.Position = this.DestinationCell;
                }
                this.ImpactSomething();
                return;
            }

        }

        public override void Draw()
        {
            if (this.flyingThing != null)
            {
                if (this.flyingThing is Pawn)
                {
                    if (this.DrawPos == null) return;
                    if (!this.DrawPos.ToIntVec3().IsValid) return;
                    Pawn pawn = this.flyingThing as Pawn;
                    pawn.Drawer.DrawAt(this.DrawPos);
                    //Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.graphic.MatFront, 0);
                }
                else
                {
                    Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.flyingThing.def.DrawMatSingle, 0);
                }
                base.Comps_PostDraw();
            }
        }

        private void ImpactSomething()
        {
            if (this.assignedTarget != null)
            {
                if (this.assignedTarget is Pawn pawn && pawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f)
                {
                    this.Impact(null);
                    return;
                }
                this.Impact(this.assignedTarget);
                return;
            }
            else
            {
                this.Impact(null);
                return;
            }
        }

        protected virtual void Impact(Thing hitThing)
        {
            GenSpawn.Spawn(this.flyingThing, this.Position, this.Map);
            if (this.impactDamage != null)
            {
                for (int i = 0; i < 3; i++) this.flyingThing.TakeDamage(this.impactDamage.Value);
            }
            this.Destroy(DestroyMode.Vanish);
        }


    }
}
