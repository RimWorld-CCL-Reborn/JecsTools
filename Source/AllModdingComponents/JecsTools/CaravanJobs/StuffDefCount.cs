using System;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class StuffDefCount : IEquatable<StuffDefCount>, IExposable
    {
        private StuffCategoryDef stuffDef;

        private int count;

        public StuffDefCount(StuffCategoryDef stuffDef, int count)
        {
            if (count < 0)
            {
                Log.Warning($"Tried to set StuffDefCount count to {count}. stuffDef={stuffDef}", false);
                count = 0;
            }
            this.stuffDef = stuffDef;
            this.count = count;
        }

        public StuffCategoryDef StuffDef => stuffDef;

        public int Count => count;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref stuffDef, nameof(stuffDef));
            Scribe_Values.Look(ref count, nameof(count), 1);
        }

        public StuffDefCount WithCount(int newCount) => new StuffDefCount(stuffDef, newCount);

        public override bool Equals(object obj) => obj is StuffDefCount other && Equals(other);

        public bool Equals(StuffDefCount other) => this == other;

        public static bool operator ==(StuffDefCount a, StuffDefCount b) => a.stuffDef == b.stuffDef && a.count == b.count;

        public static bool operator !=(StuffDefCount a, StuffDefCount b) => !(a == b);

        public override int GetHashCode() => Gen.HashCombine(count, stuffDef);

        public override string ToString() => $"({count}x {stuffDef?.defName ?? "null"}";

        public static implicit operator StuffDefCount(StuffCategoryCountClass t) => new StuffDefCount(t.stuffCatDef, t.count);
    }
}
