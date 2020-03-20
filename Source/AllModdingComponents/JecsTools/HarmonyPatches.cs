using System;
using System.Collections.Generic;
using System.Linq;
using AbilityUser;
using HarmonyLib;
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
        public static bool DEBUGMODE = false;

        //For alternating fire on some weapons
        public static Dictionary<Thing, int> AlternatingFireTracker = new Dictionary<Thing, int>();

        // Verse.Pawn_HealthTracker
        public static bool StopPreApplyDamageCheck;

        public static int? tempDamageAmount = null;
        public static int? tempDamageAbsorbed = null;

        static HarmonyPatches()
        {
            // Changed by Tad : New Harmony Instance creation required
            var instance = new Harmony("jecstools.jecrell.main");
            //Allow fortitude to soak damage
            var type = typeof(HarmonyPatches);

            //Debug Line
            //------------
//            instance.Patch(
//                AccessTools.Method(typeof(PawnGroupKindWorker_Normal),
//                    nameof(PawnGroupKindWorker_Normal.MinPointsToGenerateAnything)),
//                new HarmonyMethod(type, nameof(MinPointsTest)), null);
            //------------

            //Adds HediffCompProperties_DamageSoak checks to damage
            instance.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage)),
                new HarmonyMethod(type, nameof(PreApplyDamage_PrePatch)), null);

            //Applies cached armor damage and absorption
            instance.Patch(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"),
                new HarmonyMethod(type, nameof(ApplyProperDamage)), null);

            //Applies damage soak motes
            instance.Patch(AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage)), null,
                new HarmonyMethod(type, nameof(Post_GetPostArmorDamage)));

            //Allows for adding additional HediffSets when characters spawn using the StartWithHediff class. 
            instance.Patch(
                AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] {typeof(PawnGenerationRequest)}), null,
                new HarmonyMethod(type, nameof(Post_GeneratePawn)));

            //Checks apparel that uses the ApparelExtension
            instance.Patch(AccessTools.Method(typeof(ApparelUtility), nameof(ApparelUtility.CanWearTogether)), null,
                new HarmonyMethod(type, nameof(Post_CanWearTogether)));

            //Handles special cases of faction disturbances
            instance.Patch(AccessTools.Method(typeof(Faction), nameof(Faction.Notify_MemberDied)),
                new HarmonyMethod(type, nameof(Notify_MemberDied)), null);

            //Handles FactionSettings extension to allow for fun effects when factions arrive.
            instance.Patch(
                AccessTools.Method(typeof(PawnGroupMakerUtility), nameof(PawnGroupMakerUtility.GeneratePawns)), null,
                new HarmonyMethod(type, nameof(GeneratePawns)), null);

            //Handles cases where gendered apparel swaps out for individual genders.
            instance.Patch(
                AccessTools.Method(typeof(PawnApparelGenerator),
                    nameof(PawnApparelGenerator.GenerateStartingApparelFor)), null,
                new HarmonyMethod(type, nameof(GenerateStartingApparelFor_PostFix)), null);

            //BuildingExtension prevents some things from wiping other things when spawned.
            instance.Patch(
                AccessTools.Method(typeof(GenSpawn),
                    nameof(GenSpawn.SpawningWipes)), null,
                new HarmonyMethod(type, nameof(SpawningWipes_PostFix)), null);
            //BuildingExtension is also checked here to make sure things do not block construction.
            instance.Patch(
                AccessTools.Method(typeof(GenConstruct),
                    nameof(GenConstruct.BlocksConstruction)), null,
                new HarmonyMethod(type, nameof(BlocksConstruction_PostFix)), null);
            //
            instance.Patch(
                AccessTools.Method(typeof(Projectile),
                    "CanHit"), null,
                new HarmonyMethod(type, nameof(CanHit_PostFix)), null);
            instance.Patch(
                AccessTools.Method(typeof(Verb),
                    "CanHitCellFromCellIgnoringRange"),
                new HarmonyMethod(type, nameof(CanHitCellFromCellIgnoringRange_Prefix)), null);

            //optionally use "CutoutComplex" shader for apparel that wants it
            //instance.Patch(AccessTools.Method(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel)), null, null, new HarmonyMethod(type, nameof(CutOutComplexApparel_Transpiler)));
        }

        public static void DebugMessage(string s)
        {
            if (DEBUGMODE)
                Log.Message(s);
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
                    var check = new HashSet<string>();
                    if (aExt.coverage?.Count > 0)
                        for (int i = 0; i < aExt.coverage.Count; i++)
                        {
                            var coverageItem = aExt.coverage[i].ToLowerInvariant();
                            if (!check.Contains(coverageItem))
                                check.Add(coverageItem);
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
                            var coverageItem = bExt.coverage[j].ToLowerInvariant();
                            if (!check.Contains(coverageItem))
                                check.Add(coverageItem);
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

            DebugMessage($"c6c:: === Enter Harmony Prefix --- PreApplyDamage_ApplyExtraDamages ===");

            var pawn = (Pawn) AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            if (pawn != null && !StopPreApplyDamageCheck)
            {
                DebugMessage("c6c:: Pawn exists. StopPreApplyDamageCheck: False");
                if (pawn?.health?.hediffSet?.hediffs != null && pawn?.health?.hediffSet?.hediffs?.Count > 0)
                {
                    DebugMessage("c6c:: Pawn has health.");
                    //A list will stack.
                    var fortitudeHediffs =
                        pawn?.health?.hediffSet?.hediffs?.FindAll(x => x.TryGetComp<HediffComp_DamageSoak>() != null);
                    if (!fortitudeHediffs.NullOrEmpty())
                    {
                        DebugMessage("c6c:: Pawn has Damage Soak hediff.");
                        try
                        {
                            if (PreApplyDamage_ApplyDamageSoakers(ref dinfo, out absorbed, fortitudeHediffs, pawn))
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
                                if (PreApplyDamage_ApplyExtraDamages(dinfo, out absorbed, instigator, pawn)) return false;
                            }
                            catch (NullReferenceException e)
                            {
                                DebugMessage($"c6c:: Extra damages failure:: {e.Message}");
                            }

                            try
                            {
                                PreApplyDamage_ApplyKnockback(instigator, pawn);
                            }
                            catch (NullReferenceException e)
                            {
                                DebugMessage($"c6c:: Apply knockback failure:: {e.Message}");
                            }
                        }
                }
            }

            tempDamageAmount = (int) dinfo.Amount;
            absorbed = false;
            DebugMessage($"c6c:: === Exit Harmony Prefix --- PreApplyDamage_ApplyExtraDamages ===");
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
                        explosion.StartExplosion(null,null);
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

        private static bool PreApplyDamage_ApplyExtraDamages(DamageInfo dinfo, out bool absorbed, Pawn instigator, Pawn pawn)
        {

            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyExtraDamages ---");
            var extraDamagesHediff =
                instigator.health.hediffSet.hediffs.FirstOrDefault(y =>
                    y.TryGetComp<HediffComp_ExtraMeleeDamages>() != null);
            DebugMessage("c6c:: ExtraDamagesHediff variable assigned.");
            var damages = extraDamagesHediff?.TryGetComp<HediffComp_ExtraMeleeDamages>();
            DebugMessage("c6c:: Damages variable assigned.");
            if (damages?.Props != null && damages.Props.ExtraDamages is List<Verse.ExtraDamage> extraDamages)
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
            List<Hediff> fortitudeHediffs,
            Pawn pawn)
        {

            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyDamageSoakers ---");
            var soakedDamage = 0;
            foreach (var fortitudeHediff in fortitudeHediffs)
            {
                DebugMessage("c6c:: Soak Damage Hediff checked.");

                var soaker = fortitudeHediff.TryGetComp<HediffComp_DamageSoak>();
                var soakSetting = soaker?.Props;
                if (soakSetting == null) {
                    DebugMessage("c6c:: Soak Damage Hediff has no damage soak XML properties.");
                    continue; }
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

                    
                    if (!soakSetting.damageTypesToExclude.NullOrEmpty() &&
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
                    foreach (var soakSettings in soaker.Props.settings)
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
     // private static IEnumerable<CodeInstruction> CutOutComplexApparel_Transpiler(IEnumerable<CodeInstruction> instructions)
     // {
     //    MethodInfo shader = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Shader));
     //       FieldInfo cutOut = AccessTools.Field(typeof(ShaderDatabase), nameof(ShaderDatabase.Cutout));
     //
     //      foreach (CodeInstruction codeInstruction in instructions)
     //      {
     //          if (codeInstruction.opcode == OpCodes.Ldsfld && codeInstruction.operand == cutOut)
     //          {
     //              yield return new CodeInstruction(OpCodes.Ldarg_0); //apparel
     //              yield return new CodeInstruction(OpCodes.Call, shader); //return shader type
       //             continue; //skip instruction.
       //         }
       //         yield return codeInstruction;
       //     }
       // }

      //  private static Shader Shader (Apparel apparel)
      //  {
      //      if (apparel.def.graphicData.shaderType.Shader == ShaderDatabase.CutoutComplex)
      //          return ShaderDatabase.CutoutComplex;
      //
      //      return ShaderDatabase.Cutout;
      //  }
    }
}