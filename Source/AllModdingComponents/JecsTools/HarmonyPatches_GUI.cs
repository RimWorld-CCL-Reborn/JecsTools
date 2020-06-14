using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace JecsTools
{
    public static partial class HarmonyPatches
    {
        private static bool drawButtonsPatched;
        
        public static void GUIPatches(Harmony harmony)
        {
            // Changed by Tad : New Harmony Instance creation required
            var instance = new Harmony("jecstools.jecrell.main-gui");

            //Allow fortitude to soak damage
            var type = typeof(HarmonyPatches);
            //harmony.Patch(AccessTools.Method(typeof(DebugWindowsOpener), "DrawButtons"), null,
            //    null, new HarmonyMethod(type, nameof(DrawAdditionalButtons)));
            instance.Patch(AccessTools.Method(typeof(MoteMaker), "MakeMoodThoughtBubble"), null,
                new HarmonyMethod(type, nameof(ToggleMoodThoughtBubble)),
                null);
        }

        public static void ToggleMoodThoughtBubble(Pawn pawn, Thought thought, ref MoteBubble __result)
        {
            if (!bubblesEnabled) __result = null;
        }
//
//        public static IEnumerable<CodeInstruction> DrawAdditionalButtons(IEnumerable<CodeInstruction> instructions) {
//            var instructionsArr = instructions.ToArray();
//            var widgetRowIndex = TryGetLocalIndexOfConstructedObject(instructionsArr, typeof(WidgetRow));
//            foreach (var inst in instructionsArr) {
//                if (!drawButtonsPatched && widgetRowIndex >= 0 && inst.opcode == OpCodes.Bne_Un) {
//                    yield return new CodeInstruction(OpCodes.Ldloc, widgetRowIndex);
//                    yield return new CodeInstruction(OpCodes.Call, ((Action<WidgetRow>)HarmonyPatches.DrawDebugToolbarButton).Method);
//                    drawButtonsPatched = true;
//                }
//                yield return inst;
//            }
//        }
        
        private static int TryGetLocalIndexOfConstructedObject(IEnumerable<CodeInstruction> instructions, Type constructedType, Type[] constructorParams = null) { 
            var constructor = AccessTools.Constructor(constructedType, constructorParams);
            int localIndex = -1;
            if (constructor == null) {
                Log.Message($"Could not reflect constructor for type {constructedType}: {Environment.StackTrace}");
                return localIndex;
            }
            CodeInstruction prevInstruction = null;
            foreach (var inst in instructions) {
                if (prevInstruction != null && prevInstruction.opcode == OpCodes.Newobj && constructor.Equals(prevInstruction.operand)) {
                    if (inst.opcode == OpCodes.Stloc_0) {
                        localIndex = 0;
                    } else if (inst.opcode == OpCodes.Stloc_1) {
                        localIndex = 1;
                    } else if (inst.opcode == OpCodes.Stloc_2) {
                        localIndex = 2;
                    } else if (inst.opcode == OpCodes.Stloc_3) {
                        localIndex = 3;
                    } else if (inst.opcode == OpCodes.Stloc && inst.operand is int) {
                        localIndex = (int)inst.operand;
                    }
                    if (localIndex >= 0) break;
                }
                prevInstruction = inst;
            }
            if (localIndex < 0) {
                Log.Message($"Could not determine local index for constructed type {constructedType}: {Environment.StackTrace}");
            }
            return localIndex;
        }

        private static bool bubblesEnabled = true;
        internal static void DrawDebugToolbarButton(WidgetRow widgetRow) {
            if (Current.ProgramState == ProgramState.Playing)
            {
                widgetRow.ToggleableIcon(ref bubblesEnabled, TexButton.quickstartIconTex, "Toggle thought/speech bubbles.", null, null);
            }
        }


        
    }
}