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
    internal class CompExtraSounds : ThingComp
    {
        public CompProperties_ExtraSounds Props => (CompProperties_ExtraSounds)this.props;
    }
}
