using Verse;

namespace JecsTools
{
    public static class FactionStuffUtility
    {
        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompConsole GetCompConsole(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompConsole comp)
                    return comp;
            }
            return null;
        }

        public static CompConsole TryGetCompConsole(this Thing thing)
        {
            return thing is ThingWithComps thingWithComps ? thingWithComps.GetCompConsole() : null;
        }

        // Avoiding Def.GetModExtension<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static FactionSettings GetFactionSettings(this Def def)
        {
            var modExtensions = def.modExtensions;
            if (modExtensions == null)
                return null;
            for (int i = 0, count = modExtensions.Count; i < count; i++)
            {
                if (modExtensions[i] is FactionSettings modExtension)
                    return modExtension;
            }
            return null;
        }
    }
}
