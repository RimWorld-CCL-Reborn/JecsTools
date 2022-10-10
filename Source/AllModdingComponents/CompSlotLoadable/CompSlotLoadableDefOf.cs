using RimWorld;
using Verse;

namespace CompSlotLoadable
{
    [DefOf]
    public static class CompSlotLoadableDefOf
    {
        public static JobDef GatherSlotItem;

        public static StatCategoryDef SlotLoadable;

        static CompSlotLoadableDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(CompSlotLoadableDefOf));
    }
}
