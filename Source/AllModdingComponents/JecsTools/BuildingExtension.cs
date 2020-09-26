using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class BuildingExtension : DefModExtension
    {
        public List<string> wipeCategories;

        public HashSet<string> WipeCategories { get; private set; }

        public override IEnumerable<string> ConfigErrors()
        {
            if (wipeCategories != null)
            {
                wipeCategories = wipeCategories.ConvertAll(category => category.ToLowerInvariant());
                WipeCategories = new HashSet<string>(wipeCategories);
                if (WipeCategories.Count != wipeCategories.Count)
                    yield return nameof(wipeCategories) + " has duplicate categories: " + wipeCategories.ToStringSafeEnumerable();
            }
        }
    }
}
