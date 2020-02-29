using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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
        private static FieldInfo pawnField_Pawn_HealthTracker;
        private static FieldInfo pawnField_Pawn_EquipmentTracker;
        private static FieldInfo pawnField_PawnRenderer;
        private static FieldInfo statDefField_StatWorker;
        public static MethodInfo infoTextLineFromGear;

        public static Pawn HealthTracker_GetPawn(object instance)
        {
            return (Pawn)pawnField_Pawn_HealthTracker.GetValue(instance);
        }

        public static Pawn EquipmentTracker_GetPawn(object instance)
        {
            return (Pawn)pawnField_Pawn_EquipmentTracker.GetValue(instance);
        }

        public static Pawn PawnRenderer_GetPawn(object instance)
        {
            return (Pawn)pawnField_PawnRenderer.GetValue(instance);
        }

        public static StatDef StatDefField_StatWorker(object instance)
        {
            return (StatDef)statDefField_StatWorker.GetValue(instance);
        }

        static HarmonyPatches()
        {
            //HarmonyInstance.DEBUG = true;

            // Changed by Tad : New Harmony Instance creation required
            var instance = new Harmony("jecstools.chjees.shields");
            //ThingDef
            //            {
            //                Type type = typeof(ThingDef);
            //
            //                MethodInfo patchMethod = type.GetMethod("SpecialDisplayStats");
            //                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_ThingDef_SpecialDisplayStats));
            //
            //                Harmony.Patch(
            //                    patchMethod,
            //                    null,
            //                    new HarmonyMethod(patchCustomMethod));
            //            }
            //            //Pawn
            
            {
                Type type = typeof(Pawn);

                MethodInfo patchMethod = type.GetMethod("Tick");
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_Tick));

                instance.Patch(
                    patchMethod,
                    null,
                    new HarmonyMethod(patchCustomMethod));
            }

            
            //
            //            //PawnGenerator
            {
                Type type = typeof(PawnGenerator);

                MethodInfo patchMethod = type.GetMethod("GenerateGearFor", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_PawnGenerator_GenerateGearFor));

                instance.Patch(
                    patchMethod,
                    null,
                    new HarmonyMethod(patchCustomMethod));
            }

            
            //
            //            //PawnRenderer
            {
                Type type = typeof(PawnRenderer);

                pawnField_PawnRenderer = type.GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

                MethodInfo patchMethod = type.GetMethod("RenderPawnAt", new Type[] { typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool) });
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_PawnRenderer_RenderPawnAt));

                instance.Patch(
                    patchMethod,
                    null,
                    new HarmonyMethod(patchCustomMethod));
            }

            

            //Pawn_HealthTracker
            {
                Type type = typeof(Pawn_HealthTracker);

                pawnField_Pawn_HealthTracker = type.GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

                MethodInfo patchMethod = type.GetMethod("PreApplyDamage");
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_HealthTracker_PreApplyDamage));

                instance.Patch(
                    patchMethod,
                    new HarmonyMethod(patchCustomMethod),
                    null);
            }

            

            //Pawn_EquipmentTracker
            {
                Type type = typeof(Pawn_EquipmentTracker);

                pawnField_Pawn_EquipmentTracker = type.GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);

                MethodInfo patchMethod = type.GetMethod("MakeRoomFor");
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_EquipmentTracker_MakeRoomFor));

                instance.Patch(
                    patchMethod,
                    null,
                    new HarmonyMethod(patchCustomMethod));
            }


            
            //StatWorker //TODO - Needs fixing for 1.0
            {
                Type type = typeof(StatWorker);

                infoTextLineFromGear = type.GetMethod("InfoTextLineFromGear", BindingFlags.NonPublic | BindingFlags.Static);
                statDefField_StatWorker = type.GetField("stat", BindingFlags.NonPublic | BindingFlags.Instance);

                MethodInfo patchMethod = type.GetMethod("GetExplanationUnfinalized"); //StatRequest req, ToStringNumberSense numberSense
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Transpiler_StatWorker_GetExplanationUnfinalized));

                instance.Patch(
                    patchMethod,
                    null,
                    null,
                    new HarmonyMethod(patchCustomMethod));
            }

            

            {
                Type type = typeof(StatWorker);

                MethodInfo patchMethod = type.GetMethod("GetValueUnfinalized"); //StatRequest req, ToStringNumberSense numberSense
                //MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Transpiler_StatWorker_GetValueUnfinalized));
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_StatWorker_GetValueUnfinalized));
                instance.Patch(
                    patchMethod,
                    null,
                    new HarmonyMethod(patchCustomMethod));
            }
        }
