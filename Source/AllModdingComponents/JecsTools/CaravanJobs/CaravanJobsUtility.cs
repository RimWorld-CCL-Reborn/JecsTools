using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JecsTools
{
    public static class CaravanJobsUtility
    {
        public static void TeachCaravan(Caravan c, SkillDef sd, float rate)
        {
            if (c.PawnsListForReading.Count == 0)
            {
                Log.Error("JecsTools :: No characters found in caravan.");
                return;
            }
            foreach (var p in c.PawnsListForReading)
                p.skills?.Learn(sd, rate);
        }

        public static float GetStatValueTotal(Caravan c, StatDef s)
        {
            var result = 1f;
            if (c.PawnsListForReading.Count == 0)
            {
                Log.Error("JecsTools :: No characters found in caravan.");
                return result;
            }
            foreach (var p in c.PawnsListForReading)
                result += p?.GetStatValue(s) ?? 0f;
            return result;
        }

        public static float GetStatValueAverage(Caravan c, StatDef s)
        {
            var result = 1f;
            if (c.PawnsListForReading.Count == 0)
            {
                Log.Error("JecsTools :: No characters found in caravan.");
                return result;
            }
            foreach (var p in c.PawnsListForReading)
                result += p?.GetStatValue(s) ?? 0f;
            result /= c.PawnsListForReading.Count;
            return result;
        }

        // Avoiding World.GetComponent<T> and implementing a specific non-generic version of it here.
        // That method is slow because the `isinst` instruction with generic type arg operands is very slow,
        // while `isinst` instruction against non-generic type operand like used below is fast.
        public static CaravanJobGiver GetCaravanJobGiver()
        {
            var comps = Find.World.components;
            for (int i = 0, count = comps.Count; i < count; i++)
            {
                if (comps[i] is CaravanJobGiver comp)
                    return comp;
            }
            return null;
        }
    }
}
