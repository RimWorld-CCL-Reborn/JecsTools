using System.Runtime.CompilerServices;
using Verse;

namespace JecsTools
{
    public static class HediffCompUtility
    {
        // Similar to ThingWithComps.GetComp<T> (and ThingCompUtility.TryGetComp<T>), HediffUtility.TryGetComp<T>
        // is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand is fast.
        // Inlining the method effectively makes the method non-generic.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetHediffComp<T>(this HediffWithComps hediff) where T : HediffComp
        {
            var comps = hediff.comps;
            for (int i = 0, compCount = comps.Count; i < compCount; i++)
            {
                if (comps[i] is T comp)
                    return comp;
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetHediffComp<T>(this Pawn pawn) where T : HediffComp
        {
            var hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0, hediffCount = hediffs.Count; i < hediffCount; i++)
            {
                if (hediffs[i] is HediffWithComps hediff)
                {
                    if (hediff.GetHediffComp<T>() is T comp)
                        return comp;
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T GetHediffCompProps<T>(this HediffDef hediffDef) where T : HediffCompProperties
        {
            var comps = hediffDef.comps;
            for (int i = 0, compCount = comps.Count; i < compCount; i++)
            {
                if (comps[i] is T comp)
                    return comp;
            }
            return null;
        }
    }
}