//
//        public static void Patch_ThingDef_SpecialDisplayStats(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
//        {
//            if(__instance.HasComp(typeof(CompShield)))
//            {
//                List<StatDrawEntry> result = new List<StatDrawEntry>(__result);
//
//                new StatDrawEntry(StatCategoryDefOf.Apparel, "Covers".Translate(), coveredParts, 100, string.Empty);
//
//                __result = result;
//            }
//        }

        public static void Patch_PawnGenerator_GenerateGearFor(Pawn pawn, ref PawnGenerationRequest request)
        {
            if (pawn != null)
                PawnShieldGenerator.TryGenerateShieldFor(pawn, request);
        }

        public static float StatWorkerInjection_AddShieldValue(Pawn pawn, StatDef stat)
        {
            ThingWithComps shield = pawn.GetShield();
            if (shield != null)
            {
                float value = shield.def.equippedStatOffsets.GetStatOffsetFromList(stat);
                return shield.def.equippedStatOffsets.GetStatOffsetFromList(stat);
            }

            return 0f;
        }

        public static void Patch_StatWorker_GetValueUnfinalized(StatWorker __instance, ref float __result, ref StatRequest req, bool applyPostProcess)
        {
            Pawn pawn = req.Thing as Pawn;
            if(pawn != null)
                __result += StatWorkerInjection_AddShieldValue(pawn, StatDefField_StatWorker(__instance));
        }
