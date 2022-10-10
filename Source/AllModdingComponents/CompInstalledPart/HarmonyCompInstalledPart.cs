using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompInstalledPart
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompInstalledPart
    {
        static HarmonyCompInstalledPart()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.installedpart");
            var type = typeof(HarmonyCompInstalledPart);

            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "InterfaceDrop"),
                prefix: new HarmonyMethod(type, nameof(InterfaceDrop_PreFix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment)),
                prefix: new HarmonyMethod(type, nameof(TryDropEquipment_PreFix)));
            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming)),
                prefix: new HarmonyMethod(type, nameof(DrawEquipmentAiming_PreFix)));
        }

        public static bool DrawEquipmentAiming_PreFix(Pawn ___pawn, Thing eq, Vector3 drawLoc,
            float aimAngle)
        {
            if (___pawn != null && eq.TryGetCompInstalledPart() is CompInstalledPart installedComp)
            {
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
                angle %= 360f;
                // end copied vanilla code

                var installedWeaponGraphic = installedComp.Props?.installedWeaponGraphic;
                var graphic_StackCount = installedWeaponGraphic?.Graphic as Graphic_StackCount ??
                                         eq.Graphic as Graphic_StackCount;
                var matSingle = graphic_StackCount != null
                    ? graphic_StackCount.SubGraphicForStackCount(1, eq.def).MatSingle
                    : installedWeaponGraphic?.Graphic?.MatSingle ?? eq.Graphic.MatSingle;
                var s = new Vector3(
                    installedWeaponGraphic?.drawSize.x ?? eq.def.graphicData.drawSize.x, 1f,
                    installedWeaponGraphic?.drawSize.y ?? eq.def.graphicData.drawSize.y);
                var matrix = Matrix4x4.TRS(drawLoc, Quaternion.AngleAxis(angle, Vector3.up), s);
                Graphics.DrawMesh(flip ? MeshPool.plane10Flip : MeshPool.plane10, matrix, matSingle, 0);
                return false;
            }
            return true;
        }


        // Verse.Pawn_EquipmentTracker
        public static bool TryDropEquipment_PreFix(ThingWithComps eq, out ThingWithComps resultingEq, ref bool __result)
        {
            resultingEq = null;
            return __result = eq?.GetCompInstalledPart()?.uninstalled ?? true;
        }

        // RimWorld.Pawn_ApparelTracker
        public static bool InterfaceDrop_PreFix(ITab_Pawn_Gear __instance, Thing t)
        {
            if (t is Apparel apparel)
            {
                var pawn = itabPawnGearSelPawnForGearGetter(__instance);
                if (pawn?.apparel?.WornApparel.Contains(apparel) ?? false)
                {
                    var installedPart = apparel.GetCompInstalledPart();
                    if (installedPart != null)
                        if (!installedPart.uninstalled)
                            return false;
                }
            }
            return true;
        }

        // Note: This is an open instance delegate where the first argument is the instance.
        private static readonly Func<ITab_Pawn_Gear, Pawn> itabPawnGearSelPawnForGearGetter =
            (Func<ITab_Pawn_Gear, Pawn>)AccessTools.PropertyGetter(typeof(ITab_Pawn_Gear), "SelPawnForGear")
            .CreateDelegate(typeof(Func<ITab_Pawn_Gear, Pawn>));
    }
}
