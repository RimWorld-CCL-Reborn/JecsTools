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
        public List<string> coverage;

        public HashSet<string> Coverage { get; private set; }

        [Obsolete("Use vanilla apparel.gender field in RimWorld 1.1+")]
        public SwapCondition swapCondition = new SwapCondition();

        public override IEnumerable<string> ConfigErrors()
        {
            if (coverage != null)
            {
                coverage = coverage.ConvertAll(item => item.ToLowerInvariant());
                Coverage = new HashSet<string>(coverage);
                if (Coverage.Count != coverage.Count)
                    yield return nameof(coverage) + " has duplicate items: " + coverage.ToStringSafeEnumerable();
            }
        }
    }
}
