using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompActivatableEffect
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompActivatableEffect
    {
        static HarmonyCompActivatableEffect()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.activator");
            
            harmony.Patch(typeof(Pawn).GetMethod("GetGizmos"), null,
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect).GetMethod(nameof(GetGizmosPrefix))));
            
            harmony.Patch(typeof(PawnRenderer).GetMethod("DrawEquipmentAiming"), null,
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect), nameof(DrawEquipmentAimingPostFix)));

            harmony.Patch(AccessTools.Method(typeof(Verb), "TryStartCastOn", new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool)}),
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect), nameof(TryStartCastOnPrefix)), null);

            harmony.Patch(typeof(Pawn_DraftController).GetMethod("set_Drafted"), null,
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect).GetMethod(nameof(set_DraftedPostFix))));

            harmony.Patch(typeof(Pawn).GetMethod("ExitMap"),
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect).GetMethod(nameof(ExitMap_PreFix))), null);

            harmony.Patch(typeof(Pawn_EquipmentTracker).GetMethod("TryDropEquipment"),
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect).GetMethod(nameof(TryDropEquipment_PreFix))), null);

            harmony.Patch(typeof(Pawn_DraftController).GetMethod("set_Drafted"), null,
                new HarmonyMethod(typeof(HarmonyCompActivatableEffect).GetMethod(nameof(set_DraftedPostFix))));
        }

        //=================================== COMPACTIVATABLE

        // Verse.Pawn_EquipmentTracker
        public static void TryDropEquipment_PreFix(Pawn_EquipmentTracker __instance)
        {
            if (__instance.Primary?.GetComp<CompActivatableEffect>() is CompActivatableEffect compActivatableEffect &&
                compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                compActivatableEffect.TryDeactivate();
        }

        public static void ExitMap_PreFix(Pawn __instance)
        {
            if (__instance.equipment?.Primary?.GetComp<CompActivatableEffect>() is CompActivatableEffect compActivatableEffect &&
                compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                compActivatableEffect.TryDeactivate();
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void set_DraftedPostFix(Pawn_DraftController __instance, bool value)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (__instance.pawn?.equipment?.Primary?.GetComp<CompActivatableEffect>() is CompActivatableEffect compActivatableEffect)
                if (value == false)
                {
                    if (compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                        compActivatableEffect.TryDeactivate();
                }
                else
                {
                    if (compActivatableEffect.CurrentState == CompActivatableEffect.State.Deactivated)
                        compActivatableEffect.TryActivate();
                }
        }

        public static bool TryStartCastOnPrefix(ref bool __result, Verb __instance)
        {
            if (__instance.caster is Pawn pawn && pawn.equipment?.Primary is ThingWithComps thingWithComps &&
                thingWithComps.GetComp<CompActivatableEffect>() is CompActivatableEffect compActivatableEffect)
            {
                //Equipment source throws errors when checked while casting abilities with a weapon equipped.
                // to avoid this error preventing our code from executing, we do a try/catch.
                try
                {
                    if (__instance.EquipmentSource != thingWithComps)
                        return true;
                }
                catch
                {
                }

                if (compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                    return true;
                else if (compActivatableEffect.TryActivate())
                    return true;
                if (Find.TickManager.TicksGame % 250 == 0)
                    Messages.Message("DeactivatedWarning".Translate(pawn.Label),
                        MessageTypeDefOf.RejectInput);
                __result = false;
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Adds another "layer" to the equipment aiming if they have a
        ///     weapon with a CompActivatableEffect.
        /// </summary>
        public static void DrawEquipmentAimingPostFix(Pawn ___pawn, Thing eq, Vector3 drawLoc,
            float aimAngle)
        {
            var compActivatableEffect = ___pawn.equipment?.Primary?.GetComp<CompActivatableEffect>();
            if (compActivatableEffect?.Graphic == null) return;
            if (compActivatableEffect.CurrentState != CompActivatableEffect.State.Activated) return;

            var num = aimAngle - 90f;
            var flip = false;

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
                num += eq.def.equippedAngleOffset;
            }

            Vector3 offset = Vector3.zero;

            if (eq is ThingWithComps eqComps)
            {
                var weaponComp = eqComps.GetComp<CompOversizedWeapon.CompOversizedWeapon>();
                if (weaponComp != null)
                {
                    if (___pawn.Rotation == Rot4.East)
                        offset = weaponComp.Props.eastOffset;
                    else if (___pawn.Rotation == Rot4.West)
                        offset = weaponComp.Props.westOffset;
                    else if (___pawn.Rotation == Rot4.North)
                        offset = weaponComp.Props.northOffset;
                    else if (___pawn.Rotation == Rot4.South)
                        offset = weaponComp.Props.southOffset;
                    offset += weaponComp.Props.offset;
                }

                if (compActivatableEffect.CompDeflectorIsAnimatingNow)
                {
                    float numMod = compActivatableEffect.CompDeflectorAnimationDeflectionTicks;
                    if (numMod > 0)
                    {
                        if (!flip) num += (numMod + 1) / 2;
                        else num -= (numMod + 1) / 2;
                    }
                }
            }
            num %= 360f;

            var matSingle = compActivatableEffect.Graphic.MatSingle;

            var s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
            var matrix = default(Matrix4x4);
            matrix.SetTRS(drawLoc + offset, Quaternion.AngleAxis(num, Vector3.up), s);
            if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
            else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
        }

        public static IEnumerable<Gizmo> GizmoGetter(CompActivatableEffect compActivatableEffect)
        {
            if (compActivatableEffect.GizmosOnEquip)
            {
                foreach (var current in compActivatableEffect.EquippedGizmos())
                    yield return current;
            }
        }

        public static void GetGizmosPrefix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.equipment?.Primary?.GetComp<CompActivatableEffect>() is CompActivatableEffect compActivatableEffect)
                if (__instance.Faction == Faction.OfPlayer)
                {
                    __result = __result.Concat(GizmoGetter(compActivatableEffect));
                }
                else
                {
                    if (compActivatableEffect.CurrentState == CompActivatableEffect.State.Deactivated)
                        compActivatableEffect.Activate();
                }
        }
    }
}
