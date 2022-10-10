using RimWorld;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Convenience class for getting Defs.
    /// </summary>
    [DefOf]
    public static class ShieldHediffDefOf
    {
        /// <summary>
        /// Represents how fatiguing blocking with a shield has been.
        /// </summary>
        public static HediffDef ShieldFatigue;

        static ShieldHediffDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ShieldHediffDefOf));
    }
}
