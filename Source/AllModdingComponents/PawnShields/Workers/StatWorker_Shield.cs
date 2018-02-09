using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Worker class specifically made to show off the snazzy stats for shields.
    /// </summary>
    public class StatWorker_Shield : StatWorker
    {
        public override bool ShouldShowFor(BuildableDef eDef)
        {
            if (eDef is ThingDef thingDef && thingDef.HasComp(typeof(CompShield)))
                return true;

            return false;
        }
    }
}
