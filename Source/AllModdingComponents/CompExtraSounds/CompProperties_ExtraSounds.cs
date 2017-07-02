using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
namespace CompExtraSounds
{
    internal class CompProperties_ExtraSounds : CompProperties
    {
        public SoundDef soundMiss;
        public SoundDef soundHitPawn;
        public SoundDef soundHitBuilding;
        public SoundDef soundExtra;
        public SoundDef soundExtraTwo;

        public CompProperties_ExtraSounds() => this.compClass = typeof(CompExtraSounds);
    }
}
