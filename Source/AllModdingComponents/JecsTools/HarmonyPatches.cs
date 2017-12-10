using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Harmony;
using UnityEngine;
using Verse.Sound;
using AbilityUser;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.jecstools.main");
            //Allow fortitude to soak damage
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "PreApplyDamage"),
                new HarmonyMethod(typeof(HarmonyPatches), (nameof(PreApplyDamage_PrePatch))), null);

        }

        // Verse.Pawn_HealthTracker
        public static bool StopPreApplyDamageCheck = false;
        public static bool PreApplyDamage_PrePatch(Pawn_HealthTracker __instance, DamageInfo dinfo, out bool absorbed)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            if (pawn != null && !StopPreApplyDamageCheck)
            {
                if (pawn.health.hediffSet.hediffs != null && pawn.health.hediffSet.hediffs.Count > 0)
                {
                    //A list will stack.
                    List<Hediff> fortitudeHediffs = pawn.health.hediffSet.hediffs.FindAll((Hediff x) => x.TryGetComp<HediffComp_DamageSoak>() != null);
                    if (!fortitudeHediffs.NullOrEmpty())
                    {
                        foreach (Hediff fortitudeHediff in fortitudeHediffs)
                        {
                            HediffComp_DamageSoak soaker = fortitudeHediff.TryGetComp<HediffComp_DamageSoak>();
                            if (soaker != null)
                            {
                                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "JT_DamageSoaked".Translate(soaker.Props.damageToSoak), -1f);
                                dinfo.SetAmount(Mathf.Max(dinfo.Amount - soaker.Props.damageToSoak, 0));
                                if (dinfo.Amount <= 0)
                                {
                                    absorbed = true;
                                    return true;
                                }
                            }
                        }
                    }
                    if (dinfo.Weapon is ThingDef weaponDef && !weaponDef.IsRangedWeapon)
                    {
                        if (dinfo.Instigator is Pawn instigator)
                        {
                            Hediff extraDamagesHediff = instigator.health.hediffSet.hediffs.FirstOrDefault(y => y.TryGetComp<HediffComp_ExtraMeleeDamages>() != null);
                            if (extraDamagesHediff != null)
                            {
                                HediffComp_ExtraMeleeDamages damages = extraDamagesHediff.TryGetComp<HediffComp_ExtraMeleeDamages>();
                                if (damages != null)
                                {
                                    StopPreApplyDamageCheck = true;
                                    for (int i = 0; i < damages.Props.extraDamages.Count; i++)
                                    {
                                        ExtraMeleeDamage dmg = damages.Props.extraDamages[i]; 
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
                            }

                            Hediff knockbackHediff = instigator.health.hediffSet.hediffs.FirstOrDefault(y => y.TryGetComp<HediffComp_Knockback>() != null);
                            if (knockbackHediff != null)
                            {
                                HediffComp_Knockback knocker = knockbackHediff.TryGetComp<HediffComp_Knockback>();
                                if (knocker != null)
                                {
                                    if (knocker.Props.knockbackChance >= Rand.Value)
                                    {
                                        if (knocker.Props.explosiveKnockback)
                                        {
                                            Explosion explosion = (Explosion)GenSpawn.Spawn(ThingDefOf.Explosion, instigator.PositionHeld, instigator.MapHeld);
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
                                        if (pawn != instigator)
                                        {
                                            if (knocker.Props.stunChance > -1 && knocker.Props.stunChance >= Rand.Value)
                                            {
                                                pawn.stances.stunner.StunFor(knocker.Props.stunTicks);
                                            }
                                            PushEffect(instigator, pawn, knocker.Props.knockDistance.RandomInRange, true);
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            absorbed = false;
            return true;
        }

        public static Vector3 PushResult(Thing Caster, Thing thingToPush, int pushDist, out bool collision)
        {
            Vector3 origin = thingToPush.TrueCenter();
            Vector3 result = origin;
            bool collisionResult = false;
            for (int i = 1; i <= pushDist; i++)
            {
                int pushDistX = i;
                int pushDistZ = i;
                if (origin.x < Caster.TrueCenter().x) pushDistX = -pushDistX;
                if (origin.z < Caster.TrueCenter().z) pushDistZ = -pushDistZ;
                Vector3 tempNewLoc = new Vector3(origin.x + pushDistX, 0f, origin.z + pushDistZ);
                if (GenGrid.Standable(tempNewLoc.ToIntVec3(), Caster.Map))
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
                if (target != null && target is Pawn p && p?.MapHeld != null)
                {
                    bool applyDamage;
                    Vector3 loc = HarmonyPatches.PushResult(Caster, target, distance, out applyDamage);
                        //if (((Pawn)target).RaceProps.Humanlike) ((Pawn)target).needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PJ_ThoughtPush"), null);
                        FlyingObject flyingObject = (FlyingObject)GenSpawn.Spawn(ThingDef.Named("JT_FlyingObject"), target.Position, target.Map);
                    if (applyDamage && damageOnCollision) flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target, new DamageInfo(DamageDefOf.Blunt, Rand.Range(8, 10)));
                    else flyingObject.Launch(Caster, new LocalTargetInfo(loc.ToIntVec3()), target);
                }
            }, "PushingCharacter", false, null);

        }

    }
}
