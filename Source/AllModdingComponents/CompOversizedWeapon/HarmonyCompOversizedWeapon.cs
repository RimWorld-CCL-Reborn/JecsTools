using System;
using System.Linq;
using System.Security.Cryptography;
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
                new HarmonyMethod(typeof(HarmonyCompOversizedWeapon), nameof(get_Graphic_PostFix)));
        }


        /// <summary>
        ///     Adds another "layer" to the equipment aiming if they have a
        ///     weapon with a CompActivatableEffect.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="eq"></param>
        /// <param name="drawLoc"></param>
        /// <param name="aimAngle"></param>
        public static bool DrawEquipmentAimingPreFix(PawnRenderer __instance, Thing eq, Vector3 drawLoc, float aimAngle)
        {
            if (eq is ThingWithComps thingWithComps)
            {
                //If the deflector is active, it's already using this code.
                var deflector = thingWithComps.AllComps.FirstOrDefault(y =>
                    y.GetType().ToString() == "CompDeflector.CompDeflector" ||
                    y.GetType().BaseType.ToString() == "CompDeflector.CompDeflector");
                if (deflector != null)
                {
                    var isAnimatingNow = Traverse.Create(deflector).Property("IsAnimatingNow").GetValue<bool>();
                    if (isAnimatingNow)
                        return false;
                }

                var compOversizedWeapon = thingWithComps.TryGetComp<CompOversizedWeapon>();
                if (compOversizedWeapon != null)
                {
                    var flip = false;
                    var num = aimAngle - 90f;
                    var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                    if (pawn == null) return true;
                    
                    Mesh mesh;
                    if (aimAngle > 20f && aimAngle < 160f)
                    {
                        mesh = MeshPool.plane10;
                        num += eq.def.equippedAngleOffset;
                    }
                    else if (aimAngle > 200f && aimAngle < 340f)
                    {
                        mesh = MeshPool.plane10Flip;
                        flip = true;
                        num -= 180f;
                        num -= eq.def.equippedAngleOffset;
                    }
                    else
                    {
                        num = AdjustOffsetAtPeace(eq, pawn, compOversizedWeapon, num);
                    }
                    
                    if (compOversizedWeapon.Props != null && (!pawn.IsFighting() && (compOversizedWeapon.Props.verticalFlipNorth && pawn.Rotation == Rot4.North)))
                    {
                        num += 180f;
                    }
                    if (!pawn.IsFighting())
                    {
                        num = AdjustNonCombatRotation(pawn, num, compOversizedWeapon);
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

         
                    Vector3 curOffset = AdjustRenderOffsetFromDir(pawn, compOversizedWeapon);
                    matrix.SetTRS(drawLoc + curOffset, Quaternion.AngleAxis(num, Vector3.up), s);                        
                    
                    Graphics.DrawMesh(!flip ? MeshPool.plane10 : MeshPool.plane10Flip, matrix, matSingle, 0);
                    if (compOversizedWeapon.Props != null && compOversizedWeapon.Props.isDualWeapon)
                    {
                        curOffset = new Vector3(-1f * curOffset.x, curOffset.y, curOffset.z);
                        Mesh curPool;
                        if (pawn.Rotation == Rot4.North || pawn.Rotation == Rot4.South)
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
            //}
            return true;
        }

        private static float AdjustOffsetAtPeace(Thing eq, Pawn pawn, CompOversizedWeapon compOversizedWeapon, float num)
        {
            Mesh mesh;
            mesh = MeshPool.plane10;
            var offsetAtPeace = eq.def.equippedAngleOffset;
            if (compOversizedWeapon.Props != null && (!pawn.IsFighting() && compOversizedWeapon.Props.verticalFlipOutsideCombat))
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

        public static void get_Graphic_PostFix(Thing __instance, ref Graphic __result)
        {
            var tempGraphic = Traverse.Create(__instance).Field("graphicInt").GetValue<Graphic>();
            if (tempGraphic != null)
                if (__instance is ThingWithComps thingWithComps)
                {
                    if (thingWithComps.ParentHolder is Pawn)
                        return;
                    var activatableEffect =
                        thingWithComps.AllComps.FirstOrDefault(
                            y => y.GetType().ToString().Contains("ActivatableEffect"));
                    if (activatableEffect != null)
                    {
                        var getPawn = Traverse.Create(activatableEffect).Property("GetPawn").GetValue<Pawn>();
                        if (getPawn != null)
                            return;
                    }
                    var compOversizedWeapon = thingWithComps.TryGetComp<CompOversizedWeapon>();
                    if (compOversizedWeapon != null)
                    {
                        if (compOversizedWeapon.Props?.groundGraphic == null)
                        {
                            tempGraphic.drawSize = __instance.def.graphicData.drawSize;
                            __result = tempGraphic;   
                        }
                        else
                        {
                            if (compOversizedWeapon.IsEquipped)
                            {
                                tempGraphic.drawSize = __instance.def.graphicData.drawSize;
                                __result = tempGraphic;
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
                                    tempGraphic.drawSize = __instance.def.graphicData.drawSize;
                                    __result = tempGraphic;   
                                }
                            }
                        }
                    }
                }
        }
    }
}