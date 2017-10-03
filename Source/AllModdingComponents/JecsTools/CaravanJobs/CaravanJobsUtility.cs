using RimWorld;
using RimWorld.Planet;
using Verse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JecsTools
{
    public static class CaravanJobsUtility
    {
        public static void TeachCaravan(Caravan c, SkillDef sd, float rate)
        {
            if (c.PawnsListForReading.NullOrEmpty())
            {
                Log.Error("JecsTools :: No characters found in caravan.");
                return;
            }
            foreach (Pawn p in c.PawnsListForReading)
            {
                if (p.skills != null) p.skills.Learn(sd, rate);
            }
        }

        public static float GetStatValueTotal(Caravan c, StatDef s)
        {
            float result = 1f;
            if (c.PawnsListForReading.NullOrEmpty())
            {
                Log.Error("JecsTools :: No characters found in caravan.");
                return result;
            }
            foreach (Pawn p in c.PawnsListForReading)
            {
                result += p?.GetStatValue(s) ?? 0f;
            }
            return result;
        }

        public static float GetStatValueAverage(Caravan c, StatDef s)
        {
            float result = 1f;
            if (c.PawnsListForReading.NullOrEmpty())
            {
                Log.Error("JecsTools :: No characters found in caravan.");
                return result;
            }
            foreach (Pawn p in c.PawnsListForReading)
            {
                result += p?.GetStatValue(s) ?? 0f;
            }
            result /= c.PawnsListForReading.Count;
            return result;
        }
    }
}
