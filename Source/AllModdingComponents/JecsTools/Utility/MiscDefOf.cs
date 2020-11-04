using RimWorld;
using Verse;

namespace JecsTools
{
    [DefOf]
    public static class MiscDefOf
    {
        public static WorldObjectDef WorldObject_ProgressBar;
        public static RulePackDef JT_GrappleSuccess;
        //public static ThoughtDef PJ_ThoughtPush;
        public static ThingDef JT_FlyingObject;

        static MiscDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MiscDefOf));
    }
}
