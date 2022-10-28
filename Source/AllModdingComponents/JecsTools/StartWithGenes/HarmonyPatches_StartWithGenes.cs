using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_StartWithGenes(Harmony harmony, Type type)
    {
        //Allows for adding additional genes when characters spawn using the StartWithHediff class.
        harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                new[] { typeof(PawnGenerationRequest) }),
            postfix: new HarmonyMethod(type, nameof(Post_GeneratePawn_Genes)));
    }
    
    public static void Post_GeneratePawn_Genes(Pawn __result)
    {
        if (!ModsConfig.BiotechActive)
            return;

        if (__result?.kindDef?.modExtensions is { } extensions)
        {
            foreach (var extension in extensions)
            {
                if (extension is PawnKindGeneExtension newGenes)
                {
                    foreach (var gene in newGenes.Genes.Where(gene => Rand.Range(min: 0, max: 100) < gene.chance))
                    {
                        __result.genes.AddGene(DefDatabase<GeneDef>.GetNamed(gene.defName), false);
                    }
                }
            }
        }
        
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
