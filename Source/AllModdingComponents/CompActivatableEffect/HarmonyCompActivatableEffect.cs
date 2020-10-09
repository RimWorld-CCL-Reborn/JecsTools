using System;
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
            var type = typeof(HarmonyCompActivatableEffect);

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                postfix: new HarmonyMethod(type, nameof(GetGizmosPostfix)));

            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming)),
                postfix: new HarmonyMethod(type, nameof(DrawEquipmentAimingPostFix)));

            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.TryStartCastOn),
                    new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(type, nameof(TryStartCastOnPrefix)));

            harmony.Patch(AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)),
                postfix: new HarmonyMethod(type, nameof(set_DraftedPostFix)));

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExitMap)),
                prefix: new HarmonyMethod(type, nameof(ExitMap_PreFix)));

            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentRemoved)),
                postfix: new HarmonyMethod(type, nameof(Notify_EquipmentRemoved_PostFix)));
        }

        //=================================== COMPACTIVATABLE

        // Verse.Pawn_EquipmentTracker
        public static void Notify_EquipmentRemoved_PostFix(ThingWithComps eq)
        {
            if (eq.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect &&
                compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                compActivatableEffect.TryDeactivate();
        }

        public static void ExitMap_PreFix(Pawn __instance)
        {
            if (__instance.equipment?.Primary?.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect &&
                compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                compActivatableEffect.TryDeactivate();
        }

#pragma warning disable IDE1006 // Naming Styles
        public static void set_DraftedPostFix(Pawn_DraftController __instance, bool value)
#pragma warning restore IDE1006 // Naming Styles
        {
            if (__instance.pawn?.equipment?.Primary?.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect)
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
                thingWithComps.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect)
            {
                // EquipmentSource throws errors when checked while casting abilities with a weapon equipped.
                // to avoid this error preventing our code from executing, we do a try/catch.
                // TODO: Is this still the case?
                try
                {
                    if (__instance.EquipmentSource != thingWithComps)
                        return true;
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Verb.TryStartCastOn EquipmentSource threw exception: " + ex,
                        __instance.GetUniqueLoadID().GetHashCode());
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
            var compActivatableEffect = ___pawn.equipment?.Primary?.GetCompActivatableEffect();
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

            var weaponComp = compActivatableEffect.GetOversizedWeapon;
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

            num %= 360f;

            var matSingle = compActivatableEffect.Graphic.MatSingle;

            var s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
            var matrix = default(Matrix4x4);
            matrix.SetTRS(drawLoc + offset, Quaternion.AngleAxis(num, Vector3.up), s);
            if (!flip) Graphics.DrawMesh(MeshPool.plane10, matrix, matSingle, 0);
            else Graphics.DrawMesh(MeshPool.plane10Flip, matrix, matSingle, 0);
        }

        public static void GetGizmosPostfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.equipment?.Primary?.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect)
                if (__instance.Faction == Faction.OfPlayer)
                {
                    if (compActivatableEffect.GizmosOnEquip)
                        __result = __result.Concat(compActivatableEffect.EquippedGizmos());
                }
                else
                {
                    if (compActivatableEffect.CurrentState == CompActivatableEffect.State.Deactivated)
                        compActivatableEffect.Activate();
                }
        }
    }
}
