using System;
using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class ChancedGeneEntry
    {
        public string defName;
        public bool xenogene = false;
        public float chance = 100;
    }
    
    public class PawnKindGeneExtension : DefModExtension
    {
        private List<ChancedGeneEntry> genes; // set via reflection

        [Unsaved]
        private HashSet<ChancedGeneEntry> geneSet;

        public HashSet<ChancedGeneEntry> Genes
        {
            get
            {
                // DefModExtension lacks a ResolveReferences hook, so must use lazy initialization in property instead.
                if (geneSet == null && genes != null)
                {
                    geneSet = new HashSet<ChancedGeneEntry>(genes.Count);
                    foreach (var gene in genes)
                        geneSet.Add(gene);
                }
                return geneSet;
            }
        }
        
        public override IEnumerable<string> ConfigErrors()
        {
            if (genes != null && Genes.Count != genes.Count)
                yield return nameof(genes) + " has duplicate genes: " + genes.ToStringSafeEnumerable();
        }
    }
}
