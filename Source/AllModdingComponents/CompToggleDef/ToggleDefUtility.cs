using Verse;

namespace CompToggleDef
{
    public static class ToggleDefUtility
    {
        // Avoiding ThingWithComps.GetComp<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CompToggleDef GetCompToggleDef(this ThingWithComps thing)
        {
            var comps = thing.AllComps;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CompToggleDef comp)
                    return comp;
            }
            return null;
        }

        public static CompToggleDef TryGetCompToggleDef(this Thing thing)
        {
            return thing is ThingWithComps thingWithComps ? thingWithComps.GetCompToggleDef() : null;
        }
    }
}
