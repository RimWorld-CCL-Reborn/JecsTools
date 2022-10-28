using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_ApparelExtension(Harmony harmony, Type type)
    {
        //Checks apparel that uses the ApparelExtension
        harmony.Patch(AccessTools.Method(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether)),
            postfix: new HarmonyMethod(type, nameof(Post_CanWearTogether)));

        //Handles cases where gendered apparel swaps out for individual genders.
        harmony.Patch(AccessTools.Method(typeof(PawnApparelGenerator), nameof(PawnApparelGenerator.GenerateStartingApparelFor)),
            postfix: new HarmonyMethod(type, nameof(GenerateStartingApparelFor_PostFix)));
    }
    
    
        //PawnApparelGenerator
        public static void GenerateStartingApparelFor_PostFix(Pawn pawn)
        {
            var allWornApparel = pawn.apparel?.WornApparel;
            if (allWornApparel.NullOrEmpty())
                return;
            List<(Apparel, Apparel)> swapEntries = null;
            foreach (var wornApparel in allWornApparel)
            {
                if (wornApparel.def?.GetApparelExtension()?.swapCondition is SwapCondition sc &&
                    sc.swapWhenGender is Gender gen &&
                    gen != Gender.None && gen == pawn.gender)
                {
                    var swapApparel = (Apparel)ThingMaker.MakeThing(sc.swapTo, wornApparel.Stuff);
                    // Avoid modifying WornApparel during its enumeration by doing the swaps afterwards.
                    swapEntries ??= new List<(Apparel worn, Apparel swap)>();
                    swapEntries.Add((wornApparel, swapApparel));
                }
            }
            if (swapEntries != null)
            {
                foreach (var (wornApparel, swapApparel) in swapEntries)
                {
                    PawnGenerator.PostProcessGeneratedGear(swapApparel, pawn);
                    if (ApparelUtility.HasPartsToWear(pawn, swapApparel.def))
                    {
                        pawn.apparel.Wear(swapApparel, false);
                        DebugMessage($"apparel generation for {pawn}: swapped from {wornApparel} to {swapApparel}");
                    }
                    wornApparel.Destroy();
                    DebugMessage($"apparel generation for {pawn}: destroyed old {wornApparel}");
                }
            }
        }

        /// <summary>
        /// Using the new ApparelExtension, we can have a string based apparel check.
        /// </summary>
        public static void Post_CanWearTogether(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            static HashSet<string> GetCoverage(ThingDef thingDef)
            {
                var coverage = thingDef.GetApparelExtension()?.Coverage;
                return coverage == null || coverage.Count == 0 ? null : coverage;
            }

            if (A == null || B == null || body == null || __result == true)
                return;
            var coverageA = GetCoverage(A);
            var coverageB = GetCoverage(B);
            if (coverageA != null && coverageB != null)
            {
                foreach (var coverageItem in coverageB)
                {
                    if (coverageA.Contains(coverageItem))
                    {
                        __result = false;
                        break;
                    }
                }
            }
            else if ((coverageA != null && coverageB == null) || (coverageA == null && coverageB != null))
            {
                __result = true;
            }
        }
}
