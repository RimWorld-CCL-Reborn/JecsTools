using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace JecsTools
{
    // TODO: This doesn't seem to be used - remove?
    public static partial class HarmonyPatches
    {
        public static void GUIPatches()
        {
            var harmony = new Harmony("jecstools.jecrell.main-gui");
            var type = typeof(HarmonyPatches);

            harmony.Patch(AccessTools.Method(typeof(MoteMaker), nameof(MoteMaker.MakeMoodThoughtBubble)),
                postfix: new HarmonyMethod(type, nameof(ToggleMoodThoughtBubble)));
        }

        public static void ToggleMoodThoughtBubble(ref MoteBubble __result)
        {
            if (!bubblesEnabled)
                __result = null;
        }

        private static int TryGetLocalIndexOfConstructedObject(IEnumerable<CodeInstruction> instructions, Type constructedType, Type[] constructorParams = null)
        {
            var constructor = AccessTools.Constructor(constructedType, constructorParams);
            var localIndex = -1;
            if (constructor == null)
            {
                Log.Message($"Could not reflect constructor for type {constructedType}: {Environment.StackTrace}");
                return localIndex;
            }
            CodeInstruction prevInstruction = null;
            foreach (var inst in instructions)
            {
                if (prevInstruction != null && prevInstruction.opcode == OpCodes.Newobj && constructor.Equals(prevInstruction.operand))
                {
                    if (inst.opcode == OpCodes.Stloc_0)
                    {
                        localIndex = 0;
                    }
                    else if (inst.opcode == OpCodes.Stloc_1)
                    {
                        localIndex = 1;
                    }
                    else if (inst.opcode == OpCodes.Stloc_2)
                    {
                        localIndex = 2;
                    }
                    else if (inst.opcode == OpCodes.Stloc_3)
                    {
                        localIndex = 3;
                    }
                    else if (inst.opcode == OpCodes.Stloc && inst.operand is int)
                    {
                        localIndex = (int)inst.operand;
                    }
                    if (localIndex >= 0)
                        break;
                }
                prevInstruction = inst;
            }
            if (localIndex < 0)
            {
                Log.Message($"Could not determine local index for constructed type {constructedType}: {Environment.StackTrace}");
            }
            return localIndex;
        }

        private static bool bubblesEnabled = true;
        internal static void DrawDebugToolbarButton(WidgetRow widgetRow)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                widgetRow.ToggleableIcon(ref bubblesEnabled, TexButton.quickstartIconTex, "Toggle thought/speech bubbles.", null, null);
            }
        }
    }
}
