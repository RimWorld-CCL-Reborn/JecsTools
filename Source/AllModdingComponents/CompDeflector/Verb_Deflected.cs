using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CompDeflector
{
    public class Verb_Deflected : Verb_Shoot
    {
        public bool lastShotReflected = false;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (lastShotReflected) return true;
            return base.CanHitTargetFrom(root, targ);
        }

        //A16 TryCastShot Code
        protected override bool TryCastShot()
        {
            return TryCastShot_A16Vanilla_Modified();
        }

        public bool TryCastShot_A16Vanilla_Modified()
        {
            if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
            {
                return false;
            }
            ShootLine shootLine;
            bool flag = base.TryFindShootLineFromTo(this.caster.Position, this.currentTarget, out shootLine);
            if (this.verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            Vector3 drawPos = this.caster.DrawPos;
            Projectile projectile = (Projectile)GenSpawn.Spawn(this.verbProps.projectileDef, shootLine.Source, this.caster.Map);

            ///MODIFIED SECTION
            ////////////////////////////////////////////

            if (lastShotReflected)
            {
                ////Log.Message("lastShotReflected Called");
                projectile.Launch(this.caster, drawPos, this.currentTarget, this.ownerEquipment);
                return true;
            }

            ////////////////////////////////////////////
            //

            projectile.FreeIntercept = (this.canFreeInterceptNow && !projectile.def.projectile.flyOverhead);
            if (this.verbProps.forcedMissRadius > 0.5f)
            {
                float lengthHorizontalSquared = (this.currentTarget.Cell - this.caster.Position).LengthHorizontalSquared;
                float num;
                if (lengthHorizontalSquared < 9f)
                {
                    num = 0f;
                }
                else if (lengthHorizontalSquared < 25f)
                {
                    num = this.verbProps.forcedMissRadius * 0.5f;
                }
                else if (lengthHorizontalSquared < 49f)
                {
                    num = this.verbProps.forcedMissRadius * 0.8f;
                }
                else
                {
                    num = this.verbProps.forcedMissRadius * 1f;
                }
                if (num > 0.5f)
                {
                    int max = GenRadial.NumCellsInRadius(this.verbProps.forcedMissRadius);
                    int num2 = Rand.Range(0, max);
                    if (num2 > 0)
                    {
                        if (DebugViewSettings.drawShooting)
                        {
                            MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToForRad", -1f);
                        }
                        IntVec3 c = this.currentTarget.Cell + GenRadial.RadialPattern[num2];
                        if (this.currentTarget.HasThing)
                        {
                            projectile.ThingToNeverIntercept = this.currentTarget.Thing;
                        }
                        if (!projectile.def.projectile.flyOverhead)
                        {
                            projectile.InterceptWalls = true;
                        }
                        projectile.Launch(this.caster, drawPos, c, this.ownerEquipment);
                        return true;
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
            if (Rand.Value > shotReport.ChanceToNotGoWild_IgnoringPosture)
            {
                if (DebugViewSettings.drawShooting)
                {
                    MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToWild", -1f);
                }
                shootLine.ChangeDestToMissWild();
                if (this.currentTarget.HasThing)
                {
                    projectile.ThingToNeverIntercept = this.currentTarget.Thing;
                }
                if (!projectile.def.projectile.flyOverhead)
                {
                    projectile.InterceptWalls = true;
                }
                projectile.Launch(this.caster, drawPos, shootLine.Dest, this.ownerEquipment);
                return true;
            }
            if (Rand.Value > shotReport.ChanceToNotHitCover)
            {
                if (DebugViewSettings.drawShooting)
                {
                    MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToCover", -1f);
                }
                if (this.currentTarget.Thing != null && this.currentTarget.Thing.def.category == ThingCategory.Pawn)
                {
                    Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                    if (!projectile.def.projectile.flyOverhead)
                    {
                        projectile.InterceptWalls = true;
                    }
                    projectile.Launch(this.caster, drawPos, randomCoverToMissInto, this.ownerEquipment);
                    return true;
                }
            }
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToHit", -1f);
            }
            if (!projectile.def.projectile.flyOverhead)
            {
                projectile.InterceptWalls = (!this.currentTarget.HasThing || this.currentTarget.Thing.def.Fillage == FillCategory.Full);
            }
            if (this.currentTarget.Thing != null)
            {
                projectile.Launch(this.caster, drawPos, this.currentTarget, this.ownerEquipment);
            }
            else
            {
                projectile.Launch(this.caster, drawPos, shootLine.Dest, this.ownerEquipment);
            }
            return true;
        }
    }
}
