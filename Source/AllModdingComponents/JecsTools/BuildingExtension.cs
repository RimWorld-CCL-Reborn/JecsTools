using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class BuildingExtension : DefModExtension
    {
        private List<string> wipeCategories; // set via reflection

        [Unsaved]
        private HashSet<string> wipeCategorySet;

        public HashSet<string> WipeCategories
        {
            get
            {
                // DefModExtension lacks a ResolveReferences hook, so must use lazy initialization in property instead.
                if (wipeCategorySet == null && wipeCategories != null)
                {
                    wipeCategorySet = new HashSet<string>(wipeCategories.Count);
                    foreach (var category in wipeCategories)
                        wipeCategorySet.Add(category.ToLowerInvariant());
                }
                return wipeCategorySet;
            }
        }

        public override IEnumerable<string> ConfigErrors()
        {
            if (wipeCategories != null && WipeCategories.Count != wipeCategories.Count)
                yield return nameof(wipeCategories) + " has duplicate categories: " + wipeCategories.ToStringSafeEnumerable();
        }
    }
}
