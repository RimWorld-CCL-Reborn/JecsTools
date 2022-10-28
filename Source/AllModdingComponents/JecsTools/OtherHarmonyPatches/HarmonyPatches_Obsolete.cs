using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    // Not sure if another mod is using this, so obsoleting it rather than deleting it.
    [Obsolete]
    public static Vector3 PushResult(Thing Caster, Thing thingToPush, int pushDist, out bool collision)
    {
        return HediffComp_Knockback.PushResult(Caster, thingToPush, pushDist, out var _, out collision);
    }

    // Not sure if another mod is using this, so obsoleting it rather than deleting it.
    [Obsolete]
    public static void PushEffect(Thing Caster, Thing target, int distance, bool damageOnCollision = false)
    {
        HediffComp_Knockback.PushEffect(Caster, target, damageAbsorbedPercent: 0f, new HediffCompProperties_Knockback
        {
            knockDistance = new FloatRange(distance, distance),
            knockDistanceAbsorbedPercentCurve = HediffComp_Knockback.AlwaysOneCurve,
            knockDistanceMassCurve = HediffComp_Knockback.AlwaysOneCurve,
            knockImpactDamage = damageOnCollision ? new FloatRange(8f, 10f) : default,
            knockImpactDamageDistancePercentCurve = HediffComp_Knockback.AlwaysOneCurve,
            knockImpactDamageType = DamageDefOf.Blunt,
        });
    }
}
