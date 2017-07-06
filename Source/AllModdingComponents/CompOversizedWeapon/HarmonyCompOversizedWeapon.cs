using Harmony;
using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
namespace CompOversizedWeapon
{
    [StaticConstructorOnStartup]
    static class HarmonyCompOversizedWeapon
    {
        static HarmonyCompOversizedWeapon()
        {

            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.oversized");
            harmony.Patch(typeof(PawnRenderer).GetMethod("DrawEquipmentAiming"), new HarmonyMethod(typeof(HarmonyCompOversizedWeapon).GetMethod("DrawEquipmentAimingPreFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Thing), "get_DefaultGraphic"), null, new HarmonyMethod(typeof(HarmonyCompOversizedWeapon).GetMethod("get_Graphic_PostFix")));


        }


        /// <summary>
        /// Adds another "layer" to the equipment aiming if they have a
        /// weapon with a CompActivatableEffect.
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
                ThingComp deflector = thingWithComps.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString() == "CompDeflector.CompDeflector" || y.GetType().BaseType.ToString() == "CompDeflector.CompDeflector");
                if (deflector != null)
                {
                    bool isAnimatingNow = Traverse.Create(deflector).Property("IsAnimatingNow").GetValue<bool>();
                    if (isAnimatingNow)
                    {
                        return false;
                    }

                }

                CompOversizedWeapon compOversizedWeapon = thingWithComps.TryGetComp<CompOversizedWeapon>();
                if (compOversizedWeapon != null)
                {
                    bool flip = false;
                    float num = aimAngle - 90f;
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
                        mesh = MeshPool.plane10;
                        num += eq.def.equippedAngleOffset;
                    }
                    num %= 360f;
                    Graphic_StackCount graphic_StackCount = eq.Graphic as Graphic_StackCount;
                    Material matSingle;
                    if (graphic_StackCount != null)
                    {
                        matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                    }
                    else
                    {
                        matSingle = eq.Graphic.MatSingle;
                    }

                    Vector3 s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
                    Matrix4x4 matrix = default(Matrix4x4);
                    matrix.SetTRS(drawLoc, Quaternion.AngleAxis(num, Vector3.up), s);
                    if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
                    else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
                    return false;
                }
            }
            //}
            return true;
        }


        public static void Get_Graphic_PostFix(Thing __instance, ref Graphic __result)
        {
            Graphic tempGraphic = Traverse.Create(__instance).Field("graphicInt").GetValue<Graphic>();
            if (tempGraphic != null)
            {
                if (__instance is ThingWithComps thingWithComps)
                {
                    if (thingWithComps.ParentHolder is Pawn)
                    {
                        return;
                    }
                    ThingComp activatableEffect = thingWithComps.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString().Contains("ActivatableEffect"));
                    if (activatableEffect != null)
                    {
                        Pawn getPawn = Traverse.Create(activatableEffect).Property("GetPawn").GetValue<Pawn>();
                        if (getPawn != null)
                        {
                            //Log.Message("1");
                            return;
                        }
                    }
                    CompOversizedWeapon compOversizedWeapon = thingWithComps.TryGetComp<CompOversizedWeapon>();
                    if (compOversizedWeapon != null)
                    {
                        tempGraphic.drawSize = __instance.def.graphicData.drawSize;
                        __result = tempGraphic;
                    }
                }
            }

        }


    }
}
