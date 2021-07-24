using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompDeflector
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompDeflector
    {
        static HarmonyCompDeflector()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.deflector");
            var type = typeof(HarmonyCompDeflector);

            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage)),
                prefix: new HarmonyMethod(type, nameof(TakeDamage_PreFix)));
            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming)),
                postfix: new HarmonyMethod(type, nameof(DrawEquipmentAimingPostFix)));
        }

        public static void DrawEquipmentAimingPostFix(Pawn ___pawn, Thing eq, Vector3 drawLoc,
            float aimAngle)
        {
            var pawn_EquipmentTracker = ___pawn.equipment;
            if (pawn_EquipmentTracker != null)
            {
                foreach (var thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                {
                    var compDeflector = thingWithComps?.GetCompDeflector();
                    if (compDeflector != null)
                    {
                        if (compDeflector.IsAnimatingNow)
                        {
                            if (!Find.TickManager.Paused && compDeflector.IsAnimatingNow)
                                compDeflector.AnimationDeflectionTicks -= 20;

                            // start copied vanilla code (with mesh = flip ? MeshPool.plane10Flip : MeshPool.plane10)
                            var flip = false;
                            var angle = aimAngle - 90f;
                            if (aimAngle > 20f && aimAngle < 160f)
                            {
                                angle += eq.def.equippedAngleOffset;
                            }
                            else if (aimAngle > 200f && aimAngle < 340f)
                            {
                                flip = true;
                                angle -= 180f;
                                angle -= eq.def.equippedAngleOffset;
                            }
                            else
                            {
                                angle += eq.def.equippedAngleOffset;
                            }
                            // end copied vanilla code

                            if (compDeflector.IsAnimatingNow)
                            {
                                float animationTicks = compDeflector.AnimationDeflectionTicks;
                                if (animationTicks > 0)
                                {
                                    if (flip)
                                        angle -= (animationTicks + 1) / 2;
                                    else
                                        angle += (animationTicks + 1) / 2;
                                }
                            }

                            angle %= 360f; // copied vanilla code

                            var matSingle = eq.Graphic is Graphic_StackCount graphic_StackCount
                                ? graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle
                                : eq.Graphic.MatSingle;
                            var s = new Vector3(eq.def.graphicData.drawSize.x, 1f,
                                eq.def.graphicData.drawSize.y);
                            var matrix = Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(angle, Vector3.up), s);
                            Graphics.DrawMesh(flip ? MeshPool.plane10Flip : MeshPool.plane10, matrix, matSingle, 0);
                        }
                    }
                }
            }
        }

        public static bool TakeDamage_PreFix(Thing __instance, ref DamageInfo dinfo)
        {
            //if (dinfo.Instigator == null) return true;
            if (__instance is Pawn pawn)
            {
                var pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                    foreach (var thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                    {
                        if (dinfo.Def == DamageDefOf.Bomb || dinfo.Def == DamageDefOf.Flame || dinfo.Def.isExplosive)
                            continue;
                        var compDeflector = thingWithComps?.GetCompDeflector();
                        if (compDeflector == null)
                            continue;
                        if (dinfo.Weapon?.IsMeleeWeapon ?? false)
                        {
                            if (compDeflector.TrySpecialMeleeBlock())
                            {
                                dinfo.SetAmount(0);
                                return true;
                            }
                        }
                        else
                        {
                            compDeflector.PostPreApplyDamage(dinfo, out var absorbed);
                            if (absorbed)
                            {
                                compDeflector.AnimationDeflectionTicks = 1200;
                                dinfo.SetAmount(0);
                                return true;
                            }
                        }
                    }
            }
            return true;
        }
    }
}
