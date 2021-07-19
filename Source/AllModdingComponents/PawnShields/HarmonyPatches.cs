using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PawnShields
{
    /// <summary>
    /// Harmony patches, these aren't pretty.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("jecstools.chjees.shields");
            var type = typeof(HarmonyPatches);

            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GenerateGearFor"),
                postfix: new HarmonyMethod(type, nameof(Patch_PawnGenerator_GenerateGearFor)));

            harmony.Patch(AccessTools.Method(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt),
                    new[] { typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool) }),
                postfix: new HarmonyMethod(type, nameof(Patch_PawnRenderer_RenderPawnAt)));

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.DropAndForbidEverything)),
                postfix: new HarmonyMethod(type, nameof(Patch_Pawn_DropAndForbidEverything)));

            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.CheckForStateChange)),
                postfix: new HarmonyMethod(type, nameof(Patch_Pawn_HealthTracker_CheckForStateChance)));
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage)),
                prefix: new HarmonyMethod(type, nameof(Patch_Pawn_HealthTracker_PreApplyDamage)));

            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.MakeRoomFor)),
                postfix: new HarmonyMethod(type, nameof(Patch_Pawn_EquipmentTracker_MakeRoomFor)));

            harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
                transpiler: new HarmonyMethod(type, nameof(Transpiler_StatWorker_GetValueUnfinalized)));
            harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized)),
                transpiler: new HarmonyMethod(type, nameof(Transpiler_StatWorker_GetExplanationUnfinalized)));
        }

        public static void Patch_PawnGenerator_GenerateGearFor(Pawn pawn, ref PawnGenerationRequest request)
        {
            PawnShieldGenerator.TryGenerateShieldFor(pawn, request);
        }

        public static IEnumerable<CodeInstruction> Transpiler_StatWorker_GetValueUnfinalized(IEnumerable<CodeInstruction> instructions,
            MethodBase method, ILGenerator ilGen)
        {
            // Transforms following:
            //  if (pawn.equipment != null && pawn.equipment.Primary != null)
            //  {
            //      result += StatOffsetFromGear(pawn.equipment.Primary, stat);
            //  }
            // into:
            //  if (pawn.equipment != null)
            //  {
            //      if (pawn.equipment.Primary != null)
            //          result += StatOffsetFromGear(pawn.equipment.Primary, stat);
            //      StatWorkerInjection_AddShieldValue(ref result, pawn.equipment, stat);
            //  }

            var instructionList = instructions.AsList();
            var locals = new Locals(method, ilGen);

            var pawnEquipmentIndex = instructionList.FindSequenceIndex(
                instruction => locals.IsLdloc(instruction),
                instruction => instruction.LoadsField(pawnEquipmentField),
                instruction => instruction.IsBrfalse());
            var targetLabel = (Label)instructionList[pawnEquipmentIndex + 2].operand;

            var resultStoreIndex = instructionList.FindIndex(pawnEquipmentIndex + 3,
                instruction => locals.IsStloc(instruction, out var local) && local.LocalType == typeof(float));

            var insertionIndex = instructionList.FindIndex(pawnEquipmentIndex + 3,
                instruction => instruction.labels.Contains(targetLabel));

            var labelsToTransfer = instructionList.GetRange(pawnEquipmentIndex + 3, insertionIndex - (pawnEquipmentIndex + 3))
                .Where(instruction => instruction.operand is Label)
                .Select(instruction => (Label)instruction.operand);
            instructionList.SafeInsertRange(insertionIndex, new[]
            {
                locals.FromStloc(instructionList[resultStoreIndex]).ToLdloca(), // &result
                instructionList[pawnEquipmentIndex].Clone(), // pawn...
                instructionList[pawnEquipmentIndex + 1].Clone(), // ...equipment
                new CodeInstruction(OpCodes.Ldarg_0), // this...
                new CodeInstruction(OpCodes.Ldfld, statWorkerStatField), // ...stat
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), nameof(StatWorkerInjection_AddShieldValue))),
            }, labelsToTransfer);

            return instructionList;
        }

        private static void StatWorkerInjection_AddShieldValue(ref float result, Pawn_EquipmentTracker equipment, StatDef stat)
        {
            var shield = equipment.GetShield();
            if (shield != null)
            {
                result += shield.def.equippedStatOffsets.GetStatOffsetFromList(stat);
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler_StatWorker_GetExplanationUnfinalized(IEnumerable<CodeInstruction> instructions,
            MethodBase method, ILGenerator ilGen)
        {
            // Transforms following:
            //  if (pawn.equipment != null && pawn.equipment.Primary != null && GearAffectsStat(pawn.equipment.Primary.def, stat))
            //  {
            //      stringBuilder.AppendLine(InfoTextLineFromGear(pawn.equipment.Primary, stat));
            //  }
            // into:
            //  if (pawn.equipment != null)
            //  {
            //      if (pawn.equipment.Primary != null && GearAffectsStat(pawn.equipment.Primary.def, stat))
            //          stringBuilder.AppendLine(InfoTextLineFromGear(pawn.equipment.Primary, stat));
            //      StatWorkerInjection_BuildShieldString(stringBuilder, pawn.equipment, stat);
            //  }

            var instructionList = instructions.AsList();
            var locals = new Locals(method, ilGen);

            var pawnEquipmentIndex = instructionList.FindSequenceIndex(
                instruction => locals.IsLdloc(instruction),
                instruction => instruction.LoadsField(pawnEquipmentField),
                instruction => instruction.IsBrfalse());
            var targetLabel = (Label)instructionList[pawnEquipmentIndex + 2].operand;

            var stringBuilderIndex = instructionList.FindIndex(pawnEquipmentIndex + 3,
                instruction => locals.IsLdloc(instruction, out var local) && local.LocalType == typeof(StringBuilder));

            var insertionIndex = instructionList.FindIndex(pawnEquipmentIndex + 3,
                instruction => instruction.labels.Contains(targetLabel));

            var labelsToTransfer = instructionList.GetRange(pawnEquipmentIndex + 3, insertionIndex - (pawnEquipmentIndex + 3))
                .Where(instruction => instruction.operand is Label)
                .Select(instruction => (Label)instruction.operand);
            instructionList.SafeInsertRange(insertionIndex, new[]
            {
                instructionList[stringBuilderIndex].Clone(), // stringBuilder
                instructionList[pawnEquipmentIndex].Clone(), // pawn...
                instructionList[pawnEquipmentIndex + 1].Clone(), // ...equipment
                new CodeInstruction(OpCodes.Ldarg_0), // this...
                new CodeInstruction(OpCodes.Ldfld, statWorkerStatField), // ...stat
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HarmonyPatches), nameof(StatWorkerInjection_BuildShieldString))),
            }, labelsToTransfer);

            return instructionList;
        }

        private static void StatWorkerInjection_BuildShieldString(StringBuilder stringBuilder, Pawn_EquipmentTracker equipment, StatDef stat)
        {
            var shield = equipment.GetShield();
            if (shield != null && GearAffectsStats(shield.def, stat))
            {
                stringBuilder.AppendLine(InfoTextLineFromGear(shield, stat));
            }
        }

        private static readonly FieldInfo pawnEquipmentField = AccessTools.Field(typeof(Pawn), nameof(Pawn.equipment));
        private static readonly FieldInfo statWorkerStatField = AccessTools.Field(typeof(StatWorker), "stat");
        private static readonly Func<ThingDef, StatDef, bool> GearAffectsStats =
            (Func<ThingDef, StatDef, bool>)AccessTools.Method(typeof(StatWorker), "GearAffectsStat")
                .CreateDelegate(typeof(Func<ThingDef, StatDef, bool>));
        private static readonly Func<Thing, StatDef, string> InfoTextLineFromGear =
            (Func<Thing, StatDef, string>)AccessTools.Method(typeof(StatWorker), "InfoTextLineFromGear")
                .CreateDelegate(typeof(Func<Thing, StatDef, string>));

        public static void Patch_PawnRenderer_RenderPawnAt(Pawn ___pawn, ref Vector3 drawLoc)
        {
            //Render shield.
            if (___pawn?.GetShield() is ThingWithComps shield)
            {
                var bodyVector = drawLoc;

                var shieldComp = shield.GetCompShield();
                bodyVector += shieldComp.ShieldProps.renderProperties.Rot4ToVector3(___pawn.Rotation);

                shieldComp.RenderShield(bodyVector, ___pawn.Rotation, ___pawn, shield);
            }
        }

        public static void Patch_Pawn_EquipmentTracker_MakeRoomFor(Pawn_EquipmentTracker __instance, Pawn ___pawn, ThingWithComps eq)
        {
            var shieldComp = eq.GetCompShield();
            if (shieldComp != null)
            {
                //Unequip any existing shield.
                var shield = __instance.GetShield();
                if (shield != null)
                {
                    if (__instance.TryDropEquipment(shield, out var thingWithComps, ___pawn.Position, true))
                    {
                        thingWithComps?.SetForbidden(false, true);
                    }
                    else
                    {
                        Log.Error(___pawn + " couldn't make room for shield " + eq);
                    }
                }
            }
        }

        public static void Patch_Pawn_DropAndForbidEverything(Pawn __instance)
        {
            if (__instance.InContainerEnclosed && __instance.GetShield() is ThingWithComps shield)
            {
                __instance.equipment.TryTransferEquipmentToContainer(shield, __instance.holdingOwner);
            }
        }

        public static void Patch_Pawn_HealthTracker_CheckForStateChance(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            if (!__instance.Downed && !__instance.capacities.CapableOf(PawnCapacityDefOf.Manipulation) && ___pawn.GetShield() is ThingWithComps shield)
            {
                if (___pawn.kindDef.destroyGearOnDrop)
                {
                    ___pawn.equipment.DestroyEquipment(shield);
                }
                else if (___pawn.InContainerEnclosed)
                {
                    ___pawn.equipment.TryTransferEquipmentToContainer(shield, ___pawn.holdingOwner);
                }
                else if (___pawn.SpawnedOrAnyParentSpawned)
                {
                    ___pawn.equipment.TryDropEquipment(shield, out var _, ___pawn.PositionHeld);
                }
                else if (___pawn.IsCaravanMember())
                {
                    ___pawn.equipment.Remove(shield);
                    if (!___pawn.inventory.innerContainer.TryAdd(shield))
                    {
                        shield.Destroy();
                    }
                }
                else
                {
                    ___pawn.equipment.DestroyEquipment(shield);
                }
            }
        }

        public static bool Patch_Pawn_HealthTracker_PreApplyDamage(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (___pawn == null)
                return true;

            //Notify of agressor
            var violence = new DamageInfo(dinfo);
            violence.SetAmount(0);
            ___pawn.mindState.Notify_DamageTaken(violence);

            //Try getting equipped shield.
            var shield = ___pawn.GetShield();
            if (shield == null)
                return true;

            var shieldComp = shield.GetCompShield();

            var shieldSound = shieldComp.BlockSound ?? shieldComp.ShieldProps.defaultSound;
            var discardShield = false;

            //Determine if it is a melee or ranged attack.
            if (shieldComp.ShieldProps.canBlockRanged &&
                dinfo.Instigator != null &&
                !dinfo.Instigator.Position.AdjacentTo8WayOrInside(___pawn.Position) ||
                dinfo.Def.isExplosive)
            {
                //Ranged
                absorbed = shieldComp.AbsorbDamage(___pawn, dinfo, true);
                if (absorbed)
                    shieldSound?.PlayOneShot(___pawn);
                //MoteMaker.ThrowText(dinfo.Instigator.DrawPos, dinfo.Instigator.Map, "Ranged absorbed=" + absorbed);

                if (shieldComp.IsBroken)
                {
                    discardShield = true;
                }
            }
            else if (shieldComp.ShieldProps.canBlockMelee &&
                dinfo.Instigator != null &&
                dinfo.Instigator.Position.AdjacentTo8WayOrInside(___pawn.Position))
            {
                //Melee
                absorbed = shieldComp.AbsorbDamage(___pawn, dinfo, false);
                if (absorbed)
                    shieldSound?.PlayOneShot(___pawn);
                //MoteMaker.ThrowText(dinfo.Instigator.DrawPos, dinfo.Instigator.Map, "Melee absorbed=" + absorbed);

                if (shieldComp.IsBroken)
                {
                    discardShield = true;
                }
            }

            if (shieldComp.ShieldProps.useFatigue && ___pawn.health.hediffSet.GetFirstHediffOfDef(ShieldHediffDefOf.ShieldFatigue) is Hediff hediff &&
                hediff.Severity >= hediff.def.maxSeverity)
            {
                discardShield = true;
            }

            //Discard shield either from damage or fatigue.
            if (shieldComp.ShieldProps.canBeAutoDiscarded && discardShield)
            {
                if (___pawn.equipment.TryDropEquipment(shield, out var thingWithComps, ___pawn.Position, true))
                {
                    thingWithComps?.SetForbidden(false, true);
                }
                else
                {
                    Log.Error(___pawn + " couldn't discard shield " + shield);
                }
            }

            return !absorbed;
        }
    }
}
