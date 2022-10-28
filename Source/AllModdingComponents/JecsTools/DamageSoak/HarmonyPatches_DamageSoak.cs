using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    
    public static float? tempDamageAmount = null;
    public static float? tempDamageAbsorbed = null;
    
    public static void HarmonyPatches_DamageSoak(Harmony harmony, Type type)
    {
        //Allow fortitude (HediffComp_DamageSoak) to soak damage
        //Adds HediffCompProperties_DamageSoak checks to damage
        harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage)),
            prefix: new HarmonyMethod(type, nameof(HarmonyPatches.PreApplyDamage_PrePatch)));
        //Applies cached armor damage and absorption
        harmony.Patch(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"),
            prefix: new HarmonyMethod(type, nameof(HarmonyPatches.Pre_ApplyArmor)));
        //Applies damage soak motes
        harmony.Patch(AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage)),
            postfix: new HarmonyMethod(type, nameof(HarmonyPatches.Post_GetPostArmorDamage)));
    }
    
    public static bool PreApplyDamage_PrePatch(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
    {
        DebugMessage($"c6c:: === Enter Harmony Prefix --- PreApplyDamage_PrePatch for {___pawn} and {dinfo} ===");
        if (___pawn != null)
        {
            DebugMessage("c6c:: Pawn exists.");
            var hediffSet = ___pawn.health.hediffSet;
            if (hediffSet.hediffs.Count > 0)
            {
                DebugMessage("c6c:: Pawn has hediffs.");
                // See above ArmorUtility comments.
                if (PreApplyDamage_ApplyDamageSoakers(ref dinfo, hediffSet, ___pawn))
                {
                    DebugMessage($"c6c:: === Exit Harmony Prefix --- PreApplyDamage_PrePatch for {___pawn} and {dinfo} ===");
                    absorbed = true;
                    return false;
                }
            }
        }

        // TODO: tempDamageAmount shouldn't be set if there are no damage soaks.
        tempDamageAmount = dinfo.Amount;
        DebugMessage($"c6c:: tempDamageAmount <= {tempDamageAmount}");
        absorbed = false;
        DebugMessage($"c6c:: === Exit Harmony Prefix --- PreApplyDamage_PrePatch for {___pawn} and {dinfo} ===");
        return true;
    }
    
    
    
    private static void DamageSoakedMote(Pawn pawn, float soakedDamage)
    {
        if (soakedDamage > 0f && pawn != null && pawn.Spawned && pawn.MapHeld != null &&
            pawn.DrawPos is Vector3 drawVecDos && drawVecDos.InBounds(pawn.MapHeld))
        {
            // To avoid any rounding bias, use RoundRandom for converting int to float.
            var roundedSoakedDamage = GenMath.RoundRandom(soakedDamage);
            DebugMessage($"c6c:: DamageSoakedMote for {pawn}: {soakedDamage} rounded to {roundedSoakedDamage}");
            MoteMaker.ThrowText(drawVecDos, pawn.MapHeld, "JT_DamageSoaked".Translate(roundedSoakedDamage));
        }
    }
    
        private static bool PreApplyDamage_ApplyDamageSoakers(ref DamageInfo dinfo, HediffSet hediffSet, Pawn pawn)
        {
            // Multiple damage soak hediff comps stack.
            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyDamageSoakers for {pawn} and {dinfo} ---");
            var damageDef = dinfo.Def;
            var totalSoakedDamage = 0f;
            foreach (var hediffComp in hediffSet.GetAllComps())
            {
                if (!(hediffComp is HediffComp_DamageSoak damageSoakComp))
                    continue;
                DebugMessage("c6c:: Soak Damage Hediff checked.");

                var soakProps = damageSoakComp.Props;
                if (soakProps == null)
                {
                    DebugMessage("c6c:: Soak Damage Hediff has no damage soak XML properties.");
                    continue;
                }
                if (soakProps.settings.NullOrEmpty())
                {
                    DebugMessage("c6c:: Soak Damage Hediff has no damage soak settings.");

                    // Null, here, means "all damage types", so null should pass this check.
                    if (soakProps.damageType != null && soakProps.damageType != damageDef)
                    {
                        DebugMessage($"c6c:: {damageDef.label.CapitalizeFirst()} is not in soak settings.");
                        continue;
                    }

                    if (soakProps.damageTypesToExclude != null &&
                        soakProps.damageTypesToExclude.Contains(damageDef))
                    {
                        DebugMessage($"c6c:: {damageDef.label.CapitalizeFirst()} is to be excluded from damage soak.");
                        continue;
                    }

                    var dmgAmount = dinfo.Amount;
                    var soakedDamage = Mathf.Min(soakProps.damageToSoak, dmgAmount);
                    DebugMessage($"c6c:: Soaked: Min({soakProps.damageToSoak}, {dinfo.Amount}) => {soakedDamage}");
                    dmgAmount -= soakedDamage;
                    DebugMessage($"c6c:: Damage amount: {dinfo.Amount} - {soakedDamage} => {dmgAmount}");
                    totalSoakedDamage += soakedDamage;
                    DebugMessage($"c6c:: Total soaked: {totalSoakedDamage}");
                    dinfo.SetAmount(dmgAmount);

                    if (dinfo.Amount > 0)
                    {
                        DebugMessage($"c6c:: More damage exists. Continuing check for soakers.");
                        continue;
                    }

                    DamageSoakedMote(pawn, totalSoakedDamage);
                    DebugMessage($"c6c:: Damage absorbed.");
                    DebugMessage($"c6c::   FINAL RESULT -- Total soaked: {totalSoakedDamage}, damage amount: {dinfo.Amount}.");
                    DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyDamageSoakers for {pawn} and {dinfo} ---");
                    return true;
                }
                else
                {
                    DebugMessage("c6c:: Soak Damage Hediff has damage soak settings.");
                    foreach (var soakSettings in soakProps.settings)
                    {
                        DebugMessage($"c6c:: Hediff Damage: {damageDef}");
                        if (soakSettings.damageType != null)
                            DebugMessage($"c6c:: Soak Type: {soakSettings.damageType}");
                        else
                            DebugMessage($"c6c:: Soak Type: All");

                        //Null, here, means "all damage types"
                        //So Null should pass this check.
                        if (soakSettings.damageType != null && soakSettings.damageType != damageDef)
                        {
                            DebugMessage($"c6c:: No match. No soak.");
                            continue;
                        }

                        // This variable tracks whether the damage should be excluded by a damageTypesToExclude
                        // rule for breaking out of a nested for loop without using goto
                        bool damageExcluded = false;
                        if (!soakSettings.damageTypesToExclude.NullOrEmpty())
                        {
                            DebugMessage($"c6c:: Damage Soak Exlusions: ");
                            foreach (var exclusion in soakSettings.damageTypesToExclude)
                            {
                                DebugMessage($"c6c::    {exclusion}");
                                if (exclusion == damageDef)
                                {
                                    DebugMessage($"c6c:: Exclusion match. Damage soak aborted.");
                                    damageExcluded = true;
                                    break;
                                }
                            }
                            if (damageExcluded)
                                continue;
                        }

                        var dmgAmount = dinfo.Amount;
                        var soakedDamage = Mathf.Min(soakSettings.damageToSoak, dmgAmount);
                        DebugMessage($"c6c:: Soaked: Min({soakSettings.damageToSoak}, {dinfo.Amount}) => {soakedDamage}");
                        dmgAmount -= soakedDamage;
                        DebugMessage($"c6c:: Damage amount: {dinfo.Amount} - {soakedDamage} => {dmgAmount}");
                        totalSoakedDamage += soakedDamage;
                        DebugMessage($"c6c:: Total soaked: {totalSoakedDamage}");
                        dinfo.SetAmount(dmgAmount);

                        if (dinfo.Amount > 0)
                        {
                            DebugMessage($"c6c:: Unsoaked damage remains. Checking for more soakers.");
                            continue;
                        }

                        DamageSoakedMote(pawn, totalSoakedDamage);
                        DebugMessage($"c6c:: Damage absorbed.");
                        DebugMessage($"c6c::  FINAL RESULT -- Total soaked: {totalSoakedDamage}, damage amount: {dinfo.Amount}.");
                        DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyDamageSoakers for {pawn} and {dinfo} ---");
                        return true;
                    }
                }
            }
            if (totalSoakedDamage > 0)
            {
                DamageSoakedMote(pawn, totalSoakedDamage);
                DebugMessage($"c6c::   FINAL RESULT -- Total soaked: {totalSoakedDamage}, damage amount: {dinfo.Amount}.");
            }
            DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyDamageSoakers for {pawn} and {dinfo} ---");
            return false;
        }
    
            // ArmorUtility patches:
        // These are a workaround for PreApplyDamage_PrePatch changes to the dinfo struct not being saved, due to
        // Pawn_HealthTracker.PreApplyDamage dinfo parameter being passed by value (PreApplyDamage_PrePatch has it passed
        // by reference, but this only affects the patch; Pawn_HealthTracker.PreApplyDamage still has it passed by value).
        // Incidentally, these patches have another purpose: it allows other Pawn_HealthTracker.PreApplyDamage code like
        // Apparel.CheckPreAbsorbDamage (like shield belts), various pawn-specific notifications affecting pawn behavior,
        // and other mod's patches on the method to run, some of which could affect dinfo.Amount and absorbed flag.
        // Indeed, the choice of prefix patching Pawn_HealthTracker.PreApplyDamage rather than a Pawn.PreApplyDamage prefix
        // or a Pawn_HealthTracker.PreApplyDamage postfix is likely a compromise to allow as much change to dinfo as
        // possible yet still apply damage soaks before shield belt absorption.
        // Pawn_HealthTracker.PreApplyDamage notification specifics: if it runs (no ThingComp.PostPreApplyDamage sets
        // absorbed flag), prisoner guilt, AI updates, and current danger are triggered. If no Apparel.CheckPreAbsorbDamage
        // sets the absorbed flag, stun effects, pawn thought/memory, and tale recording are triggered.
        // XXX: I do not think this patch is reliable because:
        // 1) It's not guaranteed to run under certain conditions (e.g. if dinfo.IgnoreArmor) when it should.
        // 2) dinfo.Amount can be divided into multiple DamageInfos under certain conditions (bomb/flame damage),
        //    which this doesn't take into account.
        // 3) It assumes that all new damage amount since our PreApplyDamage_PrePatch ran should be damage soaked
        //    (as long as this patch runs, e.g. not absorbed, etc.), by setting the damage amount back to tempDamageAmount,
        //    the final damage amount recorded in PreApplyDamage_PrePatch, even if no damage soaks exist
        //    (see TODO in PreApplyDamage_PrePatch).
        // 4) If damage amount decreased yet still non-zero since our PreApplyDamage_PrePatch ran, this patch will
        //    increase the damage amount back to tempDamageAmount, which is the total opposite of damage soaking.
        // 5) The relationship of PreApplyDamage_PrePatch and this patch with respect to tempDamageAmount is fragile,
        //    especially since (1) and tempDamageAmount not always being set in PreApplyDamage_PrePatch.
        //    If another mod happens to use ArmorUtility without going through PreApplyDamage, this scheme will break.
        // TODO:
        // If we want to retain damage soaking before shield belt absorption:
        //    Instead of this patch, postfix patch (highest patch priority) Pawn.PreApplyDamage to update the original
        //    dinfo struct with any changes from PreApplyDamage_PrePatch. Make PreApplyDamage_PrePatch patch with lowest
        //    patch priority so that it runs right before Pawn_HealthTracker.PreApplyDamage. This should ensure that there
        //    no other changes to dinfo in between PreApplyDamage_PrePatch and the new Pawn.PreApplyDamage postfix patch
        //    that should've been tracked. tempDamageAmount is still needed to to transfer the damage amount info between
        //    these patches.
        // If we're fine with damage soaks applying after shield belt absorption:
        //    Simplify into a single Pawn.PreApplyDamage postfix patch.
        public static void Pre_ApplyArmor(ref float damAmount, Pawn pawn)
        {
            if (tempDamageAmount != null && damAmount > 0f)
            {
                var damageDiff = Mathf.Max(damAmount - tempDamageAmount.Value, 0f);
                var newDamAmount = GenMath.RoundRandom(tempDamageAmount.Value);
                DebugMessage($"c6c:: ApplyArmor prefix on {pawn}: tempDamageAmount {tempDamageAmount} => null, damAmount {damAmount} => {newDamAmount}");
                damAmount = newDamAmount;
                tempDamageAmount = null;
                if (damageDiff > 0f)
                    tempDamageAbsorbed = damageDiff;
            }
        }

    

        // XXX: Damage soak mote is already emitted in PreApplyDamage_ApplyDamageSoakers, so this leads to a misleading
        // redundant soak mote. Worse, if the damage amount actually changes between PreApplyDamage_ApplyDamageSoakers
        // and Pre_ApplyArmor, leading to a tempDamageAbsorbed that's different from PreApplyDamage_ApplyDamageSoakers's
        // totalSoakedDamage, this is even more misleading.
        public static void Post_GetPostArmorDamage(Pawn pawn)
        {
            if (tempDamageAbsorbed != null)
            {
                DebugMessage($"c6c:: GetPostArmorDamage postfix on {pawn}: tempDamageAbsorbed {tempDamageAbsorbed}");
                if (pawn.GetHediffComp<HediffComp_DamageSoak>() != null)
                {
                    DamageSoakedMote(pawn, tempDamageAbsorbed.Value);
                }

                tempDamageAbsorbed = null;
            }
        }
}
