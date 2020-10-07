using System;
using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    [Obsolete("Use vanilla apparel.gender field in RimWorld 1.1+")]
    public class SwapCondition
    {
        public Gender swapWhenGender = Gender.None;
        public ThingDef swapTo = null;
    }

    [Obsolete("Use vanilla apparel.gender and apparel.layers fields and custom ApparelLayerDef")]
    public class ApparelExtension : DefModExtension
    {
        [Obsolete("Use vanilla apparel.layers field and custom ApparelLayerDef in RimWorld b19+")]
        private List<string> coverage;

        [Unsaved]
        private HashSet<string> coverageSet;

        public HashSet<string> Coverage
        {
            get
            {
                // DefModExtension lacks a ResolveReferences hook, so must use lazy initialization in property instead.
                if (coverageSet == null && coverage != null)
                {
                    coverageSet = new HashSet<string>(coverage.Count);
                    foreach (var item in coverage)
                        coverageSet.Add(item.ToLowerInvariant());
                }
                return coverageSet;
            }
        }

        [Obsolete("Use vanilla apparel.gender field in RimWorld 1.1+")]
        public SwapCondition swapCondition = new SwapCondition();

        public override IEnumerable<string> ConfigErrors()
        {
            if (coverage != null && Coverage.Count != coverage.Count)
                yield return nameof(coverage) + " has duplicate items: " + coverage.ToStringSafeEnumerable();
        }
    }
}
