using Verse;

namespace JecsTools
{
    public static class ModCompatibilityUtility
    {
        public static bool IsUniversalPawn(object toCheck)
        {
            if (toCheck is Pawn || toCheck.GetType().ToString() == "Psychology.PsychologyPawn")
                return true;
            return false;
        }
    }
}