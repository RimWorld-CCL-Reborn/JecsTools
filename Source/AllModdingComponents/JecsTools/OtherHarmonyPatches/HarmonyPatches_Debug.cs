using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_Debug(Harmony harmony, Type type)
    {
        //Improve DamageInfo.ToString for debugging purposes.
        harmony.Patch(AccessTools.Method(typeof(DamageInfo), nameof(DamageInfo.ToString)),
            postfix: new HarmonyMethod(type, nameof(DamageInfo_ToString_Postfix)));
    }
    
    public static string DamageInfo_ToString_Postfix(string result, ref DamageInfo __instance)
    {
        var insertIndex = result.IndexOf(", angle=");
        return result.Insert(insertIndex, $", hitPart={__instance.HitPart.ToStringSafe()}, " +
                                          $"weapon={__instance.Weapon.ToStringSafe()}, armorPenetration={__instance.ArmorPenetrationInt}");
    }

}
