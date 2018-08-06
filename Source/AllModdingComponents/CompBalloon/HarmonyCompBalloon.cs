using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;

namespace CompLumbering
{
    [StaticConstructorOnStartup]
    static class HarmonyCompLumbering
    {
        static HarmonyCompLumbering()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.lumbering");
            harmony.Patch(AccessTools.Method(typeof(PawnGraphicSet), "nakedGraphic"), null, new HarmonyMethod(typeof(HarmonyCompLumbering), "SoundMissPrefix"));
        }
        // Verse.PawnGraphicSet


    }
}
