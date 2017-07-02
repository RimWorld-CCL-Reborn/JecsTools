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
namespace CompLumbering
{
    internal class CompProperties_Lumbering : CompProperties
    {
        public SoundDef sound = null;
        public float secondsPerStep = 0.0f;
        public float secondsBetweenSteps = 0.0f;
        public GraphicData cycledGraphic = null;

        public CompProperties_Lumbering() => this.compClass = typeof(CompLumbering);
    }
}
