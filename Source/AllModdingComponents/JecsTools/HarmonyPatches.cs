using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AbilityUser;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
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
            var harmony = HarmonyInstance.Create("rimworld.jecrell.jecstools.main");
            //Allow fortitude to soak damage
            var type = typeof(HarmonyPatches);

            //Debug Line
            //------------
//            harmony.Patch(
//                AccessTools.Method(typeof(PawnGroupKindWorker_Normal),
//                    nameof(PawnGroupKindWorker_Normal.MinPointsToGenerateAnything)),
//                new HarmonyMethod(type, nameof(MinPointsTest)), null);
            //------------

            //Adds HediffCompProperties_DamageSoak checks to damage
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage)),
                new HarmonyMethod(type, nameof(PreApplyDamage_PrePatch)), null);

            //Applies cached armor damage and absorption
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"),
                new HarmonyMethod(type, nameof(ApplyProperDamage)), null);

            //Applies damage soak motes
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage)), null,
                new HarmonyMethod(type, nameof(Post_GetPostArmorDamage)));

            //Allows for adding additional HediffSets when characters spawn using the StartWithHediff class. 
            harmony.Patch(
                AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] {typeof(PawnGenerationRequest)}), null,
                new HarmonyMethod(type, nameof(Post_GeneratePawn)));

            //Checks apparel that uses the ApparelExtension
            harmony.Patch(AccessTools.Method(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether)), null,
                new HarmonyMethod(type, nameof(Post_CanWearTogether)));

            //Handles special cases of faction disturbances
            harmony.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_MemberDied)),
                new HarmonyMethod(type, nameof(Notify_MemberDied)), null);

            //Handles FactionSettings extension to allow for fun effects when factions arrive.
            harmony.Patch(
                AccessTools.Method(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns)), null,
                new HarmonyMethod(type, nameof(GeneratePawns)), null);

            //Handles cases where gendered apparel swaps out for individual genders.
            harmony.Patch(
                AccessTools.Method(typeof(PawnApparelGenerator),
                    nameof(PawnApparelGenerator.GenerateStartingApparelFor)), null,
                new HarmonyMethod(type, nameof(GenerateStartingApparelFor_PostFix)), null);

            //BuildingExtension prevents some things from wiping other things when spawned.
            harmony.Patch(
                AccessTools.Method(typeof(GenSpawn),
                    nameof(GenSpawn.SpawningWipes)), null,
                new HarmonyMethod(type, nameof(SpawningWipes_PostFix)), null);
            //BuildingExtension is also checked here to make sure things do not block construction.
            harmony.Patch(
                AccessTools.Method(typeof(GenConstruct),
                    nameof(GenConstruct.BlocksConstruction)), null,
                new HarmonyMethod(type, nameof(BlocksConstruction_PostFix)), null);
            //
            harmony.Patch(
                AccessTools.Method(typeof(Projectile),
                    "CanHit"), null,
                new HarmonyMethod(type, nameof(CanHit_PostFix)), null);
            harmony.Patch(
                AccessTools.Method(typeof(Verb),
                    "CanHitCellFromCellIgnoringRange"),
                new HarmonyMethod(type, nameof(CanHitCellFromCellIgnoringRange_Prefix)), null);

            //optionally use "CutoutComplex" shader for apparel that wants it
            harmony.Patch(AccessTools.Method(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel)), null, null, new HarmonyMethod(type, nameof(CutOutComplexApparel_Transpiler)));
        }

        //Added B19, Oct 2019
        //ProjectileExtension check
        //Allows a bullet to pass through walls when fired.
        public static bool CanHitCellFromCellIgnoringRange_Prefix(Verb __instance, IntVec3 sourceSq, IntVec3 targetLoc, bool includeCorners, ref bool __result)
        {
            try
            {

                if (__instance?.EquipmentCompSource?.PrimaryVerb?.verbProps?.defaultProjectile is ThingDef proj &&
                    proj?.HasModExtension<ProjectileExtension>() == true &&
                    proj?.GetModExtension<ProjectileExtension>() is ProjectileExtension ext)
                {
                    if (ext.passesWalls)
                        __result = true;
                    return false;
                }

            }
            catch (Exception e)
            {

            }
            return true;
        }

        //Added B19, Oct 2019
        //ProjectileExtension check
        //Ignores all structures as part of objects that disallow being fired through.
        public static void CanHit_PostFix(Projectile __instance, Thing thing, ref bool __result)
        {
            if (!__result && __instance?.def?.HasModExtension<ProjectileExtension>() == true &&
                __instance.def.GetModExtension<ProjectileExtension>() is ProjectileExtension ext)
            {
                //Mods will often have their own walls, so we cannot do a def check for 
                //ThingDefOf.Wall
                //Most "walls" should either be in the structure category or be able to hold walls.
                if (thing?.def?.designationCategory == DesignationCategoryDefOf.Structure ||
                    thing?.def?.holdsRoof == true)
                {
                    if (ext.passesWalls)
                    {
                        __result = false;
                        return;
                    }
                }
                
            }
        }

        public static void BlocksConstruction_PostFix(Thing constructible, Thing t, ref bool __result)
        {
            ThingDef thingDef = constructible.def;
            ThingDef thingDef2 = t.def;
            if (thingDef == null || thingDef2 == null)
                return;
            if (thingDef.HasModExtension<BuildingExtension>() || thingDef2.HasModExtension<BuildingExtension>())
            {
                BuildableDef buildableDef = GenConstruct.BuiltDefOf(thingDef);
                BuildableDef buildableDef2 = GenConstruct.BuiltDefOf(thingDef2);
                __result = ShouldWipe(buildableDef, buildableDef2, t.PositionHeld, t.MapHeld);
            }
        }

        public static void SpawningWipes_PostFix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
        {
            ThingDef thingDef = newEntDef as ThingDef;
            ThingDef thingDef2 = oldEntDef as ThingDef;
            if (thingDef == null || thingDef2 == null)
                return;
            if (thingDef.HasModExtension<BuildingExtension>() || thingDef2.HasModExtension<BuildingExtension>())
            {
                BuildableDef buildableDef = GenConstruct.BuiltDefOf(thingDef);
                BuildableDef buildableDef2 = GenConstruct.BuiltDefOf(thingDef2);
                __result = ShouldWipe(buildableDef, buildableDef2, IntVec3.Invalid, null);
            }
        }

        private static bool ShouldWipe(BuildableDef newEntDef, BuildableDef oldEntDef, IntVec3 loc, Map map)
        {
            if (map == null || loc == null || !loc.IsValid)
            {
                var buildingExtensionA = newEntDef?.GetModExtension<BuildingExtension>();
                var buildingExtensionB = oldEntDef?.GetModExtension<BuildingExtension>();
                if (buildingExtensionB == null && buildingExtensionA == null)
                {
                    //Log.Message("Both null");
                    return true;
                }

                //Log.Message("A: " + newEntDef.label);
                //Log.Message("B: " + oldEntDef.label);
                if (buildingExtensionA != null && buildingExtensionB == null &&
                    buildingExtensionA.wipeCategories?.Count > 0)
                {
                    //Log.Message("B null");

                    return false;
                }

                if (buildingExtensionB != null && buildingExtensionA == null &&
                    buildingExtensionB.wipeCategories?.Count > 0)
                {
                    //Log.Message("A null");

                    return false;
                }

                if (buildingExtensionA != null && buildingExtensionB != null &&
                    buildingExtensionA.wipeCategories?.Count > 0 &&
                    buildingExtensionB.wipeCategories?.Count > 0)
                {
                    var hashes = new HashSet<string>();
                    foreach (var str in buildingExtensionA.wipeCategories)
                        hashes.Add(str);
                    foreach (var strB in buildingExtensionB.wipeCategories)
                    {
                        if (!hashes.Contains(strB)) continue;
                        //Log.Message("ShouldWipe");
                        return true;
                    }
                }

                return true;
            }

            var locThings = loc.GetThingList(map);
            for (var index = 0; index < locThings.Count; index++)
            {
                var thing = locThings[index];
                if (thing.def is ThingDef thingDef &&
                    ShouldWipe(newEntDef, GenConstruct.BuiltDefOf(thingDef), IntVec3.Invalid, null))
                    return true;
            }

            return true;
        }

        public static void MinPointsTest(PawnGroupKindWorker_Normal __instance, PawnGroupMaker groupMaker)
        {
//            if (groupMaker?.options?.Count == null ||
//                groupMaker.options.Count <= 0)
//            {
//                Log.Message("No options available.");
//            }
//            foreach (var x in groupMaker.options)
//            {
//                Log.Message(x.kind.defName + " " + x.kind.isFighter.ToString() +  " " + x.Cost);
//            }
        }


        //PawnApparelGenerator
        public static void GenerateStartingApparelFor_PostFix(Pawn pawn, PawnGenerationRequest request)
        {
            var swappables = pawn?.apparel?.WornApparel?.FindAll(x => x.def.HasModExtension<ApparelExtension>());
            if (swappables == null || swappables?.Count <= 0) return;
            var destroyables = new HashSet<Apparel>();
            foreach (var swap in swappables)
            {
                if (swap.def?.GetModExtension<ApparelExtension>()?.swapCondition is SwapCondition sc &&
                    sc?.swapWhenGender is Gender gen &&
                    gen != Gender.None && gen == pawn.gender)
                {
                    Apparel apparel = (Apparel) ThingMaker.MakeThing(sc.swapTo, swap.Stuff);
                    PawnGenerator.PostProcessGeneratedGear(apparel, pawn);
                    if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
                        pawn.apparel.Wear(apparel, false);
                    }

                    destroyables.Add(swap);
                }
            }

            if (destroyables == null || destroyables?.Count <= 0) return;
            while (destroyables?.Count > 0)
            {
                var first = destroyables.First();
                first.Destroy();
                destroyables.Remove(first);
            }
        }

