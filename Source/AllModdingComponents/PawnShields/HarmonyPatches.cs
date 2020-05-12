using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
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
        private static readonly Func<Thing, StatDef, string> infoTextLineFromGear;

        static HarmonyPatches()
        {
            // Changed by Tad : New Harmony Instance creation required
            var instance = new Harmony("jecstools.chjees.shields");

            ////ThingDef
            //{
            //    Type type = typeof(ThingDef);

            //    MethodInfo patchMethod = type.GetMethod("SpecialDisplayStats");
            //    MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_ThingDef_SpecialDisplayStats));

            //    instance.Patch(patchMethod, postfix: new HarmonyMethod(patchCustomMethod));
            //}

            //Pawn
            {
                Type type = typeof(Pawn);

                MethodInfo patchMethod = type.GetMethod("Tick");
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_Tick));

                instance.Patch(patchMethod, postfix: new HarmonyMethod(patchCustomMethod));
            }

            //PawnGenerator
            {
                Type type = typeof(PawnGenerator);

                MethodInfo patchMethod = type.GetMethod("GenerateGearFor", BindingFlags.NonPublic | BindingFlags.Static);
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_PawnGenerator_GenerateGearFor));

                instance.Patch(patchMethod, postfix: new HarmonyMethod(patchCustomMethod));
            }

            //PawnRenderer
            {
                Type type = typeof(PawnRenderer);

                MethodInfo patchMethod = type.GetMethod("RenderPawnAt", new Type[] { typeof(Vector3), typeof(RotDrawMode), typeof(bool), typeof(bool) });
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_PawnRenderer_RenderPawnAt));

                instance.Patch(patchMethod, postfix: new HarmonyMethod(patchCustomMethod));
            }

            //Pawn_HealthTracker
            {
                Type type = typeof(Pawn_HealthTracker);

                MethodInfo patchMethod = type.GetMethod("PreApplyDamage");
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_HealthTracker_PreApplyDamage));

                instance.Patch(patchMethod, prefix: new HarmonyMethod(patchCustomMethod));
            }

            //Pawn_EquipmentTracker
            {
                Type type = typeof(Pawn_EquipmentTracker);

                MethodInfo patchMethod = type.GetMethod("MakeRoomFor");
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_Pawn_EquipmentTracker_MakeRoomFor));

                instance.Patch(patchMethod, postfix: new HarmonyMethod(patchCustomMethod));
            }


            
            //StatWorker //TODO - Needs fixing for 1.0
            {
                Type type = typeof(StatWorker);

                infoTextLineFromGear = (Func<Thing, StatDef, string>) AccessTools.Method(type, "InfoTextLineFromGear")
                    .CreateDelegate(typeof(Func<Thing, StatDef, string>));

                MethodInfo patchMethod = type.GetMethod("GetExplanationUnfinalized"); //StatRequest req, ToStringNumberSense numberSense
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Transpiler_StatWorker_GetExplanationUnfinalized));

                instance.Patch(patchMethod, transpiler: new HarmonyMethod(patchCustomMethod));
            }

            {
                Type type = typeof(StatWorker);

                MethodInfo patchMethod = type.GetMethod("GetValueUnfinalized"); //StatRequest req, ToStringNumberSense numberSense
                //MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Transpiler_StatWorker_GetValueUnfinalized));
                MethodInfo patchCustomMethod = typeof(HarmonyPatches).GetMethod(nameof(Patch_StatWorker_GetValueUnfinalized));
                instance.Patch(patchMethod, postfix: new HarmonyMethod(patchCustomMethod));
            }
        }

