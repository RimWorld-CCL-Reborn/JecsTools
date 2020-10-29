//#define DEBUGLOG

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

            //Allow fortitude (HediffComp_DamageSoak) to soak damage
            //Adds HediffCompProperties_DamageSoak checks to damage
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage)),
                prefix: new HarmonyMethod(type, nameof(PreApplyDamage_PrePatch)));
            //Applies cached armor damage and absorption
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"),
                prefix: new HarmonyMethod(type, nameof(Pre_ApplyArmor)));
            //Applies damage soak motes
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage)),
                postfix: new HarmonyMethod(type, nameof(Post_GetPostArmorDamage)));

            //Allows for adding additional HediffSets when characters spawn using the StartWithHediff class.
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn),
                    new[] { typeof(PawnGenerationRequest) }),
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
            if (__instance.EquipmentCompSource?.PrimaryVerb?.verbProps?.defaultProjectile?.GetProjectileExtension() is ProjectileExtension ext)
            {
                if (ext.passesWalls)
                    __result = true;
                return false;
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
                //Mods will often have their own walls, so we cannot do a def check for ThingDefOf.Wall
                //Most "walls" should either be in the structure category or be able to hold walls.
                if (thing?.def is ThingDef def)
                    if (def.designationCategory == DesignationCategoryDefOf.Structure ||
                        def.holdsRoof)
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
            if (allWornApparel.NullOrEmpty())
                return;
            List<(Apparel, Apparel)> swapEntries = null;
            foreach (var wornApparel in allWornApparel)
            {
                if (wornApparel.def?.GetApparelExtension()?.swapCondition is SwapCondition sc &&
                    sc.swapWhenGender is Gender gen &&
                    gen != Gender.None && gen == pawn.gender)
                {
                    var swapApparel = (Apparel)ThingMaker.MakeThing(sc.swapTo, wornApparel.Stuff);
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
                        DebugMessage($"apparel generation for {pawn}: swapped from {wornApparel} to {swapApparel}");
                    }
                    wornApparel.Destroy();
                    DebugMessage($"apparel generation for {pawn}: destroyed old {wornApparel}");
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
            if (member?.Faction == null)
                return true;
            if (!(dinfo.HasValue && dinfo.Value.Instigator is Pawn instigator))
                return true;

            var notLeader = __instance.leader != member;

            var notPlayerKiller = instigator.Faction != Faction.OfPlayerSilentFail;

            //var notAttackingPlayer = member.LastAttackedTarget.IsValid && member.LastAttackedTarget.Thing is Pawn p && p.Faction != Faction.OfPlayerSilentFail;

            var inTime = lastPhoneAideTick < (Find.TickManager?.TicksGame + GenDate.HoursPerDay ?? 0);

            var isPhoneFaction = __instance == lastPhoneAideFaction;

            return !isPhoneFaction || !inTime || !notLeader || !notPlayerKiller; //|| !notAttackingPlayer
        }

        /// <summary>
        /// Using the new ApparelExtension, we can have a string based apparel check.
        /// </summary>
        public static void Post_CanWearTogether(ThingDef A, ThingDef B, BodyDef body, ref bool __result)
        {
            static HashSet<string> GetCoverage(ThingDef thingDef)
            {
                var coverage = thingDef.GetApparelExtension()?.Coverage;
                return coverage == null || coverage.Count == 0 ? null : coverage;
            }

            if (A == null || B == null || body == null || __result == true)
                return;
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

        public static void Post_GeneratePawn(Pawn __result)
        {
            if (__result?.def?.race?.hediffGiverSets?
                .SelectMany(x => x.hediffGivers.Where(y => y is HediffGiver_StartWithHediff))
                .FirstOrDefault() is HediffGiver_StartWithHediff hediffGiver)
            {
                hediffGiver.GiveHediff(__result);
            }
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
        // 5) Extra damages are also recorded in tempDamageAmount, which can unnecessarily trigger the above behavior,
        //    although this is also necessary to avoid a stale tempDamageAmount being used in these patches.
        // 6) The relationship of PreApplyDamage_PrePatch and this patch with respect to tempDamageAmount is fragile,
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
                // TODO: tempDamageAmount is an integer - so RoundRandom has no effect (other than float conversion).
                // Shouldn't tempDamageAmount be a float?
                var newDamAmount = GenMath.RoundRandom(tempDamageAmount.Value);
                DebugMessage($"c6c:: ApplyArmor prefix on {pawn}: tempDamageAmount {tempDamageAmount} => null, damAmount {damAmount} => {newDamAmount}");
                damAmount = newDamAmount;
                tempDamageAmount = null;
                if (damageDiff > 0f)
                    tempDamageAbsorbed = GenMath.RoundRandom(damageDiff);
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

        public static bool PreApplyDamage_PrePatch(Pawn ___pawn, ref DamageInfo dinfo, out bool absorbed)
        {
            DebugMessage($"c6c:: === Enter Harmony Prefix --- PreApplyDamage_PrePatch for {___pawn} and {dinfo} ===");
            if (___pawn != null && !StopPreApplyDamageCheck)
            {
                DebugMessage("c6c:: Pawn exists. StopPreApplyDamageCheck: False");
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

                    // Since this is a Pawn_HealthTracker.PreApplyDamage prefix patch, applying extra damage and knockback
                    // only happens if no ThingComp.PostPreApplyDamage's (or earlier run patches) set the absorbed flag,
                    // and it happens before any Apparel.CheckPreAbsorbDamage could set the absorbed flag, so e.g.
                    // extra damage and knockback are applied regardless of a shield belt potentially setting absorbed flag.
                    // TODO: Should these be applied before any possible absorbed flag (in a Pawn.PreApplyDamage prefix patch),
                    // only after it's definitely not absorbed (in a Pawn_HealthTracker.PreApplyDamage postfix patch),
                    // or retain the existing behavior (this Pawn_HealthTracker.PreApplyDamage prefix patch)?
                    if (dinfo.Weapon is ThingDef weaponDef && !weaponDef.IsRangedWeapon &&
                        dinfo.Instigator is Pawn instigator)
                    {
                        DebugMessage("c6c:: Pawn has non-ranged weapon.");
                        if (PreApplyDamage_ApplyExtraDamages(instigator, ___pawn))
                        {
                            absorbed = false;
                            return false;
                        }
                        PreApplyDamage_ApplyKnockback(instigator, ___pawn);
                    }
                }
            }

            // TODO: tempDamageAmount shouldn't be set if there are no damage soaks (and no extra damages?).
            tempDamageAmount = (int)dinfo.Amount;
            DebugMessage($"c6c:: tempDamageAmount <= {tempDamageAmount}");
            absorbed = false;
            DebugMessage($"c6c:: === Exit Harmony Prefix --- PreApplyDamage_PrePatch for {___pawn} and {dinfo} ===");
            return true;
        }

        private static void PreApplyDamage_ApplyKnockback(Pawn instigator, Pawn pawn)
        {
            var knockerProps = instigator.GetHediffComp<HediffComp_Knockback>()?.Props;
            if (knockerProps != null)
                if (knockerProps.knockbackChance >= Rand.Value)
                {
                    if (knockerProps.explosiveKnockback)
                    {
                        // TODO: Use GenExplosion.DoExplosion instead?
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
                        explosion.damageFalloff = false;
                        explosion.StartExplosion(knockerProps.knockbackSound, null);
                    }

                    if (pawn != instigator && !pawn.Dead && !pawn.Downed && pawn.Spawned)
                    {
                        if (knockerProps.stunChance > -1 && knockerProps.stunChance >= Rand.Value)
                            pawn.stances.stunner.StunFor(knockerProps.stunTicks, instigator);
                        PushEffect(instigator, pawn, knockerProps.knockDistance.RandomInRange, damageOnCollision: true);
                    }
                }
        }

        private static bool PreApplyDamage_ApplyExtraDamages(Pawn instigator, Pawn pawn)
        {
            // TODO: This should be a patch on Verb_MeleeAttackDamage to have consistent behavior with vanilla usage
            // of ExtraDamage (in Bullet and Verb_MeleeAttackDamage), including vanilla damage & armor penetration
            // calculations, combat log association, and same interaction with damage soaks.
            // TODO: Support multiple HediffComp_ExtraMeleeDamages on the same instigator?
            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyExtraDamages for {pawn} ---");
            var extraDamagesComp = instigator.GetHediffComp<HediffComp_ExtraMeleeDamages>();
            DebugMessage("c6c:: ExtraDamagesComp variable assigned.");
            if (extraDamagesComp?.Props?.ExtraDamages is List<Verse.ExtraDamage> extraDamages)
            {
                DebugMessage("c6c:: Extra damages list exists.");
                // This flag prevents both infinite recursion (Thing.TakeDamage => PreApplyDamage_PrePatch)
                // (It also prevents damage soaks from being applied to these extra damages, which is inconsistent
                // with vanilla extra damage.)
                StopPreApplyDamageCheck = true;
                foreach (var dmg in extraDamages)
                {
                    DebugMessage($"c6c:: Extra Damage: {dmg.def}");
                    if (pawn == null || !pawn.Spawned || pawn.Dead)
                    {
                        DebugMessage($"c6c:: Pawn is null, unspawned, or dead. Aborting.");
                        StopPreApplyDamageCheck = false;
                        return true;
                    }

                    //var battleLogEntry_MeleeCombat = new BattleLogEntry_MeleeCombat(damageDef.combatLogRules, true,
                    //    instigator, pawn, ImplementOwnerTypeDefOf.Bodypart, (dinfo.Weapon != null) ? dinfo.Weapon.label : damageDef.label);
                    //DebugMessage($"c6c:: MeleeCombat Log generated.");
                    //DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
                    //DebugMessage($"c6c:: MeleeCombat Damage Result generated.");
                    //damageResult = pawn.TakeDamage(new DamageInfo(dmg.def, dmg.amount, dmg.armorPenetration, -1, instigator));
                    var extraDinfo = new DamageInfo(dmg.def, dmg.amount, dmg.armorPenetration, -1, instigator);
                    DebugMessage($"c6c:: MeleeCombat ExtraDamage dinfo: {extraDinfo}");
                    pawn.TakeDamage(extraDinfo);
                    //damageResult.AssociateWithLog(battleLogEntry_MeleeCombat);
                    //DebugMessage($"c6c:: MeleeCombat Damage associated with log.");
                    //battleLogEntry_MeleeCombat.def = LogEntryDefOf.MeleeAttack;
                    //DebugMessage($"c6c:: MeleeCombat Log def set as MeleeAttack.");
                    //Find.BattleLog.Add(battleLogEntry_MeleeCombat);
                    //DebugMessage($"c6c:: MeleeCombat Log added to battle log.");
                }

                StopPreApplyDamageCheck = false;
            }
            DebugMessage($"c6c:: --- Exit PreApplyDamage_ApplyExtraDamages for {pawn} ---");
            return false;
        }

        private static bool PreApplyDamage_ApplyDamageSoakers(ref DamageInfo dinfo, HediffSet hediffSet, Pawn pawn)
        {
            // Multiple damage soak hediff comps stack.
            DebugMessage($"c6c:: --- Enter PreApplyDamage_ApplyDamageSoakers for {pawn} and {dinfo} ---");
            var damageDef = dinfo.Def;
            var totalSoakedDamage = 0;
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
                    // TODO: Don't int-truncate here, and instead Mathf.RoundToInt or GenMath.RoundRandom when displaying it.
                    totalSoakedDamage += (int)soakedDamage;
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

                        if (!soakSettings.damageTypesToExclude.NullOrEmpty())
                        {
                            DebugMessage($"c6c:: Damage Soak Exlusions: ");
                            foreach (var exclusion in soakSettings.damageTypesToExclude)
                            {
                                DebugMessage($"c6c::    {exclusion}");
                                if (exclusion == damageDef)
                                {
                                    DebugMessage($"c6c:: Exclusion match. Damage soak aborted.");
                                    continue;
                                }
                            }
                        }

                        var dmgAmount = dinfo.Amount;
                        var soakedDamage = Mathf.Min(soakSettings.damageToSoak, dmgAmount);
                        DebugMessage($"c6c:: Soaked: Min({soakSettings.damageToSoak}, {dinfo.Amount}) => {soakedDamage}");
                        dmgAmount -= soakedDamage;
                        DebugMessage($"c6c:: Damage amount: {dinfo.Amount} - {soakedDamage} => {dmgAmount}");
                        // TODO: Don't int-truncate here, and instead Mathf.RoundToInt or GenMath.RoundRandom when displaying it.
                        totalSoakedDamage += (int)soakedDamage;
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

        private static void DamageSoakedMote(Pawn pawn, int soakedDamage)
        {
            if (soakedDamage > 0 && pawn != null && pawn.Spawned && pawn.MapHeld != null &&
                pawn.DrawPos is Vector3 drawVecDos && drawVecDos.InBounds(pawn.MapHeld))
            {
                Log.Message($"c6c:: DamageSoakedMote for {pawn}: {soakedDamage}");
                MoteMaker.ThrowText(drawVecDos, pawn.MapHeld, "JT_DamageSoaked".Translate(soakedDamage));
            }
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
                if (origin.x < Caster.TrueCenter().x)
                    pushDistX = -pushDistX;
                if (origin.z < Caster.TrueCenter().z)
                    pushDistZ = -pushDistZ;
                var tempNewLoc = new Vector3(origin.x + pushDistX, 0f, origin.z + pushDistZ);
                if (tempNewLoc.ToIntVec3().Standable(Caster.Map))
                {
                    result = tempNewLoc;
                }
                else
                {
                    if (thingToPush is Pawn)
                    {
                        //target.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Rand.Range(3, 6)));
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
            LongEventHandler.QueueLongEvent(() =>
            {
                if (target is Pawn p && p.Spawned && !p.Downed && !p.Dead && p.MapHeld != null)
                {
                    var loc = PushResult(Caster, target, distance, out var applyDamage);
                    //if (p.RaceProps.Humanlike)
                    //    p.needs.mood.thoughts.memories.TryGainMemory(MiscDefOf.PJ_ThoughtPush, null);
                    var flyingObject = (FlyingObject)GenSpawn.Spawn(MiscDefOf.JT_FlyingObject, p.PositionHeld, p.MapHeld);
                    if (applyDamage && damageOnCollision)
                        flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target,
                            // TODO: Make this configurable.
                            new DamageInfo(DamageDefOf.Blunt, Rand.Range(8, 10)));
                    else
                        flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target);
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
