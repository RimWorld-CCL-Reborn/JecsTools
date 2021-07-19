using Verse;

namespace CompInstalledPart
{
    public static class InstalledPartUtility
    {
        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompInstalledPart GetCompInstalledPart(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompInstalledPart comp)
                    return comp;
            }
            return null;
        }

        public static CompInstalledPart TryGetCompInstalledPart(this Thing thing)
        {
            return thing is ThingWithComps thingWithComps ? thingWithComps.GetCompInstalledPart() : null;
        }
    }
}
