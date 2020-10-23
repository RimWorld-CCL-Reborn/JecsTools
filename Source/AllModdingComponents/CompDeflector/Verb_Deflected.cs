using RimWorld;
using Verse;

namespace CompDeflector
{
    public class Verb_Deflected : Verb_Shoot
    {
        public bool lastShotReflected = false;

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            if (lastShotReflected)
                return true;
            return base.CanHitTargetFrom(root, targ);
        }

        //A16 TryCastShot Code
        protected override bool TryCastShot()
        {
            return TryCastShot_V1Vanilla_Modified();
        }

        public bool TryCastShot_V1Vanilla_Modified()
        {
            if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
                return false;
            var flag = TryFindShootLineFromTo(caster.Position, currentTarget, out var shootLine);
            if (verbProps.stopBurstWithoutLos && !flag)
                return false;
            var drawPos = caster.DrawPos;
            var projectile = (Projectile)GenSpawn.Spawn(verbProps.defaultProjectile, shootLine.Source, caster.Map);

            ///MODIFIED SECTION
            ////////////////////////////////////////////

            if (lastShotReflected)
            {
                ////Log.Message("lastShotReflected Called");
                projectile.Launch(caster, currentTarget, currentTarget, ProjectileHitFlags.IntendedTarget, EquipmentSource); //TODO
                return true;
            }

            ////////////////////////////////////////////
            //

            //projectile.FreeIntercept = canFreeInterceptNow && !projectile.def.projectile.flyOverhead;
            if (verbProps.forcedMissRadius > 0.5f)
            {
                var num = VerbUtility.CalculateAdjustedForcedMiss(verbProps.forcedMissRadius,
                    currentTarget.Cell - caster.Position);
                if (num > 0.5f)
                {
                    var max = GenRadial.NumCellsInRadius(verbProps.forcedMissRadius);
                    var num2 = Rand.Range(0, max);
                    if (num2 > 0)
                    {
                        if (DebugViewSettings.drawShooting)
                            MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToForRad"); // TODO: Translate()?
                        var c = currentTarget.Cell + GenRadial.RadialPattern[num2];
                        ProjectileHitFlags projectileHitFlags;
                        if (Rand.Chance(0.5f))
                        {
                            projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                        }
                        else
                        {
                            projectileHitFlags = ProjectileHitFlags.All;
                        }
                        if (!canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile.Launch(caster, currentTarget, c, projectileHitFlags, EquipmentSource); //TODO
                        return true;
                    }
                }
            }
            var shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
            var randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            var targetCoverDef = randomCoverToMissInto?.def;
            if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                if (DebugViewSettings.drawShooting)
                {
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToWild"); // TODO: Translate()?
                }
                shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
                ProjectileHitFlags projectileHitFlags2;
                if (Rand.Chance(0.5f))
                {
                    projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                }
                else
                {
                    projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                    if (canHitNonTargetPawnsNow)
                    {
                        projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                    }
                }
                projectile.Launch(caster, currentTarget, shootLine.Dest, projectileHitFlags2, EquipmentSource); //TODO
                return true;
            }
            if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance))
            {
                if (DebugViewSettings.drawShooting)
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToCover"); // TODO: Translate()?
                if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn)
                {
                    var projectileHitFlags5 = ProjectileHitFlags.NonTargetWorld;
                    if (canHitNonTargetPawnsNow)
                    {
                        projectileHitFlags5 |= ProjectileHitFlags.NonTargetPawns;
                    }
                    projectile.Launch(caster, currentTarget, randomCoverToMissInto, projectileHitFlags5, EquipmentSource);
                    return true;
                }
            }
            else
            {
                if (!Rand.Chance(shotReport.PassCoverChance))
                {
                    if (DebugViewSettings.drawShooting)
                    {
                        MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToCover"); // TODO: Translate()?
                    }
                    if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn)
                    {
                        var projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                        if (canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile.Launch(caster, drawPos, randomCoverToMissInto, currentTarget,
                            projectileHitFlags3, EquipmentSource, targetCoverDef);
                        return true;
                    }
                }
                if (DebugViewSettings.drawShooting)
                {
                    MoteMaker.ThrowText(caster.DrawPos, caster.Map, "ToHit"); // TODO: Translate()?
                }
                var projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
                if (canHitNonTargetPawnsNow)
                {
                    projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
                }
                if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
                {
                    projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
                }
                if (currentTarget.Thing != null)
                {
                    projectile.Launch(caster, drawPos, currentTarget, currentTarget, projectileHitFlags4,
                        EquipmentSource, targetCoverDef);
                }
                else
                {
                    projectile.Launch(caster, drawPos, shootLine.Dest, currentTarget, projectileHitFlags4,
                        EquipmentSource, targetCoverDef);
                }
                return true;
            }
            return false;
        }

    }
}
