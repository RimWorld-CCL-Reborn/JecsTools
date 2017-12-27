using RimWorld;
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
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;
            var flag = TryFindShootLineFromTo(caster.Position, currentTarget, out var shootLine);
            if (verbProps.stopBurstWithoutLos && !flag)
                return false;
            var drawPos = caster.DrawPos;
            var projectile = (Projectile) GenSpawn.Spawn(verbProps.defaultProjectile, shootLine.Source, caster.Map);

            ///MODIFIED SECTION
            ////////////////////////////////////////////

            if (lastShotReflected)
            {
                ////Log.Message("lastShotReflected Called");
                projectile.Launch(caster, drawPos, currentTarget, ownerEquipment);
                return true;
            }

            ////////////////////////////////////////////
            //

            projectile.FreeIntercept = canFreeInterceptNow && !projectile.def.projectile.flyOverhead;
            if (verbProps.forcedMissRadius > 0.5f)
            {
                float lengthHorizontalSquared = (currentTarget.Cell - caster.Position).LengthHorizontalSquared;
                float num;
                if (lengthHorizontalSquared < 9f)
                    num = 0f;
                else if (lengthHorizontalSquared < 25f)
                    num = verbProps.forcedMissRadius * 0.5f;
                else if (lengthHorizontalSquared < 49f)
                    num = verbProps.forcedMissRadius * 0.8f;
                else
                    num = verbProps.forcedMissRadius * 1f;
                if (num > 0.5f)
                {
                    var max = GenRadial.NumCellsInRadius(verbProps.forcedMissRadius);
                    var num2 = Rand.Range(0, max);
                    if (num2 > 0)
                    {
                        if (DebugViewSettings.drawShooting)
                            MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToForRad", -1f);
                        var c = currentTarget.Cell + GenRadial.RadialPattern[num2];
                        if (currentTarget.HasThing)
                            projectile.ThingToNeverIntercept = currentTarget.Thing;
                        if (!projectile.def.projectile.flyOverhead)
                            projectile.InterceptWalls = true;
                        projectile.Launch(caster, drawPos, c, ownerEquipment);
                        return true;
                    }
                }
            }
            var shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
            if (Rand.Value > shotReport.ChanceToNotGoWild_IgnoringPosture)
            {
                if (DebugViewSettings.drawShooting)
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToWild", -1f);
                shootLine.ChangeDestToMissWild();
                if (currentTarget.HasThing)
                    projectile.ThingToNeverIntercept = currentTarget.Thing;
                if (!projectile.def.projectile.flyOverhead)
                    projectile.InterceptWalls = true;
                projectile.Launch(caster, drawPos, shootLine.Dest, ownerEquipment);
                return true;
            }
            if (Rand.Value > shotReport.ChanceToNotHitCover)
            {
                if (DebugViewSettings.drawShooting)
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToCover", -1f);
                if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn)
                {
                    var randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                    if (!projectile.def.projectile.flyOverhead)
                        projectile.InterceptWalls = true;
                    projectile.Launch(caster, drawPos, randomCoverToMissInto, ownerEquipment);
                    return true;
                }
            }
            if (DebugViewSettings.drawShooting)
                MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToHit", -1f);
            if (!projectile.def.projectile.flyOverhead)
                projectile.InterceptWalls =
                    !currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full;
            if (currentTarget.Thing != null)
                projectile.Launch(caster, drawPos, currentTarget, ownerEquipment);
            else
                projectile.Launch(caster, drawPos, shootLine.Dest, ownerEquipment);
            return true;
        }
    }
}