//
        public static IEnumerable<CodeInstruction> Transpiler_StatWorker_GetValueUnfinalized(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> instructionInjectionList = new List<CodeInstruction>();

            //Look for the Primary part.
            int desiredPosition = instructionList.FirstIndexOf(instruction => instruction.opcode == OpCodes.Ldfld && instruction.operand == typeof(Pawn).GetField("skills"));

            //Log.Message("#1: desiredPosition is at: " + desiredPosition);

            //Now after it is popped inject our own code.
            {
                //Pawn
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldloc_1;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                //Stat
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldarg_0;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                //Stat
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldfld;
                injectedInstruction.operand = typeof(StatWorker).GetField("stat", BindingFlags.NonPublic | BindingFlags.Instance);
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                //Load: Inject our own function.
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Call;
                injectedInstruction.operand = typeof(HarmonyPatches).GetMethod(nameof(StatWorkerInjection_AddShieldValue));
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                //Load: Value being modifier
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldloc_0;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                //Add both together.
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Add;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                //Store: New value.
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Stloc_0;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }

            if (instructionInjectionList.Count > 0)
                instructionList.InsertRange(desiredPosition - 1, instructionInjectionList);

            return instructionList;
        }

        public static void StatWorkerInjection_BuildShieldString(StringBuilder stringBuilder, Pawn pawn, StatDef stat)
        {
            ThingWithComps shield = pawn.GetShield();
            if(shield != null)
            {
                stringBuilder.AppendLine((string)infoTextLineFromGear.Invoke(null, new object[] { shield, stat }));
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler_StatWorker_GetExplanationUnfinalized(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> instructionInjectionList = new List<CodeInstruction>();

            //Look for the Primary part.
            int desiredPosition = instructionList.FirstIndexOf(instruction => instruction.opcode == OpCodes.Callvirt && instruction.operand == typeof(Pawn_EquipmentTracker).GetProperty("Primary").GetGetMethod());

            //Log.Message("#1: desiredPosition is at: " + desiredPosition);

            //Now go forward two System.Text.StringBuilder::AppendLine() calls.
            MethodInfo appendLineMethod1 = typeof(StringBuilder).GetMethod("AppendLine", new Type[] { typeof(string) });
            MethodInfo appendLineMethod2 = typeof(StringBuilder).GetMethod("AppendLine", new Type[] {});

            int calls = 0;
            for(int i = desiredPosition; i < instructionList.Count; i++)
            {
                CodeInstruction cil = instructionList[i];
                if (cil.opcode == OpCodes.Callvirt && (cil.operand == appendLineMethod1 || cil.operand == appendLineMethod2))
                    calls++;

                if (calls >= 2)
                {
                    desiredPosition = i;
                    break;
                }
            }

            desiredPosition -= 2;

            //Log.Message("#2: desiredPosition is at: " + desiredPosition);

            //Now after it is popped inject our own code.
            {
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldloc_0;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldloc_2;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldarg_0;
                injectedInstruction.operand = null;
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Ldfld;
                injectedInstruction.operand = typeof(StatWorker).GetField("stat", BindingFlags.NonPublic | BindingFlags.Instance);
                instructionInjectionList.Add(injectedInstruction);
            }
            {
                CodeInstruction injectedInstruction = new CodeInstruction(instructionList[desiredPosition]);
                injectedInstruction.opcode = OpCodes.Call;
                injectedInstruction.operand = typeof(HarmonyPatches).GetMethod(nameof(StatWorkerInjection_BuildShieldString));
                instructionInjectionList.Add(injectedInstruction);
            }

            if (instructionInjectionList.Count > 0)
                instructionList.InsertRange(desiredPosition + 1, instructionInjectionList);

            return instructionList;
        }

        public static void Patch_PawnRenderer_RenderPawnAt(PawnRenderer __instance, ref Vector3 drawLoc, ref RotDrawMode bodyDrawType, ref bool headStump)
        {
            Pawn pawn = PawnRenderer_GetPawn(__instance);

            //Render shield.
            if(pawn != null && pawn.GetShield() is ThingWithComps shield)
            {
                Vector3 bodyVector = drawLoc;

                CompShield shieldComp = shield.GetComp<CompShield>();
                bodyVector += shieldComp.ShieldProps.renderProperties.Rot4ToVector3(pawn.Rotation);

                shieldComp.RenderShield(bodyVector, pawn.Rotation, pawn, shield);
            }
        }

        public static void Patch_Pawn_EquipmentTracker_MakeRoomFor(Pawn_EquipmentTracker __instance, ref ThingWithComps eq)
        {
            CompShield shieldComp = eq.TryGetComp<CompShield>();
            if(shieldComp != null)
            {
                //Unequip any existing shield.
                ThingWithComps shield = __instance.GetShield();
                if(shield != null)
                {
                    Pawn pawn = EquipmentTracker_GetPawn(__instance);
                    ThingWithComps thingWithComps;

                    if (__instance.TryDropEquipment(shield, out thingWithComps, pawn.Position, true))
                    {
                        if (thingWithComps != null)
                        {
                            thingWithComps.SetForbidden(false, true);
                        }
                    }
                    else
                    {
                        Log.Error(pawn + " couldn't make room for shield " + eq);
                    }
                }
            }
        }

        public static void Patch_Pawn_Tick(Pawn __instance)
        {
            if (__instance.equipment != null && (__instance.ParentHolder != null && !ThingOwnerUtility.ContentsSuspended(__instance.ParentHolder)))
            {
                //Tick shield.
                ThingWithComps shield = __instance.GetShield();
                if (shield == null)
                    return;

                CompShield shieldComp = shield.GetComp<CompShield>();

                shield.Tick();
            }
        }

        public static bool Patch_Pawn_HealthTracker_PreApplyDamage(Pawn_HealthTracker __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            //Log.Message("Try getting pawn.");

            Pawn pawn = HealthTracker_GetPawn(__instance);

            if (pawn == null)
                return true;

            //Notify of agressor
            DamageInfo violence = new DamageInfo(dinfo);
            violence.SetAmount(0);
            pawn?.mindState.Notify_DamageTaken(violence);

            //Log.Message("Pawn got. " + pawn.Name);

            if (pawn.equipment == null)
                return true;

            //Log.Message("Equipment got.");

            //Try getting equipped shield.
            ThingWithComps shield = pawn.GetShield();
            if (shield == null)
                return true;

            CompShield shieldComp = shield.GetComp<CompShield>();

            SoundDef shieldSound = shieldComp.BlockSound ?? shieldComp.ShieldProps.defaultSound;
            bool discardShield = false;

            //Determine if it is a melee or ranged attack.
            if (shieldComp.ShieldProps.canBlockRanged &&
                (dinfo.Instigator != null &&
                !dinfo.Instigator.Position.AdjacentTo8WayOrInside(pawn.Position)) ||
                dinfo.Def.isExplosive)
            {
                //Ranged
                absorbed = shieldComp.AbsorbDamage(pawn, dinfo, true);
                if(absorbed && shieldSound != null)
                    shieldSound.PlayOneShot(pawn);
                //MoteMaker.ThrowText(dinfo.Instigator.DrawPos, dinfo.Instigator.Map, "Ranged absorbed=" + absorbed);

                if (shieldComp.IsBroken)
                {
                    discardShield = true;
                }
            }
            else if (shieldComp.ShieldProps.canBlockMelee &&
                (dinfo.Instigator != null &&
                dinfo.Instigator.Position.AdjacentTo8WayOrInside(pawn.Position)))
            {
                //Melee
                absorbed = shieldComp.AbsorbDamage(pawn, dinfo, false);
                if (absorbed && shieldSound != null)
                    shieldSound.PlayOneShot(pawn);
                //MoteMaker.ThrowText(dinfo.Instigator.DrawPos, dinfo.Instigator.Map, "Melee absorbed=" + absorbed);

                if (shieldComp.IsBroken)
                {
                    discardShield = true;
                }
            }

            if(shieldComp.ShieldProps.useFatigue && pawn.health.hediffSet.GetFirstHediffOfDef(ShieldHediffDefOf.ShieldFatigue) is Hediff hediff && hediff.Severity >= hediff.def.maxSeverity)
            {
                discardShield = true;
            }

            //Discard shield either from damage or fatigue.
            if (shieldComp.ShieldProps.canBeAutoDiscarded && discardShield)
            {
                ThingWithComps thingWithComps;
                if(pawn.equipment.TryDropEquipment(shield, out thingWithComps, pawn.Position, true))
                {
                    if (thingWithComps != null)
                    {
                        thingWithComps.SetForbidden(false, true);
                    }
                }
                else
                {
                    Log.Error(pawn + " couldn't discard shield " + shield);
                }
            }

            if (absorbed)
                return false;

            return true;
        }
    }
}
