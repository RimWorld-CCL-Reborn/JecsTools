using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompSlotLoadable
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompSlotLoadable
    {
        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        static HarmonyCompSlotLoadable()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.slotloadable");
            var type = typeof(HarmonyCompSlotLoadable);

            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos)),
                postfix: new HarmonyMethod(type, nameof(GetGizmos_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"),
                postfix: new HarmonyMethod(type, nameof(DamageInfosToApply_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "DrawThingRow"),
                postfix: new HarmonyMethod(type, nameof(DrawThingRow_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PostApplyDamage)),
                postfix: new HarmonyMethod(type, nameof(PostApplyDamage_PostFix)));
            // TODO: Patch StatWorker.GetExplanationUnfinalized to include stat augment explanation?
            harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValue), new[] { typeof(Thing), typeof(bool) }),
                postfix: new HarmonyMethod(type, nameof(StatWorker_GetValue_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear)),
                postfix: new HarmonyMethod(type, nameof(StatOffsetFromGear_PostFix)));
        }

        public static void StatWorker_GetValue_PostFix(ref float __result, Thing thing, StatDef ___stat)
        {
            __result += SlotLoadableUtility.CheckThingSlotsForStatAugment(thing, ___stat);
        }

        public static void StatOffsetFromGear_PostFix(ref float __result, Thing gear, StatDef stat)
        {
            __result += SlotLoadableUtility.CheckThingSlotsForStatAugment(gear, stat);
        }

        /// <summary>
        ///     Applies the special properties to the slot loadable.
        /// </summary>
        public static void PostApplyDamage_PostFix(Pawn __instance)
        {
            if (__instance.Dead)
                return;
            var slots = __instance.equipment?.Primary?.GetSlots();
            if (slots != null)
                foreach (var slot in slots)
                {
                    var defensiveHealChance = slot.SlotOccupant?.TryGetCompSlottedBonus()?.Props?.defensiveHealChance;
                    if (defensiveHealChance != null)
                    {
                        var randValue = Rand.Value;
                        //Log.Message("defensiveHealingCalled: randValue = " + randValue);
                        if (randValue <= defensiveHealChance.chance)
                        {
                            MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, "Heal Chance: Success", 6f); // TODO: Translate()?
                            ApplyHealing(__instance, defensiveHealChance.woundLimit, defensiveHealChance.amountRange);
                        }
                    }
                }
        }

        [ThreadStatic]
        private static List<BodyPartRecord> tempInjuredParts;

        public static void ApplyHealing(Pawn pawn, int woundLimit, FloatRange amountRange, Pawn vampiricTarget = null,
            DamageDef vampiricDamageDef = null, float vampiricArmorPenetration = 0f, Vector3? vampiricDamageAngle = default)
        {
            if (tempInjuredParts == null)
                tempInjuredParts = new List<BodyPartRecord>();
            else
                tempInjuredParts.Clear();

            // This heals non-permanent injury hediffs for x randomly chosen injured body parts,
            // where x = woundLimit (or all injured body parts if woundLimit is 0).
            // The amount healed per body part is randomly chosen from amountRange.
            // If there are multiple injury hediffs that can be healed, they are healed in FIFO order.
            var hediffSet = pawn.health.hediffSet;
            var hediffs = hediffSet.hediffs;
            var maxInjuriesToHeal = woundLimit;
            foreach (var bodyPart in hediffSet.GetInjuredParts().InRandomOrder(tempInjuredParts))
            {
                if (maxInjuriesToHeal == 0)
                    break;
                var maxHealAmount = -1f;
                for (var i = 0; i < hediffs.Count; i++)
                {
                    var hediff = hediffs[i];
                    if (hediff.Part == bodyPart && (Hediff_Injury)hediff is var injury &&
                        !injury.ShouldRemove && injury.CanHealNaturally()) // basically check for scars and old wounds
                    {
                        if (maxHealAmount < 0f)
                        {
                            maxHealAmount = amountRange.RandomInRange;
                            //Log.Message($"{pawn} {bodyPart} total heal amount {maxHealAmount}");
                        }
                        var healAmount = Mathf.Min(maxHealAmount, injury.Severity); // this should be >0
                        //Log.Message($"{pawn} {bodyPart} healed {healAmount} of {injury.Severity}; " +
                        //    $"remaining max heal amount {maxHealAmount - healAmount}; " +
                        //    $"remaining max injuries to heal {maxInjuriesToHeal - 1}");
                        // Note: even if fully healing, not using HealthUtility.CureHediff since it modifies
                        // hediffs list and doesn't call the CompPostInjuryHeal hook.
                        injury.Heal(healAmount);
                        maxInjuriesToHeal--;
                        if (maxInjuriesToHeal == 0)
                            break;
                        maxHealAmount -= healAmount;
                        if (maxHealAmount <= 0f)
                            break;
                    }
                }
            }

            if (vampiricTarget != null)
            {
                var maxInjuriesToMake = woundLimit;
                foreach (var bodyPart in vampiricTarget.health.hediffSet.GetNotMissingParts().InRandomOrder(tempInjuredParts))
                {
                    if (maxInjuriesToMake == 0)
                        break;
                    var dinfo = new DamageInfo(vampiricDamageDef, amountRange.RandomInRange, vampiricArmorPenetration, -1f,
                        pawn, bodyPart);
                    dinfo.SetAngle(vampiricDamageAngle.Value);
                    //Log.Message($"{vampiricTarget} {bodyPart} vampiric dinfo {dinfo}; " +
                    //    $"remaining max injuries to make {maxInjuriesToMake - 1}");
                    vampiricTarget.TakeDamage(dinfo);
                    maxInjuriesToMake--;
                }
            }
        }

        public static void DrawThingRow_PostFix(ref float y, float width, Thing thing)
        {
            var slots = thing.GetSlots();
            if (slots != null)
                foreach (var slot in slots)
                    if (slot.SlotOccupant is Thing slotOccupant)
                    {
                        var rect = new Rect(0f, y, width, 28f);
                        Widgets.InfoCardButton(rect.width - 24f, y, slotOccupant);
                        rect.width -= 24f;
                        if (Mouse.IsOver(rect))
                        {
                            GUI.color = HighlightColor;
                            GUI.DrawTexture(rect, TexUI.HighlightTex);
                        }
                        if (slotOccupant.def.DrawMatSingle?.mainTexture != null)
                            Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), slotOccupant, 1f);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        GUI.color = ThingLabelColor;
                        var rect4 = new Rect(36f, y, width - 36f, 28f);
                        var text = slotOccupant.LabelCap;
                        Widgets.Label(rect4, text);
                        y += 28f;
                    }
        }

        // XXX: If any slot has a CompSlottedBonus with damageDef, all existing melee attacks are replaced with a custom melee attack
        // that uses that damageDef & custom armorPenetration & damage = orig damage * 0.8~1.2. The logic is based off Verb_MeleeAttackDamage's,
        // but is missing extra damages, surprise attack, and potentially other logic.
        // It also results in a melee attack per CompSlottedBonus with damageDef, which sounds broken.
        // TODO: Consider revamping this such that it keeps reuses existing melee attacks, overriding their damage type, armor penetration,
        // and damage, randomly selected from available CompSlottedBonus with damageDef.
        // RimWorld.Verb_MeleeAttackDamage
        private const float MeleeDamageRandomFactorMin = 0.8f;
        private const float MeleeDamageRandomFactorMax = 1.2f;
        public static void DamageInfosToApply_PostFix(Verb_MeleeAttack __instance, ref IEnumerable<DamageInfo> __result,
            LocalTargetInfo target)
        {
            var equipmentSource = __instance.EquipmentSource;
            if (equipmentSource == null)
                return;
            var casterPawn = __instance.CasterPawn;
            if (casterPawn == null)
                return;

            var slots = equipmentSource.GetSlots();
            if (slots != null)
            {
                Vector3? damageAngle = null;
                var hediffCompSource = __instance.HediffCompSource;
                var verbProps = __instance.verbProps;

                List<DamageInfo> newList = null;
                foreach (var slot in slots)
                {
                    // Skip slot if doesChangeStats (defaults to false) is false.
                    if (!(slot.Def?.doesChangeStats ?? false))
                        continue;
                    var slotBonus = slot.SlotOccupant?.TryGetCompSlottedBonus();
                    if (slotBonus != null)
                    {
                        var slotBonusProps = slotBonus.Props;
                        var damageDef = slotBonusProps.damageDef;
                        if (damageDef != null)
                        {
                            // This logic should be same as the first Verb_MeleeAttackDamage.DamageInfosToApply DamageInfo,
                            // except using damageDef (and CasterPawn always being non-null as established above).
                            var damageAmount = verbProps.AdjustedMeleeDamageAmount(__instance, casterPawn);
                            var weaponBodyPartGroup = verbProps.AdjustedLinkedBodyPartsGroup(__instance.tool);
                            HediffDef weaponHediff = null;
                            damageAmount = Rand.Range(damageAmount * MeleeDamageRandomFactorMin,
                                damageAmount * MeleeDamageRandomFactorMax);
                            if (damageAmount >= 1f)
                            {
                                if (hediffCompSource != null)
                                    weaponHediff = hediffCompSource.Def;
                            }
                            else
                            {
                                damageAmount = 1f;
                                damageDef = DamageDefOf.Blunt;
                            }

                            damageAngle ??= (target.Thing.Position - casterPawn.Position).ToVector3();
                            // TODO: armorPenetration should somehow be calculated via VerbProperties.AdjustedArmorPenetration.
                            var damageInfo = new DamageInfo(damageDef, damageAmount, slotBonusProps.armorPenetration,
                                -1f, casterPawn, weapon: equipmentSource.def);
                            damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                            damageInfo.SetWeaponBodyPartGroup(weaponBodyPartGroup);
                            damageInfo.SetWeaponHediff(weaponHediff);
                            damageInfo.SetAngle(damageAngle.Value);

                            if (newList == null)
                            {
                                newList = new List<DamageInfo>();
                                __result = newList;
                            }
                            newList.Add(damageInfo);
                        }
                        var vampiricEffect = slotBonusProps.vampiricHealChance;
                        if (vampiricEffect != null)
                        {
                            var randValue = Rand.Value;
                            //Log.Message("vampiricHealingCalled: randValue = " + randValue);
                            if (randValue <= vampiricEffect.chance)
                            {
                                MoteMaker.ThrowText(casterPawn.DrawPos, casterPawn.Map, "Vampiric Effect: Success", 6f); // TODO: Translate()?
                                damageAngle ??= (target.Thing.Position - casterPawn.Position).ToVector3();
                                ApplyHealing(casterPawn, vampiricEffect.woundLimit, vampiricEffect.amountRange, target.Pawn,
                                    vampiricEffect.damageDef, vampiricEffect.armorPenetration, damageAngle);
                            }
                        }
                    }
                }
            }
        }

        public static void GetGizmos_PostFix(Pawn_EquipmentTracker __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.pawn.Faction == Faction.OfPlayer)
            {
                var compSlotLoadable = __instance.Primary?.GetCompSlotLoadable();
                if (compSlotLoadable != null && compSlotLoadable.GizmosOnEquip)
                {
                    __result = __result.Concat(compSlotLoadable.EquippedGizmos());
                }
            }
        }
    }
}
