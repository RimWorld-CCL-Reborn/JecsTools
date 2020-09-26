//#define DEBUGLOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AbilityUser;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    public static partial class HarmonyPatches
    {
        //For alternating fire on some weapons
        public static Dictionary<Thing, int> AlternatingFireTracker = new Dictionary<Thing, int>();

        // Verse.Pawn_HealthTracker
        public static bool StopPreApplyDamageCheck;

        public static int? tempDamageAmount = null;
        public static int? tempDamageAbsorbed = null;

        static HarmonyPatches()
        {
            var harmony = new Harmony("jecstools.jecrell.main");
            var type = typeof(HarmonyPatches);

            //Debug Line
            //------------
            //harmony.Patch(AccessTools.Method(typeof(PawnGroupKindWorker_Normal), nameof(PawnGroupKindWorker_Normal.MinPointsToGenerateAnything)),
            //    prefix: new HarmonyMethod(type, nameof(MinPointsTest)));
            //------------

            //Allow fortitude to soak damage

            //Adds HediffCompProperties_DamageSoak checks to damage
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage)),
                prefix: new HarmonyMethod(type, nameof(PreApplyDamage_PrePatch)));

            //Applies cached armor damage and absorption
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"),
                prefix: new HarmonyMethod(type, nameof(ApplyProperDamage)));

            //Applies damage soak motes
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage)),
                postfix: new HarmonyMethod(type, nameof(Post_GetPostArmorDamage)));

            //Allows for adding additional HediffSets when characters spawn using the StartWithHediff class.
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(type, nameof(Post_GeneratePawn)));

            //Checks apparel that uses the ApparelExtension
            harmony.Patch(AccessTools.Method(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether)),
                postfix: new HarmonyMethod(type, nameof(Post_CanWearTogether)));

            //Handles special cases of faction disturbances
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_MemberDied)),
                prefix: new HarmonyMethod(type, nameof(Notify_MemberDied)));

            //Handles FactionSettings extension to allow for fun effects when factions arrive.
            harmony.Patch(AccessTools.Method(typeof(PawnGroupKindWorker), nameof(PawnGroupKindWorker.GeneratePawns),
                    new[] { typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(bool) }),
                postfix: new HarmonyMethod(type, nameof(GeneratePawns)));

            //Handles cases where gendered apparel swaps out for individual genders.
            harmony.Patch(AccessTools.Method(typeof(PawnApparelGenerator), nameof(PawnApparelGenerator.GenerateStartingApparelFor)),
                postfix: new HarmonyMethod(type, nameof(GenerateStartingApparelFor_PostFix)));

            //BuildingExtension prevents some things from wiping other things when spawned/constructing/blueprinted.
            harmony.Patch(AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.SpawningWipes)),
                postfix: new HarmonyMethod(type, nameof(SpawningWipes_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintOver)),
                postfix: new HarmonyMethod(type, nameof(CanPlaceBlueprintOver_PostFix)));

            harmony.Patch(AccessTools.Method(typeof(Projectile), "CanHit"),
                postfix: new HarmonyMethod(type, nameof(CanHit_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Verb), "CanHitCellFromCellIgnoringRange"),
                prefix: new HarmonyMethod(type, nameof(CanHitCellFromCellIgnoringRange_Prefix)));

            //optionally use "CutoutComplex" shader for apparel that wants it
            //harmony.Patch(AccessTools.Method(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel)),
            //    transpiler: new HarmonyMethod(type, nameof(CutOutComplexApparel_Transpiler)));
        }

        [Conditional("DEBUGLOG")]
        private static void DebugMessage(string s)
        {
            Log.Message(s);
        }

        //Added B19, Oct 2019
        //ProjectileExtension check
        //Allows a bullet to pass through walls when fired.
        public static bool CanHitCellFromCellIgnoringRange_Prefix(Verb __instance, ref bool __result)
        {
            try
            {

                if (__instance.EquipmentCompSource?.PrimaryVerb?.verbProps?.defaultProjectile?.GetProjectileExtension() is ProjectileExtension ext)
                {
                    if (ext.passesWalls)
                        __result = true;
                    return false;
                }
            }
            catch
            {
            }
            return true;
        }

        //Added B19, Oct 2019
        //ProjectileExtension check
        //Ignores all structures as part of objects that disallow being fired through.
        public static void CanHit_PostFix(Projectile __instance, Thing thing, ref bool __result)
        {
            if (!__result && __instance.def?.GetProjectileExtension() is ProjectileExtension ext)
            {
                //Mods will often have their own walls, so we cannot do a def check for
                //ThingDefOf.Wall
                //Most "walls" should either be in the structure category or be able to hold walls.
                if (thing?.def is ThingDef def)
                    if (def.designationCategory == DesignationCategoryDefOf.Structure ||
                        def.holdsRoof == true)
                    {
                        if (ext.passesWalls)
                        {
                            __result = false;
                            return;
                        }
                    }
            }
        }

        public static void SpawningWipes_PostFix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
        {
            // If SpawningWipes is already returning true, don't need to do anything.
            if (__result == false && newEntDef is ThingDef newDef && oldEntDef is ThingDef oldDef)
            {
                if (HasSharedWipeCategory(newDef, oldDef))
                    __result = true;
            }
        }

        public static void CanPlaceBlueprintOver_PostFix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
        {
            // If CanPlaceBlueprintOver is already returning false, don't need to do anything.
            if (__result == true && newDef is ThingDef thingDef)
            {
                if (HasSharedWipeCategory(thingDef, oldDef))
                    __result = false;
            }
        }

        private static bool HasSharedWipeCategory(ThingDef newDef, ThingDef oldDef)
        {
            static HashSet<string> GetWipeCategories(ThingDef thingDef)
            {
                var buildingExtension = GenConstruct.BuiltDefOf(thingDef)?.GetBuildingExtension();
                if (buildingExtension == null)
                    return null;
                var wipeCategorySet = buildingExtension.WipeCategories;
                return wipeCategorySet == null || wipeCategorySet.Count == 0 ? null : wipeCategorySet;
            }

            var wipeCategoriesA = GetWipeCategories(newDef);
            DebugMessage($"{newDef} wipeCategoriesA: {wipeCategoriesA.ToStringSafeEnumerable()}");
            var wipeCategoriesB = GetWipeCategories(oldDef);
            DebugMessage($"{oldDef} wipeCategoriesB: {wipeCategoriesB.ToStringSafeEnumerable()}");
            if (wipeCategoriesB == null && wipeCategoriesA == null)
            {
                DebugMessage("both wipeCategories null => false");
                return false;
            }
            else if (wipeCategoriesA != null && wipeCategoriesB == null)
            {
                DebugMessage("wipeCategoriesB null => false");
                return false;
            }
            else if (wipeCategoriesB != null && wipeCategoriesA == null)
            {
                DebugMessage("wipeCategoriesA null => false");
                return false;
            }
            else
            {
                foreach (var strB in wipeCategoriesB)
                {
                    if (wipeCategoriesA.Contains(strB))
                    {
                        DebugMessage($"found shared wipeCategories ({strB}) => true");
                        return true;
                    }
                }
                DebugMessage("no shared wipeCategories => false");
                return false;
            }
        }

        //public static void MinPointsTest(PawnGroupMaker groupMaker)
        //{
        //    if (!(groupMaker?.options?.Count > 0))
        //    {
        //        Log.Message("No options available.");
        //        return;
        //    }
        //    foreach (var x in groupMaker.options)
        //    {
        //        Log.Message(x.kind.defName + " " + x.kind.isFighter.ToString() + " " + x.Cost);
        //    }
        //}

        //PawnApparelGenerator
        public static void GenerateStartingApparelFor_PostFix(Pawn pawn)
        {
            var allWornApparel = pawn.apparel?.WornApparel;
            if (allWornApparel.NullOrEmpty()) return;
            List<(Apparel, Apparel)> swapEntries = null;
            foreach (var wornApparel in allWornApparel)
            {
                if (wornApparel.def?.GetApparelExtension()?.swapCondition is SwapCondition sc &&
                    sc.swapWhenGender is Gender gen &&
                    gen != Gender.None && gen == pawn.gender)
                {
                    Apparel swapApparel = (Apparel)ThingMaker.MakeThing(sc.swapTo, wornApparel.Stuff);
                    // Avoid modifying WornApparel during its enumeration by doing the swaps afterwards.
                    swapEntries ??= new List<(Apparel worn, Apparel swap)>();
                    swapEntries.Add((wornApparel, swapApparel));
                }
            }
            if (swapEntries != null)
            {
                foreach (var (wornApparel, swapApparel) in swapEntries)
                {
                    PawnGenerator.PostProcessGeneratedGear(swapApparel, pawn);
                    if (ApparelUtility.HasPartsToWear(pawn, swapApparel.def))
                    {
                        pawn.apparel.Wear(swapApparel, false);
                        //DebugMessage($"apparel generation for {pawn}: swapped from {wornApparel} to {swapApparel}");
                    }
                    wornApparel.Destroy();
                    //DebugMessage($"apparel generation for {pawn}: destroyed old {wornApparel}");
                }
            }
        }

        public static Faction lastPhoneAideFaction = null;
        public static int lastPhoneAideTick = 0;

        //PawnGroupKindWorker
        public static void GeneratePawns(PawnGroupMakerParms parms, List<Pawn> __result)
        {
            if (__result.Count > 0 && parms.faction.def.GetFactionSettings() is FactionSettings fs)
            {
                fs.entrySoundDef?.PlayOneShotOnCamera();
            }
        }

        //Faction
        public static bool Notify_MemberDied(Faction __instance, Pawn member, DamageInfo? dinfo)
        {
            if (member?.Faction == null) return true;
            if (!dinfo.HasValue) return true;
            if (!(dinfo.Value.Instigator is Pawn instigator)) return true;

            var notLeader = __instance.leader != member;

            var notPlayerKiller = instigator.Faction != Faction.OfPlayerSilentFail;

            //var notAttackingPlayer = member.LastAttackedTarget.IsValid && member.LastAttackedTarget.Thing is Pawn p && p.Faction != Faction.OfPlayerSilentFail;

            var inTime = lastPhoneAideTick < (Find.TickManager?.TicksGame + GenDate.HoursPerDay ?? 0);

            var isPhoneFaction = __instance == lastPhoneAideFaction;

            if (isPhoneFaction &&
                inTime &&
                notLeader &&
                notPlayerKiller) // &&
                //notAttackingPlayer)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Using the new ApparelExtension, we can have a string based apparel check.
        /// </summary>
        public static void Post_CanWearTogether(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            try
            {
                static HashSet<string> GetCoverage(ThingDef thingDef)
                {
                    var coverage = thingDef.GetApparelExtension()?.Coverage;
                    return coverage == null || coverage.Count == 0 ? null : coverage;
                }

                if (A == null || B == null || body == null || __result == true) return;
                var coverageA = GetCoverage(A);
                var coverageB = GetCoverage(B);
                if (coverageA != null && coverageB != null)
                {
                    foreach (var coverageItem in coverageB)
                    {
                        if (coverageA.Contains(coverageItem))
                        {
                            __result = false;
                            break;
                        }
                    }
                }
                else if ((coverageA != null && coverageB == null) || (coverageA == null && coverageB != null))
                {
                    __result = true;
                }
            }
            catch (Exception e)
            {
                Log.Message(e.ToString());
            }
        }

        public static void Post_GeneratePawn(Pawn __result)
        {
            if (__result?.def?.race?.hediffGiverSets?.SelectMany(
                x => x.hediffGivers.Where(y => y is HediffGiver_StartWithHediff)).FirstOrDefault() is
                HediffGiver_StartWithHediff hediffGiver)
            {
                hediffGiver.GiveHediff(__result);
            }
        }

        //ArmorUtility
        public static void Post_GetPostArmorDamage(Pawn pawn)
        {
            if (tempDamageAbsorbed != null)
            {
                var hasFortitudeHediffs =
                    pawn?.health?.hediffSet?.hediffs?.Any(x => x.TryGetComp<HediffComp_DamageSoak>() != null) ?? false;
                if (hasFortitudeHediffs)
                {
                    DamageSoakedMote(pawn, tempDamageAbsorbed.Value);
                }

                tempDamageAbsorbed = null;
            }
        }

        public static void ApplyProperDamage(ref float damAmount)
        {
            if (tempDamageAmount != null && damAmount > 0)
            {
                float damageDiff = Mathf.Clamp(damAmount - tempDamageAmount.Value, 0, damAmount);

                DebugMessage("Apply amount original: " + damAmount);
                DebugMessage("Apply amount modified: " + tempDamageAmount);
                damAmount = GenMath.RoundRandom(tempDamageAmount.Value);
                tempDamageAmount = null;
                if (damageDiff > 0)
                    tempDamageAbsorbed = GenMath.RoundRandom(damageDiff);
            }
        }

        public static bool PreApplyDamage_PrePatch(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            DebugMessage($"c6c:: === Enter Harmony Prefix --- PreApplyDamage_ApplyExtraDamages ===");
            if (___pawn != null && !StopPreApplyDamageCheck)
            {
                DebugMessage("c6c:: Pawn exists. StopPreApplyDamageCheck: False");
                var hediffs = ___pawn.health?.hediffSet?.hediffs;
                if (!hediffs.NullOrEmpty())
                {
                    DebugMessage("c6c:: Pawn has health.");
                    //A list will stack.
                    var fortitudeHediffs = hediffs.FindAll(x => x.TryGetComp<HediffComp_DamageSoak>() != null);
                    if (fortitudeHediffs.Count > 0)
                    {
                        DebugMessage("c6c:: Pawn has Damage Soak hediff.");
                        try
                        {
                            if (PreApplyDamage_ApplyDamageSoakers(ref dinfo, out absorbed, fortitudeHediffs, ___pawn))
                            {
                                DebugMessage($"c6c:: === Exit Harmony Prefix --- PreApplyDamage_ApplyExtraDamages ===");
                                return false;
                            }
                        }
                        catch (NullReferenceException e)
                        {
                            DebugMessage($"c6c:: Soak failure:: {e.Message}");
                        }
                    }

                    if (dinfo.Weapon is ThingDef weaponDef && !weaponDef.IsRangedWeapon)
                        if (dinfo.Instigator is Pawn instigator)
                        {
                            DebugMessage("c6c:: Pawn has non-ranged weapon.");
                            try
                            {
                                if (PreApplyDamage_ApplyExtraDamages(ref dinfo, out absorbed, instigator, ___pawn)) return false;
                            }
                            catch (NullReferenceException e)
                            {
                                DebugMessage($"c6c:: Extra damages failure:: {e.Message}");
                            }

                            try
                            {
                                PreApplyDamage_ApplyKnockback(instigator, ___pawn);
                            }
                            catch (NullReferenceException e)
                            {
                                DebugMessage($"c6c:: Apply knockback failure:: {e.Message}");
                            }
                        }
                }
            }

            tempDamageAmount = (int)dinfo.Amount;
            absorbed = false;
            DebugMessage($"c6c:: === Exit Harmony Prefix --- PreApplyDamage_ApplyExtraDamages ===");
            return true;
        }

        private static void PreApplyDamage_ApplyKnockback(Pawn instigator, Pawn pawn)
        {
            var knockerProps =
                instigator?.health?.hediffSet?.hediffs?.Select(y => y.TryGetComp<HediffComp_Knockback>()).FirstOrDefault(
                    knockbackHediff => knockbackHediff != null)?.Props;
            if (knockerProps != null)
                if (knockerProps.knockbackChance >= Rand.Value)
                {
                    if (knockerProps.explosiveKnockback)
                    {
                        var explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion,
                            instigator.PositionHeld, instigator.MapHeld);
                        explosion.radius = knockerProps.explosionSize;
                        explosion.damType = knockerProps.explosionDmg;
                        explosion.instigator = instigator;
                        explosion.damAmount = 0;
                        explosion.weapon = null;
                        explosion.projectile = null;
                        explosion.preExplosionSpawnThingDef = null;
                        explosion.preExplosionSpawnChance = 0f;
                        explosion.preExplosionSpawnThingCount = 1;
                        explosion.postExplosionSpawnThingDef = null;
                        explosion.postExplosionSpawnChance = 0f;
                        explosion.postExplosionSpawnThingCount = 1;
                        explosion.applyDamageToExplosionCellsNeighbors = false;
                        explosion.chanceToStartFire = 0f;
                        explosion.damageFalloff = false; // dealMoreDamageAtCenter = false;
                        explosion.StartExplosion(null, null);
                    }

                    if (pawn != instigator && !pawn.Dead && !pawn.Downed && pawn.Spawned)
                    {
                        if (knockerProps.stunChance > -1 && knockerProps.stunChance >= Rand.Value)
                            pawn.stances.stunner.StunFor(knockerProps.stunTicks, instigator);
                        PushEffect(instigator, pawn, knockerProps.knockDistance.RandomInRange, damageOnCollision: true);
                    }
                }
        }

        private static bool PreApplyDamage_ApplyExtraDamages(ref DamageInfo dinfo, out bool absorbed, Pawn instigator, Pawn pawn)
        {
            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyExtraDamages ---");
            var extraDamagesHediff =
                instigator.health.hediffSet.hediffs.FirstOrDefault(y =>
                    y.TryGetComp<HediffComp_ExtraMeleeDamages>() != null);
            DebugMessage("c6c:: ExtraDamagesHediff variable assigned.");
            var damages = extraDamagesHediff?.TryGetComp<HediffComp_ExtraMeleeDamages>();
            DebugMessage("c6c:: Damages variable assigned.");
            if (damages?.Props?.ExtraDamages is List<Verse.ExtraDamage> extraDamages)
            {
                DebugMessage("c6c:: Extra damages list exists.");
                StopPreApplyDamageCheck = true;
                foreach (var dmg in extraDamages)
                {
                    DebugMessage($"c6c:: Extra Damage: {dmg.def.defName}");
                    if (pawn == null || !pawn.Spawned || pawn.Dead)
                    {
                        DebugMessage($"c6c:: Pawn is null, unspawned, or dead. Aborting.");
                        absorbed = false;
                        StopPreApplyDamageCheck = false;
                        return true;
                    }

                    //BattleLogEntry_MeleeCombat battleLogEntry_MeleeCombat = new BattleLogEntry_MeleeCombat(dinfo.Def.combatLogRules, true,
                    //    instigator, pawn, ImplementOwnerTypeDefOf.Bodypart, (dinfo.Weapon != null) ? dinfo.Weapon.label : dinfo.Def.label );
                    //DebugMessage($"c6c:: MeleeCombat Log generated.");
                    //DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
                    //DebugMessage($"c6c:: MeleeCombat Damage Result generated.");
                    //damageResult = pawn.TakeDamage(new DamageInfo(dmg.def, dmg.amount, dmg.armorPenetration, -1, instigator));
                    pawn.TakeDamage(new DamageInfo(dmg.def, dmg.amount, dmg.armorPenetration, -1, instigator));
                    DebugMessage($"c6c:: MeleeCombat TakeDamage set to -- Def:{dmg.def.defName} Amt:{dmg.amount} ArmorPen:{dmg.armorPenetration}.");
                    //try
                    //{
                    //    damageResult.AssociateWithLog(battleLogEntry_MeleeCombat);
                    //    DebugMessage($"c6c:: MeleeCombat Damage associated with log.");
                    //}
                    //catch (Exception e)
                    //{
                    //    DebugMessage($"c6c:: Failed to associate log: {e.Message}");
                    //}
                    //battleLogEntry_MeleeCombat.def = LogEntryDefOf.MeleeAttack;
                    //DebugMessage($"c6c:: MeleeCombat Log def set as MeleeAttack.");
                    //Find.BattleLog.Add(battleLogEntry_MeleeCombat);
                    //DebugMessage($"c6c:: MeleeCombat Log added to battle log.");
                }

                StopPreApplyDamageCheck = false;
            }
            DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyExtraDamages ---");
            absorbed = false;
            return false;
        }

        private static bool PreApplyDamage_ApplyDamageSoakers(ref DamageInfo dinfo, out bool absorbed,
            List<Hediff> fortitudeHediffs, Pawn pawn)
        {
            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyDamageSoakers ---");
            var soakedDamage = 0;
            foreach (var fortitudeHediff in fortitudeHediffs)
            {
                DebugMessage("c6c:: Soak Damage Hediff checked.");

                var soakSetting = fortitudeHediff.TryGetComp<HediffComp_DamageSoak>()?.Props;
                if (soakSetting == null)
                {
                    DebugMessage("c6c:: Soak Damage Hediff has no damage soak XML properties.");
                    continue;
                }
                if (soakSetting.settings.NullOrEmpty())
                {
                    DebugMessage("c6c:: Soak Damage Hediff has no damage soak settings.");

                    //Null, here, means "all damage types"
                    //So Null should pass this check.
                    if (soakSetting.damageType != null && soakSetting.damageType != dinfo.Def)
                    {
                        DebugMessage($"c6c:: {dinfo.Def.label.CapitalizeFirst()} is not in soak settings.");
                        continue;
                    }

                    if (soakSetting.damageTypesToExclude != null &&
                        soakSetting.damageTypesToExclude.Contains(dinfo.Def))
                    {
                        DebugMessage($"c6c:: {dinfo.Def.label.CapitalizeFirst()} is to be excluded from damage soak.");
                        continue;
                    }
                    var dmgAmount = Mathf.Clamp(dinfo.Amount - soakSetting.damageToSoak, 0, dinfo.Amount);
                    DebugMessage($"c6c:: Min: 0, Max: {dinfo.Amount}. Calc: {dinfo.Amount} - {soakSetting.damageToSoak}.");
                    soakedDamage += (int)Mathf.Min(dinfo.Amount, soakSetting.damageToSoak);
                    DebugMessage($"c6c:: Soaked Running Total: {soakedDamage}");
                    dinfo.SetAmount(dmgAmount);
                    DebugMessage($"c6c:: Result: {dinfo.Amount}");
                    if (dinfo.Amount > 0)
                    {
                        DebugMessage($"c6c:: More damage exists. Continuing check for soakers.");
                        continue;
                    }
                    DamageSoakedMote(pawn, soakedDamage);
                    DebugMessage($"c6c:: Damage absorbed.");
                    DebugMessage($"c6c::   FINAL RESULT -- Soak: {soakedDamage} Damage: {dinfo.Amount}.");
                    DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyDamageSoakers ---");
                    absorbed = true;
                    return true;
                }
                else
                {
                    DebugMessage("c6c:: Soak Damage Hediff has damage soak settings.");
                    foreach (var soakSettings in soakSetting.settings)
                    {
                        DamageInfo info = dinfo;

                        DebugMessage($"c6c:: Hediff Damage: {info.Def.defName}");
                        if (soakSettings.damageType != null)
                            DebugMessage($"c6c:: Soak Type: {soakSettings.damageType.defName}");
                        else
                            DebugMessage($"c6c:: Soak Type: All");

                        //Null, here, means "all damage types"
                        //So Null should pass this check.
                        if (soakSettings.damageType != null && soakSettings.damageType != info.Def)
                        {
                            DebugMessage($"c6c:: No match. No soak.");
                            continue;
                        }

                        // ReSharper disable once PossibleNullReferenceException
                        if (!soakSettings.damageTypesToExclude.NullOrEmpty())
                        {
                            DebugMessage($"c6c:: Damage Soak Exlusions: ");
                            foreach (var exclusion in soakSettings.damageTypesToExclude)
                            {
                                DebugMessage($"c6c::    {exclusion.defName}");
                                if (exclusion == info.Def)
                                {
                                    DebugMessage($"c6c:: Exclusion match. Damage soak aborted.");
                                    continue;
                                }
                            }
                        }

                        var dmgAmount = Mathf.Clamp(dinfo.Amount - soakSettings.damageToSoak, 0, dinfo.Amount);
                        DebugMessage($"c6c:: Min: 0, Max: {dinfo.Amount}. Calc: {dinfo.Amount} - {soakSettings.damageToSoak}.");
                        soakedDamage += (int)Mathf.Min(soakSettings.damageToSoak, dinfo.Amount);
                        dinfo.SetAmount(dmgAmount);
                        DebugMessage($"c6c:: Result: {dinfo.Amount}");
                        DebugMessage($"c6c:: Total soaked: {soakedDamage}");
                        if (dinfo.Amount > 0)
                        {
                            DebugMessage($"c6c:: Unsoaked damage remains. Checking for more soakers.");
                            continue;
                        }
                        DamageSoakedMote(pawn, soakedDamage);
                        DebugMessage($"c6c:: Damage absorbed.");
                        DebugMessage($"c6c::  FINAL RESULT -- Soak: {soakedDamage} Damage: {dinfo.Amount}.");
                        DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyDamageSoakers ---");
                        absorbed = true;
                        return true;
                    }
                }
            }
            if (soakedDamage > 0)
            {
                DamageSoakedMote(pawn, soakedDamage);
                DebugMessage($"c6c::   FINAL RESULT -- Soak: {soakedDamage} Damage: {dinfo.Amount}.");
            }
            DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyDamageSoakers ---");
            absorbed = false;
            return false;
        }

        private static void DamageSoakedMote(Pawn pawn, int soakedDamage)
        {
            if (soakedDamage > 0 && pawn != null && pawn.Spawned && pawn.MapHeld != null &&
                pawn.DrawPos is Vector3 drawVecDos && drawVecDos.InBounds(pawn.MapHeld))
                MoteMaker.ThrowText(drawVecDos, pawn.MapHeld,
                    "JT_DamageSoaked".Translate(soakedDamage), -1f);
        }

        public static Vector3 PushResult(Thing Caster, Thing thingToPush, int pushDist, out bool collision)
        {
            var origin = thingToPush.TrueCenter();
            var result = origin;
            var collisionResult = false;
            for (var i = 1; i <= pushDist; i++)
            {
                var pushDistX = i;
                var pushDistZ = i;
                if (origin.x < Caster.TrueCenter().x) pushDistX = -pushDistX;
                if (origin.z < Caster.TrueCenter().z) pushDistZ = -pushDistZ;
                var tempNewLoc = new Vector3(origin.x + pushDistX, 0f, origin.z + pushDistZ);
                if (tempNewLoc.ToIntVec3().Standable(Caster.Map))
                {
                    result = tempNewLoc;
                }
                else
                {
                    if (thingToPush is Pawn)
                    {
                        //target.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Rand.Range(3, 6), -1, null, null, null));
                        collisionResult = true;
                        break;
                    }
                }
            }

            collision = collisionResult;
            return result;
        }

        public static void PushEffect(Thing Caster, Thing target, int distance, bool damageOnCollision = false)
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                if (target is Pawn p && p.Spawned && !p.Downed && !p.Dead && p.MapHeld != null)
                {
                    var loc = PushResult(Caster, target, distance, out var applyDamage);
                    //if (p.RaceProps.Humanlike) p.needs.mood.thoughts.memories.TryGainMemory(MiscDefOf.PJ_ThoughtPush, null);
                    var flyingObject = (FlyingObject)GenSpawn.Spawn(MiscDefOf.JT_FlyingObject, p.PositionHeld, p.MapHeld);
                    if (applyDamage && damageOnCollision)
                        flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target,
                            new DamageInfo(DamageDefOf.Blunt, Rand.Range(8, 10)));
                    else flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target);
                }
            }, "PushingCharacter", false, null);
        }

        //added 2018/12/13 - Mehni.
        //Uses CutoutComplex shader for apparel that wants it.
        //private static IEnumerable<CodeInstruction> CutOutComplexApparel_Transpiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    MethodInfo shader = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Shader));
        //    FieldInfo cutOut = AccessTools.Field(typeof(ShaderDatabase), nameof(ShaderDatabase.Cutout));

        //    foreach (CodeInstruction codeInstruction in instructions)
        //    {
        //        if (codeInstruction.opcode == OpCodes.Ldsfld && codeInstruction.operand == cutOut)
        //        {
        //            yield return new CodeInstruction(OpCodes.Ldarg_0); //apparel
        //            yield return new CodeInstruction(OpCodes.Call, shader); //return shader type
        //            continue; //skip instruction.
        //        }
        //        yield return codeInstruction;
        //    }
        //}

        //private static Shader Shader(Apparel apparel)
        //{
        //    if (apparel.def.graphicData.shaderType.Shader == ShaderDatabase.CutoutComplex)
        //        return ShaderDatabase.CutoutComplex;

        //    return ShaderDatabase.Cutout;
        //}
    }
}
