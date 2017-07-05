using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
namespace CompToggleDef
{
    public class CompProperties_ToggleDef: CompProperties
    {

        public string labelKey;

        public CompProperties_ToggleDef()
        {
            this.compClass = typeof(CompToggleDef);
        }

    }
}
