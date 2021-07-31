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

            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos)),
                postfix: new HarmonyMethod(type, nameof(GetGizmosPostfix)));

            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.DrawEquipmentAiming)),
                postfix: new HarmonyMethod(type, nameof(DrawEquipmentAimingPostFix)));

            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.TryStartCastOn),
                    new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(type, nameof(TryStartCastOnPrefix)));

            harmony.Patch(AccessTools.PropertySetter(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted)),
                postfix: new HarmonyMethod(type, nameof(set_DraftedPostFix)));

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExitMap)),
                prefix: new HarmonyMethod(type, nameof(ExitMap_PreFix)));
        }

        public static void ExitMap_PreFix(Pawn __instance)
        {
            if (__instance.equipment?.Primary?.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect &&
                compActivatableEffect.CurrentState == CompActivatableEffect.State.Activated)
                compActivatableEffect.TryDeactivate();
        }

        public static void set_DraftedPostFix(Pawn_DraftController __instance, bool value)
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
            if (__instance.caster is Pawn pawn && pawn.Spawned && pawn.equipment?.Primary is ThingWithComps thingWithComps &&
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
                if (Find.TickManager.TicksGame % GenTicks.TickRareInterval == 0)
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
            if (compActivatableEffect?.Graphic == null ||
                compActivatableEffect.CurrentState != CompActivatableEffect.State.Activated)
                return;

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

            var offset = Vector3.zero;

            var weaponComp = compActivatableEffect.GetOversizedWeapon;
            if (weaponComp != null)
            {
                offset = OffsetFromRotation(weaponComp.Props, ___pawn.Rotation);
            }

            if (compActivatableEffect.CompDeflectorIsAnimatingNow)
            {
                float animationTicks = compActivatableEffect.CompDeflectorAnimationDeflectionTicks;
                if (animationTicks > 0)
                {
                    if (flip)
                        angle -= (animationTicks + 1) / 2;
                    else
                        angle += (animationTicks + 1) / 2;
                }
            }

            angle %= 360f; // copied vanilla code

            var matSingle = compActivatableEffect.Graphic.MatSingle;

            var s = new Vector3(eq.def.graphicData.drawSize.x, 1f, eq.def.graphicData.drawSize.y);
            var matrix = Matrix4x4.TRS(drawLoc + offset, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(flip ? MeshPool.plane10Flip : MeshPool.plane10, matrix, matSingle, 0);
        }

        // Workaround for mod lists that contain other mods with an outdated copy of CompOversizedWeapon that's loaded before ours:
        // avoid calling new code that's in our version.
        private static Vector3 OffsetFromRotation(CompOversizedWeapon.CompProperties_OversizedWeapon weaponComp, Rot4 rotation)
        {
            if (rotation == Rot4.North)
                return weaponComp.northOffset;
            else if (rotation == Rot4.East)
                return weaponComp.eastOffset;
            else if (rotation == Rot4.West)
                return weaponComp.westOffset;
            else
                return weaponComp.southOffset;
        }

        public static void GetGizmosPostfix(Pawn_EquipmentTracker __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.Primary?.GetCompActivatableEffect() is CompActivatableEffect compActivatableEffect)
                if (__instance.pawn.Faction == Faction.OfPlayer)
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
