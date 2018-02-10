using System.Linq;
using Harmony;
using UnityEngine;
using Verse;

namespace CompOversizedWeapon
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompOversizedWeapon
    {
        static HarmonyCompOversizedWeapon()
        {
            var harmony = HarmonyInstance.Create("rimworld.jecrell.comps.oversized");
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
                    var graphic_StackCount = eq.Graphic as Graphic_StackCount;
                    Material matSingle;
                    if (graphic_StackCount != null)
                        matSingle = graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle;
                    else
                        matSingle = eq.Graphic.MatSingle;

                    var s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
                    var matrix = default(Matrix4x4);
                    matrix.SetTRS(drawLoc + compOversizedWeapon.Props.offset, Quaternion.AngleAxis(num, Vector3.up), s);
                    if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
                    else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
                    return false;
                }
            }
            //}
            return true;
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
                        tempGraphic.drawSize = __instance.def.graphicData.drawSize;
                        __result = tempGraphic;
                    }
                }
        }
    }
}