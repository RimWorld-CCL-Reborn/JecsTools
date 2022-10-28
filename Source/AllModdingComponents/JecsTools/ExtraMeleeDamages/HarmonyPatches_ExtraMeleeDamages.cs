using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_ExtraMeleeDamages(Harmony harmony, Type type)
    {
        //Applies hediff-based extra damage to melee attacks.
        harmony.Patch(typeof(Verb_MeleeAttackDamage).FindIteratorMethod("DamageInfosToApply"),
            transpiler: new HarmonyMethod(type, nameof(Verb_MeleeAttackDamage_DamageInfosToApply_Transpiler)));
    }
    
        public static IEnumerable<CodeInstruction> Verb_MeleeAttackDamage_DamageInfosToApply_Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator ilGen)
        {
            // Transforms following:
            //  if (tool != null && tool.extraMeleeDamages != null)
            //  {
            //      foreach (ExtraDamage extraMeleeDamage in tool.extraMeleeDamages)
            //          ...
            //  }
            // into:
            //  var extraDamages = DamageInfosToApply_ExtraDamages(this);
            //  if (extraDamages != null)
            //  {
            //      foreach (ExtraDamage extraMeleeDamage in extraDamages)
            //          ...
            //  }
            // Note: We're actually modifying an iterator method, which delegates all of its logic to a compiler-generated
            // IEnumerator class with a convoluted FSM with the primary logic in the MoveNext method.
            // The logic surrounding yields within loops is especially complex, so it's best to just modify what's being
            // looped over; in this case, that's replacing the tool.extraMeleeDamages with our own enumerable
            // (along with adjusting the null check conditionals).

            var fieldof_Verb_tool = AccessTools.Field(typeof(Verb), nameof(Verb.tool));
            var fieldof_Tool_extraMeleeDamages = AccessTools.Field(typeof(Tool), nameof(Tool.extraMeleeDamages));
            var methodof_List_GetEnumerator =
                AccessTools.Method(typeof(List<ExtraDamage>), nameof(IEnumerable.GetEnumerator));
            var instructionList = instructions.AsList();
            var locals = new Locals(method, ilGen);

            var extraDamagesVar = locals.DeclareLocal<List<ExtraDamage>>();

            var verbToolFieldNullCheckIndex = instructionList.FindSequenceIndex(
                locals.IsLdloc,
                instr => instr.Is(OpCodes.Ldfld, fieldof_Verb_tool),
                instr => instr.IsBrfalse());
            var toolExtraDamagesIndex = instructionList.FindIndex(verbToolFieldNullCheckIndex + 3, // after above 3 predicates
                instr => instr.Is(OpCodes.Ldfld, fieldof_Tool_extraMeleeDamages));
            var verbToolFieldIndex = verbToolFieldNullCheckIndex + 1;
            instructionList.SafeReplaceRange(verbToolFieldIndex, toolExtraDamagesIndex + 1 - verbToolFieldIndex, new[]
            {
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(HarmonyPatches), nameof(DamageInfosToApply_ExtraDamages))),
                extraDamagesVar.ToStloc(),
                extraDamagesVar.ToLdloc(),
            });

            var verbToolExtraDamagesEnumeratorIndex = instructionList.FindSequenceIndex(verbToolFieldIndex,
                locals.IsLdloc,
                instr => instr.Is(OpCodes.Ldfld, fieldof_Verb_tool),
                instr => instr.Is(OpCodes.Ldfld, fieldof_Tool_extraMeleeDamages),
                instr => instr.Calls(methodof_List_GetEnumerator));
            instructionList.SafeReplaceRange(verbToolExtraDamagesEnumeratorIndex, 4, new[] // after above 4 predicates
            {
                extraDamagesVar.ToLdloc(),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(List<ExtraDamage>), nameof(IEnumerable.GetEnumerator))),
            });

            return instructionList;
        }

        [ThreadStatic]
        private static Dictionary<(Tool, Pawn), List<ExtraDamage>> extraDamageCache;

        // In the above transpiler, this replaces tool.extraMeleeDamages as the foreach loop enumeration target in
        // Verb_MeleeAttackDamage.DamageInfosToApply.
        // This must return a List<ExtraDamage> rather than IEnumerator<ExtraDamage> since Tool.extraMeleeDamages is a list.
        // Specifically, the compiler-generated code calls List<ExtraDamage>.GetEnumerator(), stores it in a
        // List<ExtraDamage>.Enumerator field in the internal iterator class (necessary for the FSM to work), then explicitly
        // calls List<ExtraDamage>.Enumerator methods/properties in multiple iterator class methods along with an initobj
        // rather than ldnull for clearing it (since List<ExtraDamage>.Enumerator is a struct). Essentially, it would be
        // difficult to replace all this with IEnumerator<ExtraDamage> versions in the above transpiler, we just have this
        // method return the same type as Tool.extraMeleeDamages: List<ExtraDamage>.
        // If either tool.extraMeleeDamages and CasterPawn.GetHediffComp<HediffComp_ExtraMeleeDamages>().Props.ExtraDamages
        // are null, we can simply return the other, since both are lists. However, if both are non-null, we cannot simply
        // return Enumerable.Concat of them both; we need to create a new list that contains both. Since list creation and
        // getting the hediff extra damages are both relatively expensive operations, we utilize a cache.
        // This cache is ThreadStatic to be optimized for single-threaded usage yet safe for multithreaded usage.
        private static List<ExtraDamage> DamageInfosToApply_ExtraDamages(Verb_MeleeAttackDamage verb)
        {
            extraDamageCache ??= new Dictionary<(Tool, Pawn), List<ExtraDamage>>();
            var key = (verb.tool, verb.CasterPawn);
            if (!extraDamageCache.TryGetValue(key, out var extraDamages))
            {
                var toolExtraDamages = key.tool?.extraMeleeDamages;
                var hediffExtraDamages = key.CasterPawn.GetHediffComp<HediffComp_ExtraMeleeDamages>()?.Props?.ExtraDamages;
                if (toolExtraDamages == null)
                    extraDamages = hediffExtraDamages;
                else if (hediffExtraDamages == null)
                    extraDamages = toolExtraDamages;
                else
                {
                    extraDamages = new List<ExtraDamage>(toolExtraDamages.Count + hediffExtraDamages.Count);
                    extraDamages.AddRange(toolExtraDamages);
                    extraDamages.AddRange(hediffExtraDamages);
                }
                DebugMessage($"DamageInfosToApply_ExtraDamages({verb}) => caching for {key}: {extraDamages.Join(ToString)}");
                extraDamageCache[key] = extraDamages;
            }
            return extraDamages;
        }

    
}