//        public static void Patch_ThingDef_SpecialDisplayStats(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
//        {
//            if (__instance.HasComp(typeof(CompShield)))
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
                return shield.def.equippedStatOffsets.GetStatOffsetFromList(stat);
            }
            return 0f;
        }

        public static void Patch_StatWorker_GetValueUnfinalized(StatDef ___stat, ref float __result, ref StatRequest req)
        {
            if (req.Thing is Pawn pawn)
                __result += StatWorkerInjection_AddShieldValue(pawn, ___stat);
        }

        public static IEnumerable<CodeInstruction> Transpiler_StatWorker_GetValueUnfinalized(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> instructionInjectionList = new List<CodeInstruction>();

            //Look for the Primary part.
            int desiredPosition = instructionList.FirstIndexOf(instruction => instruction.opcode == OpCodes.Ldfld &&
                instruction.operand as FieldInfo == typeof(Pawn).GetField("skills"));

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
            if (shield != null)
            {
                stringBuilder.AppendLine(infoTextLineFromGear(shield, stat));
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler_StatWorker_GetExplanationUnfinalized(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> instructionInjectionList = new List<CodeInstruction>();

            //Look for the Primary part.
            int desiredPosition = instructionList.FirstIndexOf(instruction => instruction.opcode == OpCodes.Callvirt &&
                instruction.operand as MethodInfo == typeof(Pawn_EquipmentTracker).GetProperty("Primary").GetGetMethod());

            //Log.Message("#1: desiredPosition is at: " + desiredPosition);

            //Now go forward two System.Text.StringBuilder::AppendLine() calls.
            MethodInfo appendLineMethod1 = typeof(StringBuilder).GetMethod("AppendLine", new Type[] { typeof(string) });
            MethodInfo appendLineMethod2 = typeof(StringBuilder).GetMethod("AppendLine", Type.EmptyTypes);

            int calls = 0;
            for (int i = desiredPosition; i < instructionList.Count; i++)
            {
                CodeInstruction cil = instructionList[i];
                if (cil.opcode == OpCodes.Callvirt && (cil.operand as MethodInfo == appendLineMethod1 || cil.operand as MethodInfo == appendLineMethod2))
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

        public static void Patch_PawnRenderer_RenderPawnAt(Pawn ___pawn, ref Vector3 drawLoc)
        {
            //Render shield.
            if (___pawn?.GetShield() is ThingWithComps shield)
            {
                Vector3 bodyVector = drawLoc;

                CompShield shieldComp = shield.GetComp<CompShield>();
                bodyVector += shieldComp.ShieldProps.renderProperties.Rot4ToVector3(___pawn.Rotation);

                shieldComp.RenderShield(bodyVector, ___pawn.Rotation, ___pawn, shield);
            }
        }

        public static void Patch_Pawn_EquipmentTracker_MakeRoomFor(Pawn_EquipmentTracker __instance, Pawn ___pawn, ThingWithComps eq)
        {
            CompShield shieldComp = eq.GetComp<CompShield>();
            if (shieldComp != null)
            {
                //Unequip any existing shield.
                ThingWithComps shield = __instance.GetShield();
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

        public static void Patch_Pawn_Tick(Pawn __instance)
        {
            if (__instance.equipment != null && __instance.ParentHolder != null && !ThingOwnerUtility.ContentsSuspended(__instance.ParentHolder))
            {
                //Tick shield.
                ThingWithComps shield = __instance.GetShield();
                shield?.Tick();
            }
        }

        public static bool Patch_Pawn_HealthTracker_PreApplyDamage(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            if (___pawn == null)
                return true;

            //Notify of agressor
            DamageInfo violence = new DamageInfo(dinfo);
            violence.SetAmount(0);
            ___pawn.mindState.Notify_DamageTaken(violence);

            //Try getting equipped shield.
            ThingWithComps shield = ___pawn.GetShield();
            if (shield == null)
                return true;

            CompShield shieldComp = shield.GetComp<CompShield>();

            SoundDef shieldSound = shieldComp.BlockSound ?? shieldComp.ShieldProps.defaultSound;
            bool discardShield = false;

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

            if (shieldComp.ShieldProps.useFatigue && ___pawn.health.hediffSet.GetFirstHediffOfDef(ShieldHediffDefOf.ShieldFatigue) is Hediff hediff && hediff.Severity >= hediff.def.maxSeverity)
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

            if (absorbed)
                return false;

            return true;
        }
    }
}
