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
        //Allows for adding additional genes when characters spawn with the PawnKindGeneExtension modExtension
        harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                new[] { typeof(PawnGenerationRequest) }),
            postfix: new HarmonyMethod(type, nameof(Post_GeneratePawn_Genes)));
    }
    
    public static void Post_GeneratePawn_Genes(Pawn __result)
    {
        if (!ModsConfig.BiotechActive)
            return;

        var pawnKindGeneExtension = __result?.kindDef.GetPawnKindGeneExtension();
        if (pawnKindGeneExtension == null)
            return;
        
        foreach (var gene in pawnKindGeneExtension.Genes.Where(gene => Rand.Range(min: 0, max: 100) < gene.chance))
        {
            __result.genes.AddGene(DefDatabase<GeneDef>.GetNamed(gene.defName), gene.xenogene);
        }
    }
    
}
