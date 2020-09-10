using RimWorld;
using Verse;

namespace CompInstalledPart
{
    [DefOf]
    public static class CompInstalledPartDefOf
    {
        public static JobDef CompInstalledPart_InstallPart;
        public static JobDef CompInstalledPart_UninstallPart;

        static CompInstalledPartDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(CompInstalledPartDefOf));
    }
}
