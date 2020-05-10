using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompOversizedWeapon
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompOversizedWeapon
    {
        static HarmonyCompOversizedWeapon()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.oversized");
            harmony.Patch(typeof(PawnRenderer).GetMethod("DrawEquipmentAiming"),
                new HarmonyMethod(typeof(HarmonyCompOversizedWeapon).GetMethod("DrawEquipmentAimingPreFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Thing), "get_DefaultGraphic"), null,
                new HarmonyMethod(typeof(HarmonyCompOversizedWeapon), nameof(get_DefaultGraphic_PostFix)));
        }

        /// <summary>
        ///     Adds another "layer" to the equipment aiming if they have a
        ///     weapon with a CompActivatableEffect.
        /// </summary>
        public static bool DrawEquipmentAimingPreFix(Pawn ___pawn, Thing eq, Vector3 drawLoc, float aimAngle)
        {
            if (___pawn == null) return true;
            if (eq is ThingWithComps thingWithComps)
            {
                var compOversizedWeapon = thingWithComps.TryGetComp<CompOversizedWeapon>();
                if (compOversizedWeapon != null)
                {
                    //If the deflector is animating now, deflector handles drawing (and already has the drawSize fix).
                    if (compOversizedWeapon.CompDeflectorIsAnimatingNow) return false;

                    var flip = false;
                    var num = aimAngle - 90f;
                    
                    if (aimAngle > 20f && aimAngle < 160f)
                    {
                        num += eq.def.equippedAngleOffset;
                    }
                    else if (aimAngle > 200f && aimAngle < 340f)
                    {
                        flip = true;
                        num -= 180f;
                        num -= eq.def.equippedAngleOffset;
                    }
                    else
                    {
                        num = AdjustOffsetAtPeace(eq, ___pawn, compOversizedWeapon, num);
                    }
                    
                    if (!___pawn.IsFighting())
                    {
                        if (compOversizedWeapon.Props != null && compOversizedWeapon.Props.verticalFlipNorth && ___pawn.Rotation == Rot4.North)
                        {
                            num += 180f;
                        }
                        num = AdjustNonCombatRotation(___pawn, num, compOversizedWeapon);
                    }
                    num %= 360f;

                    var graphic_StackCount = eq.Graphic as Graphic_StackCount;
                    Material matSingle;
                    if (graphic_StackCount != null)
                        matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                    else
                        matSingle = eq.Graphic.MatSingle;

                    var s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
                    var matrix = default(Matrix4x4);

                    Vector3 curOffset = AdjustRenderOffsetFromDir(___pawn, compOversizedWeapon);
                    matrix.SetTRS(drawLoc + curOffset, Quaternion.AngleAxis(num, Vector3.up), s);                        
                    
                    Graphics.DrawMesh(!flip ? MeshPool.plane10 : MeshPool.plane10Flip, matrix, matSingle, 0);
                    if (compOversizedWeapon.Props != null && compOversizedWeapon.Props.isDualWeapon)
                    {
                        curOffset = new Vector3(-1f * curOffset.x, curOffset.y, curOffset.z);
                        Mesh curPool;
                        if (___pawn.Rotation == Rot4.North || ___pawn.Rotation == Rot4.South)
                        {
                            num += 135f;
                            num %= 360f;
                            curPool = !flip ? MeshPool.plane10Flip : MeshPool.plane10;
                        }
                        else
                        {
                            curOffset = new Vector3(curOffset.x, curOffset.y - 0.1f, curOffset.z + 0.15f);
                            curPool = !flip ? MeshPool.plane10 : MeshPool.plane10Flip;
                        }
                        matrix.SetTRS(drawLoc + curOffset, Quaternion.AngleAxis(num, Vector3.up), s);                        
                        Graphics.DrawMesh(curPool, matrix, matSingle, 0);
                    }
                    return false;
                }
            }
            return true;
        }

        private static float AdjustOffsetAtPeace(Thing eq, Pawn pawn, CompOversizedWeapon compOversizedWeapon, float num)
        {
            var offsetAtPeace = eq.def.equippedAngleOffset;
            if (compOversizedWeapon.Props != null && !pawn.IsFighting() && compOversizedWeapon.Props.verticalFlipOutsideCombat)
            {
                offsetAtPeace += 180f;
            }
            num += offsetAtPeace;
            return num;
        }

        private static float AdjustNonCombatRotation(Pawn pawn, float num, CompOversizedWeapon compOversizedWeapon)
        {
            if (compOversizedWeapon.Props != null)
            {
                if (pawn.Rotation == Rot4.North)
                {
                    num += compOversizedWeapon.Props.angleAdjustmentNorth;
                }
                else if (pawn.Rotation == Rot4.East)
                {
                    num += compOversizedWeapon.Props.angleAdjustmentEast;
                }
                else if (pawn.Rotation == Rot4.West)
                {
                    num += compOversizedWeapon.Props.angleAdjustmentWest;
                }
                else if (pawn.Rotation == Rot4.South)
                {
                    num += compOversizedWeapon.Props.angleAdjustmentSouth;
                }
            }
            return num;
        }

        private static Vector3 AdjustRenderOffsetFromDir(Pawn pawn, CompOversizedWeapon compOversizedWeapon)
        {
            var curDir = pawn.Rotation;
         
            Vector3 curOffset = Vector3.zero;
         
            if (compOversizedWeapon.Props != null)
            {
         
                curOffset = compOversizedWeapon.Props.northOffset;
                if (curDir == Rot4.East)
                {
                    curOffset = compOversizedWeapon.Props.eastOffset;
                }
                else if (curDir == Rot4.South)
                {
                    curOffset = compOversizedWeapon.Props.southOffset;
                }
                else if (curDir == Rot4.West)
                {
                    curOffset = compOversizedWeapon.Props.westOffset;
                }
            }
         
            return curOffset;
        }

        public static void get_DefaultGraphic_PostFix(Thing __instance, Graphic ___graphicInt, ref Graphic __result)
        {
            if (___graphicInt == null) return;
            if (__instance.ParentHolder is Pawn) return;

            var compOversizedWeapon = __instance.TryGetComp<CompOversizedWeapon>();
            if (compOversizedWeapon != null)
            {
                //Following commented-out section is an unnecessary "optimization" that actually hurts performance due to the reflection involved.
                //var activatableEffect =
                //    thingWithComps.AllComps.FirstOrDefault(
                //        y => y.GetType().ToString().Contains("ActivatableEffect"));
                //if (activatableEffect != null)
                //{
                //    var getPawn = Traverse.Create(activatableEffect).Property("GetPawn").GetValue<Pawn>();
                //    if (getPawn != null)
                //        return;
                //}
                if (compOversizedWeapon.Props?.groundGraphic == null)
                {
                    ___graphicInt.drawSize = __instance.def.graphicData.drawSize;
                    __result = ___graphicInt;
                }
                else
                {
                    if (compOversizedWeapon.IsEquipped)
                    {
                        ___graphicInt.drawSize = __instance.def.graphicData.drawSize;
                        __result = ___graphicInt;
                    }
                    else
                    {
                        if (compOversizedWeapon.Props?.groundGraphic?.GraphicColoredFor(__instance) is Graphic
                            newResult)
                        {
                            newResult.drawSize = compOversizedWeapon.Props.groundGraphic.drawSize;
                            __result = newResult;
                        }
                        else
                        {
                            ___graphicInt.drawSize = __instance.def.graphicData.drawSize;
                            __result = ___graphicInt;
                        }
                    }
                }
            }
        }
    }
}