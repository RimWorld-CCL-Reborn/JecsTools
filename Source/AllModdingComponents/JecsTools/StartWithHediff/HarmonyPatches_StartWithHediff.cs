using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_StartWithHediff(Harmony harmony, Type type)
    {
        //Allows for adding additional HediffSets when characters spawn using the StartWithHediff class.
        harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                new[] { typeof(PawnGenerationRequest) }),
            postfix: new HarmonyMethod(type, nameof(Post_GeneratePawn)));
    }
    
    public static void Post_GeneratePawn(Pawn __result)
    {
        var hediffGiverSets = __result?.def?.race?.hediffGiverSets;
        if (hediffGiverSets != null)
        {
            foreach (var hediffGiverSet in hediffGiverSets)
            {
                foreach (var hediffGiver in hediffGiverSet.hediffGivers)
                {
                    if (hediffGiver is HediffGiver_StartWithHediff hediffGiverStartWithHediff)
                    {
                        hediffGiverStartWithHediff.GiveHediff(__result);
                        // TODO: Should this really only use the first found HediffGiver_StartWithHediff?
                        return;
                    }
                }
            }
        }
    }
    
}
