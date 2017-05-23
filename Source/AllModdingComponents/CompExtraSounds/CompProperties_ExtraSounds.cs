using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompExtraSounds
{
    internal class CompProperties_ExtraSounds : CompProperties
    {
        public SoundDef soundMiss;
        public SoundDef soundHitPawn;
        public SoundDef soundHitBuilding;
        public SoundDef soundExtra;
        public SoundDef soundExtraTwo;

        public CompProperties_ExtraSounds()
        {
            this.compClass = typeof(CompExtraSounds);
        }
    }
}