//
//        //PawnApparelGenerator
//        public static void IsNaked(Gender gender, ref bool __result)
//        {
//            if (!__result) return;
//            var aps = Traverse.Create(AccessTools.TypeByName("PossibleApparelSet")).Field("aps").GetValue<List<ThingStuffPair>>();
//            if (aps == null || aps?.Count <= 0) return;
//            for (int i = 0; i < aps.Count; i++)
//            {
//                if (!aps[i].thing.HasModExtension<ApparelExtension>())
//                    continue;
//                var aExt = aps[i].thing.GetModExtension<ApparelExtension>();
//                if (aExt.forcedGender == Gender.None)
//                    continue;
//                if (aExt.forcedGender == gender) continue;
//                __result = false;
//                return;
//            }
//        }

        public static Faction lastPhoneAideFaction = null;
        public static int lastPhoneAideTick = 0;

        //public class PawnGroupMakerUtility
        //{
        public static void GeneratePawns(PawnGroupMakerParms parms,
            bool warnOnZeroResults, ref IEnumerable<Pawn> __result)
        {
            if (__result?.Count() > 0 &&
                parms.faction.def.GetModExtension<FactionSettings>() is FactionSettings settings)
            {
                settings.entrySoundDef?.PlayOneShotOnCamera();
            }
        }

        //Faction
        public static bool Notify_MemberDied(Faction __instance, Pawn member, DamageInfo? dinfo, bool wasWorldPawn,
            Map map)
        {
            //Log.Message("1");
            if (member?.Faction == null) return true;
            if (!dinfo.HasValue) return true;
            if (!(dinfo.Value.Instigator is Pawn instigator)) return true;
            //Log.Message("2");


            var notLeader = __instance?.leader != member;
            //Log.Message("3");

            var notPlayerKiller = instigator?.Faction != Faction.OfPlayerSilentFail;
            //Log.Message("4");

            //var notAttackingPlayer = member.LastAttackedTarget.IsValid && member?.LastAttackedTarget.Thing is Pawn p && p?.Faction != Faction.OfPlayerSilentFail;
            //Log.Message("5");

            var inTime = lastPhoneAideTick < (Find.TickManager?.TicksGame + GenDate.HoursPerDay ?? 0);
            //Log.Message("6");


            var isPhoneFaction = __instance == lastPhoneAideFaction;
            //Log.Message("7");


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
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="body"></param>
        /// <param name="__result"></param>
        public static void Post_CanWearTogether(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            try
            {
                if (A == null || B == null || body == null || __result == true) return;
                var aHasExt = A.HasModExtension<ApparelExtension>();
                var bHasExt = B.HasModExtension<ApparelExtension>();
                if (aHasExt && bHasExt)
                {
                    var aExt = A.GetModExtension<ApparelExtension>();
                    var bExt = B.GetModExtension<ApparelExtension>();
                    var check = new Dictionary<string, int>();
                    if (aExt.coverage?.Count > 0)
                        for (int i = 0; i < aExt.coverage.Count; i++)
                        {
                            if (!check.ContainsKey(aExt.coverage[i]))
                                check.Add(aExt.coverage[i].ToLowerInvariant(), 1);
                            else
                            {
                                Log.Warning("JecsTools :: ApparelExtension :: Warning:: " + A.label +
                                            " has multiple of the same tags.");
                                return;
                            }
                        }

                    if (bExt.coverage?.Count > 0)
                        for (int j = 0; j < bExt.coverage.Count; j++)
                        {
                            if (!check.ContainsKey(bExt.coverage[j]))
                                check.Add(bExt.coverage[j].ToLowerInvariant(), 1);
                            else
                            {
                                __result = false;
                                break;
                            }
                        }
                }
                else if ((aHasExt && !bHasExt) || (!aHasExt && bHasExt))
                {
                    __result = true;
                }
            }
            catch (Exception e)
            {
                Log.Message(e.ToString());
            }
        }

        public static void Post_GeneratePawn(PawnGenerationRequest request, ref Pawn __result)
        {
            var hediffGiverSet = __result?.def?.race?.hediffGiverSets?.FirstOrDefault(
                x => x.hediffGivers.Any(y => y is HediffGiver_StartWithHediff));
            if (hediffGiverSet == null) return;

            if (hediffGiverSet.hediffGivers.FirstOrDefault(x => x is HediffGiver_StartWithHediff) is
                HediffGiver_StartWithHediff hediffGiver)
            {
                hediffGiver.GiveHediff(__result);
            }
        }

        //ArmorUtility
        public static void Post_GetPostArmorDamage(Pawn pawn, float amount, BodyPartRecord part, DamageDef damageDef,
            ref float __result)
        {
            if (tempDamageAbsorbed != null)
            {
                var hasFortitudeHediffs =
                    pawn?.health?.hediffSet?.hediffs?.Any(x => x.TryGetComp<HediffComp_DamageSoak>() != null);
                if (hasFortitudeHediffs ?? false)
                {
                    DamageSoakedMote(pawn, tempDamageAbsorbed.Value);
                }

                tempDamageAbsorbed = null;
            }
        }

        public static void ApplyProperDamage(ref float damAmount, float armorRating, Thing armorThing,
            DamageDef damageDef, Pawn pawn, ref bool metalArmor)
        {
            if (tempDamageAmount != null && damAmount > 0)
            {
                float damageDiff = Mathf.Clamp(damAmount - tempDamageAmount.Value, 0, damAmount);

                //Log.Message("Apply amount original: " + damAmount);
                //Log.Message("Apply amount modified: " + tempDamageAmount.Value);
                damAmount = GenMath.RoundRandom(tempDamageAmount.Value);
                tempDamageAmount = null;
                if (damageDiff > 0)
                    tempDamageAbsorbed = GenMath.RoundRandom(damageDiff);
            }
        }

        public static bool PreApplyDamage_PrePatch(Pawn_HealthTracker __instance, ref DamageInfo dinfo,
            out bool absorbed)
        {
            var pawn = (Pawn) AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            //Log.Message("Entry");
            if (pawn != null && !StopPreApplyDamageCheck)
            {
                //Log.Message("0");
                if (pawn?.health?.hediffSet?.hediffs != null && pawn?.health?.hediffSet?.hediffs?.Count > 0)
                {
                    //Log.Message("1");
                    //A list will stack.
                    var fortitudeHediffs =
                        pawn?.health?.hediffSet?.hediffs?.FindAll(x => x.TryGetComp<HediffComp_DamageSoak>() != null);
                    if (!fortitudeHediffs.NullOrEmpty())
                    {
                        //Log.Message("2");
                        try
                        {
                            if (PreApplyDamage_ApplyDamageSoakers(ref dinfo, out absorbed, fortitudeHediffs, pawn))
                                return false;
                        }
                        catch (NullReferenceException e)
                        {
                        }
                    }

                    if (dinfo.Weapon is ThingDef weaponDef && !weaponDef.IsRangedWeapon)
                        if (dinfo.Instigator is Pawn instigator)
                        {
                            try
                            {
                                if (PreApplyDamage_ApplyExtraDamages(out absorbed, instigator, pawn)) return false;
                            }
                            catch (NullReferenceException e)
                            {
                            }

                            try
                            {
                                PreApplyDamage_ApplyKnockback(instigator, pawn);
                            }
                            catch (NullReferenceException e)
                            {
                            }
                        }
                }
            }

            tempDamageAmount = (int) dinfo.Amount;
            absorbed = false;
            //Log.Message("Current Damage :" + dinfo.Amount);
            return true;
        }

        private static void PreApplyDamage_ApplyKnockback(Pawn instigator, Pawn pawn)
        {
            var knockbackHediff =
                instigator?.health?.hediffSet?.hediffs.FirstOrDefault(y =>
                    y.TryGetComp<HediffComp_Knockback>() != null);
            var knocker = knockbackHediff?.TryGetComp<HediffComp_Knockback>();
            if (knocker != null)
                if (knocker?.Props?.knockbackChance >= Rand.Value)
                {
                    if (knocker.Props.explosiveKnockback)
                    {
                        var explosion = (Explosion) GenSpawn.Spawn(ThingDefOf.Explosion,
                            instigator.PositionHeld, instigator.MapHeld);
                        explosion.radius = knocker.Props.explosionSize;
                        explosion.damType = knocker.Props.explosionDmg;
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
                        explosion.StartExplosion(null);
                    }

                    if (pawn != instigator && !pawn.Dead && !pawn.Downed && pawn.Spawned)
                    {
                        if (knocker.Props.stunChance > -1 && knocker.Props.stunChance >= Rand.Value)
                            pawn.stances.stunner.StunFor(knocker.Props.stunTicks, instigator);
                        PushEffect(instigator, pawn, knocker.Props.knockDistance.RandomInRange,
                            true);
                    }
                }
        }

        private static bool PreApplyDamage_ApplyExtraDamages(out bool absorbed, Pawn instigator, Pawn pawn)
        {
            var extraDamagesHediff =
                instigator.health.hediffSet.hediffs.FirstOrDefault(y =>
                    y.TryGetComp<HediffComp_ExtraMeleeDamages>() != null);
            var damages = extraDamagesHediff?.TryGetComp<HediffComp_ExtraMeleeDamages>();
            if (damages?.Props != null && !damages.Props.extraDamages.NullOrEmpty())
            {
                StopPreApplyDamageCheck = true;
                foreach (var dmg in damages.Props.extraDamages)
                {
                    if (pawn == null || !pawn.Spawned || pawn.Dead)
                    {
                        absorbed = false;
                        StopPreApplyDamageCheck = false;
                        return true;
                    }

                    pawn.TakeDamage(new DamageInfo(dmg.def, dmg.amount, dmg.armorPenetration, -1, instigator));
                }

                StopPreApplyDamageCheck = false;
            }

            absorbed = false;
            return false;
        }

        private static bool PreApplyDamage_ApplyDamageSoakers(ref DamageInfo dinfo, out bool absorbed,
            List<Hediff> fortitudeHediffs,
            Pawn pawn)
        {
            //Log.Message("3");

            var soakedDamage = 0;
            foreach (var fortitudeHediff in fortitudeHediffs)
            {
                //Log.Message("Hediff");

                var soaker = fortitudeHediff.TryGetComp<HediffComp_DamageSoak>();
                var soakSetting = soaker?.Props;
                if (soakSetting == null) continue;
                if (soakSetting.settings.NullOrEmpty())
                {
                    //Log.Message("Hediff_A1");
                    //Null, here, means "all damage types"
                    //So Null should pass this check.
                    if (soakSetting.damageType != null && soakSetting.damageType != dinfo.Def) continue;

                    //Log.Message("Hediff_A2");

                    if (!soakSetting.damageTypesToExclude.NullOrEmpty() &&
                        soakSetting.damageTypesToExclude.Contains(dinfo.Def))
                        continue;
                    //Log.Message("Hediff_A3");
                    var dmgAmount = Mathf.Clamp(dinfo.Amount - soakSetting.damageToSoak, 0, dinfo.Amount);
                    //Log.Message(dinfo.Amount + " - " + soakSetting.damageToSoak + " = " + dmgAmount);
                    dinfo.SetAmount(dmgAmount);
                    //Log.Message("New damage amt: " + dinfo.Amount);
                    soakedDamage += (int) dmgAmount;
                    if (dinfo.Amount > 0) continue;
                    //Log.Message("Hediff_A_Absorbed");
                    DamageSoakedMote(pawn, soakedDamage);
                    absorbed = true;
                    return true;
                }
                else
                {
                    //Log.Message("Hediff_B1");
                    foreach (var soakSettings in soaker.Props.settings)
                    {
                        DamageInfo info = dinfo;
                        //Log.Message("Hediff_B1_Setting");

                        //Log.Message("Hediff Damage: " + info.Def.defName);
                        //Null, here, means "all damage types"
                        //So Null should pass this check.
                        if (soakSettings.damageType != null && soakSettings.damageType != info.Def) continue;
                        //Log.Message("Hediff_B1_Setting1");

                        // ReSharper disable once PossibleNullReferenceException
                        if (!soakSettings.damageTypesToExclude.NullOrEmpty() &&
                            soakSettings.damageTypesToExclude.Any(x => x == info.Def))
                            continue;
                        //Log.Message("Hediff_B1_Setting2");

                        var dmgAmount = Mathf.Clamp(dinfo.Amount - soakSettings.damageToSoak, 0, dinfo.Amount);
                        //Log.Message(dinfo.Amount + " - " + soakSettings.damageToSoak + " = " + dmgAmount);
                        soakedDamage += (int) dmgAmount;
                        dinfo.SetAmount(dmgAmount);
                        //Log.Message("New damage amt: " + dinfo.Amount);
                        //Log.Message("Total soaked: " + soakedDamage);
                        if (dinfo.Amount > 0) continue;
                        //Log.Message("Hediff_B_Setting_Absorbed");
                        DamageSoakedMote(pawn, soakedDamage);
                        absorbed = true;
                        return true;
                    }
                }
            }

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
                if (target != null && target is Pawn p && p.Spawned && !p.Downed && !p.Dead && p?.MapHeld != null)
                {
                    bool applyDamage;
                    var loc = PushResult(Caster, target, distance, out applyDamage);
                    //if (((Pawn)target).RaceProps.Humanlike) ((Pawn)target).needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PJ_ThoughtPush"), null);
                    var flyingObject = (FlyingObject) GenSpawn.Spawn(ThingDef.Named("JT_FlyingObject"), p.PositionHeld,
                        p.MapHeld);
                    if (applyDamage && damageOnCollision)
                        flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target,
                            new DamageInfo(DamageDefOf.Blunt, Rand.Range(8, 10)));
                    else flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target);
                }
            }, "PushingCharacter", false, null);
        }

        //added 2018/12/13 - Mehni.
        //Uses CutoutComplex shader for apparel that wants it.
        private static IEnumerable<CodeInstruction> CutOutComplexApparel_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo shader = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Shader));
            FieldInfo cutOut = AccessTools.Field(typeof(ShaderDatabase), nameof(ShaderDatabase.Cutout));

            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Ldsfld && codeInstruction.operand == cutOut)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //apparel
                    yield return new CodeInstruction(OpCodes.Call, shader); //return shader type
                    continue; //skip instruction.
                }
                yield return codeInstruction;
            }
        }

        private static Shader Shader (Apparel apparel)
        {
            if (apparel.def.graphicData.shaderType.Shader == ShaderDatabase.CutoutComplex)
                return ShaderDatabase.CutoutComplex;

            return ShaderDatabase.Cutout;
        }
    }
}