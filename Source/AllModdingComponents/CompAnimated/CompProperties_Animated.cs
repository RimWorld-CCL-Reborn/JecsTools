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
namespace CompAnimated
{
    public class CompProperties_Animated : CompProperties
    {
        public SoundDef sound = null;
        public float secondsBetweenFrames = 0.0f;
        public List<GraphicData> movingFrames = new List<GraphicData>();
        public List<GraphicData> stillFrames = new List<GraphicData>();
        public CompProperties_Animated() => this.compClass = typeof(CompAnimated);
    }
}
