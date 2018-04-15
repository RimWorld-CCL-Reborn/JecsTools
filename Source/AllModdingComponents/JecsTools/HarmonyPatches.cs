using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AbilityUser;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
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
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "PreApplyDamage"),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(PreApplyDamage_PrePatch)), null);
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"),
                new HarmonyMethod(typeof(HarmonyPatches), nameof(ApplyProperDamage)), null);
            harmony.Patch(AccessTools.Method(typeof(ArmorUtility), "GetPostArmorDamage"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(Post_GetPostArmorDamage)));
            harmony.Patch(AccessTools.Method(typeof(PawnGenerator), "GeneratePawnInternal"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(Post_GeneratePawnInternal)));
            
            
/*            harmony.Patch(
                AccessTools.Method(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury",
                    new[]
                    {
                        typeof(Pawn), typeof(Hediff_Injury), typeof(DamageInfo),
                        AccessTools.TypeByName("DamageResult").MakeByRefType()
                    }),
                new HarmonyMethod(typeof(HarmonyPatches),
                    nameof(ApplyProperDamage)), null);*/
        }

        public static void Post_GeneratePawnInternal(PawnGenerationRequest request, ref Pawn __result)
        {
            var hediffGiverSet = __result?.def?.race?.hediffGiverSets?.FirstOrDefault(
                x => x.hediffGivers.Any(y => y is HediffGiver_StartWithHediff));
            if (hediffGiverSet == null) return;

            if (hediffGiverSet.hediffGivers.FirstOrDefault(x => x is HediffGiver_StartWithHediff) is HediffGiver_StartWithHediff hediffGiver)
            {
                hediffGiver.GiveHediff(__result);
            }
        }
        
        //ArmorUtility
        public static void Post_GetPostArmorDamage(Pawn pawn, int amountInt, BodyPartRecord part, DamageDef damageDef)
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
        
        public static void ApplyProperDamage(ref float damAmount, float armorRating, Thing armorThing, DamageDef damageDef)
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
        
        public static bool PreApplyDamage_PrePatch(Pawn_HealthTracker __instance, ref DamageInfo dinfo, out bool absorbed)
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
            tempDamageAmount = dinfo.Amount;
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
                        explosion.dealMoreDamageAtCenter = false;
                        explosion.StartExplosion(null);
                    }
                    if (pawn != instigator && !pawn.Dead && !pawn.Downed && pawn.Spawned)
                    {
                        if (knocker.Props.stunChance > -1 && knocker.Props.stunChance >= Rand.Value)
                            pawn.stances.stunner.StunFor(knocker.Props.stunTicks);
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
                    pawn.TakeDamage(new DamageInfo(dmg.def, dmg.amount, -1, instigator));
                }
                StopPreApplyDamageCheck = false;
            }
            absorbed = false;
            return false;
        }

        private static bool PreApplyDamage_ApplyDamageSoakers(ref DamageInfo dinfo, out bool absorbed, List<Hediff> fortitudeHediffs,
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
                    soakedDamage += dmgAmount;
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
                        soakedDamage += dmgAmount;
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
    }
}