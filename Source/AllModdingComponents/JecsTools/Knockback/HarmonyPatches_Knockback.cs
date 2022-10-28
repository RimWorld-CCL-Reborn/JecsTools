using System;
using System.Collections.Concurrent;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_Knockback(Harmony harmony, Type type)
    {
        //Applies knockback
        harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PreApplyDamage)),
            prefix: new HarmonyMethod(type, nameof(Pawn_PreApplyDamage_Prefix)) { priority = Priority.High },
            postfix: new HarmonyMethod(type, nameof(Pawn_PreApplyDamage_Postfix)) { priority = Priority.Low });
        harmony.Patch(AccessTools.Method(typeof(Scenario), nameof(Scenario.TickScenario)),
            postfix: new HarmonyMethod(type, nameof(Scenario_TickScenario_Postfix)));
    }
    
    
    private static readonly ConcurrentDictionary<Pair<Thing, Thing>, int> knockbackLastTicks =
        new ConcurrentDictionary<Pair<Thing, Thing>, int>();

    
    // Stores original dinfo.Amount in __state, that below Pawn_PreApplyDamage_Postfix can access.
    public static void Pawn_PreApplyDamage_Prefix(ref DamageInfo dinfo, ref float __state)
    {
        __state = dinfo.Amount;
    }
    
    
    // This should happen after all modifications to dinfo and any possible setting of absorbed flag,
    // i.e. after all ThingComp.PostPreApplyDamage and Apparel.CheckPreAbsorbDamage (shield belts).
    public static void Pawn_PreApplyDamage_Postfix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed,
        float __state)
    {
        if (dinfo.Weapon is ThingDef weaponDef && !weaponDef.IsRangedWeapon &&
            dinfo.Instigator is Pawn instigator)
        {
            DebugMessage($"c6c:: Instigator using non-ranged weapon: {dinfo}");
            var hediffCompKnockback = instigator.GetHediffComp<HediffComp_Knockback>();
            if (hediffCompKnockback != null)
            {
                // Hack to prevent multiple knockbacks occurring due to multiple damage infos (e.g. extra damage) for same instigator+target:
                // prevent knockback if tick hasn't passed since last knockback for instigator+target pair.
                // This requires a (instigator+target)=>tick cache, which is cleared after every tick via Scenario_TickScenario_Postfix.
                var pair = new Pair<Thing, Thing>(instigator, __instance);
                var ticks = Find.TickManager.TicksGame;
                if (knockbackLastTicks.TryGetValue(pair, out var lastTicks) && lastTicks == ticks)
                    return;
                knockbackLastTicks[pair] = ticks;
                hediffCompKnockback.ApplyKnockback(__instance,
                    damageAbsorbedPercent: absorbed ? 1f : 1f - Mathf.Clamp01(dinfo.Amount / __state));
            }
        }
    }

    public static void Scenario_TickScenario_Postfix()
    {
        knockbackLastTicks.Clear();
    }
    
}
