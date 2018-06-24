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
                Log.Warning(string.Concat(new object[]
                {
                    "Tried to set StuffDefCount count to ",
                    count,
                    ". stuffDef=",
                    stuffDef
                }), false);
                count = 0;
            }
            this.stuffDef = stuffDef;
            this.count = count;
        }

        public StuffCategoryDef StuffDef
        {
            get
            {
                return this.stuffDef;
            }
        }

        public int Count
        {
            get
            {
                return this.count;
            }
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<StuffCategoryDef>(ref this.stuffDef, "stuffDef");
            Scribe_Values.Look<int>(ref this.count, "count", 1, false);
        }

        public StuffDefCount WithCount(int newCount)
        {
            return new StuffDefCount(this.stuffDef, newCount);
        }

        public override bool Equals(object obj)
        {
            return obj is StuffDefCount && this.Equals((StuffDefCount)obj);
        }

        public bool Equals(StuffDefCount other)
        {
            return this == other;
        }

        public static bool operator ==(StuffDefCount a, StuffDefCount b)
        {
            return a.stuffDef == b.stuffDef && a.count == b.count;
        }

        public static bool operator !=(StuffDefCount a, StuffDefCount b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return Gen.HashCombine<StuffCategoryDef>(this.count, this.stuffDef);
        }

        public override string ToString()
        {
            return string.Concat(new object[]
            {
                "(",
                this.count,
                "x ",
                (this.stuffDef == null) ? "null" : this.stuffDef.defName,
                ")"
            });
        }

        public static implicit operator StuffDefCount(StuffCategoryCountClass t)
        {
            return new StuffDefCount(t.stuffCatDef, t.count);
        }
    }
}