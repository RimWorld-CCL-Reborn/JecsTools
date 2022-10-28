//#define DEBUGLOG

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    public static partial class HarmonyPatches
    {
        //For alternating fire on some weapons
        public static Dictionary<Thing, int> AlternatingFireTracker = new Dictionary<Thing, int>();

        static HarmonyPatches()
        {
            var harmony = new Harmony("jecstools.jecrell.main");
            var type = typeof(HarmonyPatches);
            
            HarmonyPatches_ApparelExtension(harmony, type);
            HarmonyPatches_BuildingExtension(harmony, type);
            HarmonyPatches_DamageSoak(harmony, type);
            HarmonyPatches_Debug(harmony, type);
            HarmonyPatches_ExtraMeleeDamages(harmony, type);
            HarmonyPatches_Knockback(harmony, type);
            HarmonyPatches_StartWithHediff(harmony, type);
            HarmonyPatches_StartWithGenes(harmony, type);
        }

        [Conditional("DEBUGLOG")]
        private static void DebugMessage(string s)
        {
            Log.Message(s);
        }
        
        private static string ToString(ExtraDamage ed)
        {
            return $"(def={ed.def}, amount={ed.amount}, armorPenetration={ed.armorPenetration}, chance={ed.chance})";
        }
        

        
    }
}
