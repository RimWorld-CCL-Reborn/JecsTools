using System;
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
            if (___pawn != null)
            {
                var pawn_EquipmentTracker = ___pawn.equipment;
                if (pawn_EquipmentTracker != null)
                    foreach (var thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                        if (thingWithComps != null)
                        {
                            var compDeflector = thingWithComps.GetCompDeflector();
                            if (compDeflector != null)
                                if (compDeflector.IsAnimatingNow)
                                {
                                    var flip = false;
                                    if (!Find.TickManager.Paused && compDeflector.IsAnimatingNow)
                                        compDeflector.AnimationDeflectionTicks -= 20;
                                    var offset = eq.def.equippedAngleOffset;
                                    var num = aimAngle - 90f;
                                    if (aimAngle > 20f && aimAngle < 160f)
                                    {
                                        num += offset;
                                        if (compDeflector.IsAnimatingNow)
                                            num += (compDeflector.AnimationDeflectionTicks + 1) / 2;
                                    }
                                    else if (aimAngle > 200f && aimAngle < 340f)
                                    {
                                        flip = true;
                                        num -= 180f;
                                        num -= offset;
                                        if (compDeflector.IsAnimatingNow)
                                            num -= (compDeflector.AnimationDeflectionTicks + 1) / 2;
                                    }
                                    else
                                    {
                                        num += offset;
                                        if (compDeflector.IsAnimatingNow)
                                            num += (compDeflector.AnimationDeflectionTicks + 1) / 2;
                                    }
                                    num %= 360f;
                                    var graphic_StackCount = eq.Graphic as Graphic_StackCount;
                                    Material matSingle;
                                    if (graphic_StackCount != null)
                                        matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                                    else
                                        matSingle = eq.Graphic.MatSingle;

                                    var s = new Vector3(eq.def.graphicData.drawSize.x, 1f,
                                        eq.def.graphicData.drawSize.y);
                                    var matrix = default(Matrix4x4);
                                    matrix.SetTRS(drawLoc, Quaternion.AngleAxis(num, Vector3.up), s);
                                    if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
                                    else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
                                }
                        }
            }
        }

        public static bool TakeDamage_PreFix(Thing __instance, ref DamageInfo dinfo)
        {
            //if (dinfo.Instigator == null) return true;
            try
            {
                if (__instance is Pawn pawn)
                {
                    var pawn_EquipmentTracker = pawn.equipment;
                    if (pawn_EquipmentTracker != null)
                        foreach (var thingWithComps in pawn_EquipmentTracker.AllEquipmentListForReading)
                        {
                            var compDeflector = thingWithComps?.GetCompDeflector();
                            if (compDeflector == null) continue;
                            if (dinfo.Def == DamageDefOf.Bomb) continue;
                            if (dinfo.Def == DamageDefOf.Flame) continue;
                            if (dinfo.Def.isExplosive) continue;
                            if (!dinfo.Weapon.IsMeleeWeapon)
                            {
                                compDeflector.PostPreApplyDamage(dinfo, out var newAbsorbed);
                                if (newAbsorbed)
                                {
                                    compDeflector.AnimationDeflectionTicks = 1200;
                                    dinfo.SetAmount(0);
                                    return true;
                                }
                            }
                            else
                            {
                                if (compDeflector.TrySpecialMeleeBlock())
                                {
                                    dinfo.SetAmount(0);
                                    return true;
                                }
                            }
                        }
                }
            }
            catch (NullReferenceException e) // TODO: Is this still needed?
            {
                Log.Message(e.ToString());
            }
            return true;
        }
    }
}
