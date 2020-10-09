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

            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)),
                postfix: new HarmonyMethod(type, nameof(GetGizmos_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue), new[] { typeof(Thing), typeof(StatDef), typeof(bool) }),
                postfix: new HarmonyMethod(type, nameof(GetStatValue_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"),
                postfix: new HarmonyMethod(type, nameof(DamageInfosToApply_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "DrawThingRow"),
                postfix: new HarmonyMethod(type, nameof(DrawThingRow_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PostApplyDamage)),
                postfix: new HarmonyMethod(type, nameof(PostApplyDamage_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(StatWorker), "StatOffsetFromGear"),
                postfix: new HarmonyMethod(type, nameof(StatOffsetFromGear_PostFix)));
        }

        //try to extend this
        public static void StatOffsetFromGear_PostFix(ref float __result, Thing gear, StatDef stat)
        {
            __result += SlotLoadableUtility.CheckThingSlotsForStatAugment(gear, stat);
        }

        /// <summary>
        ///     Applies the special properties to the slot loadable.
        /// </summary>
        public static void PostApplyDamage_PostFix(Pawn __instance)
        {
            if (__instance.Dead) return;
            var slots = __instance.equipment?.Primary?.GetSlots();
            if (slots != null)
                foreach (var slot in slots)
                {
                    var defensiveHealChance = slot.SlotOccupant?.TryGetCompSlottedBonus()?.Props?.defensiveHealChance;
                    if (defensiveHealChance != null)
                    {
                        var randValue = Rand.Value;
                        //Log.Message("defensiveHealingCalled: randValue = " + randValue.ToString());
                        if (randValue <= defensiveHealChance.chance)
                        {
                            MoteMaker.ThrowText(__instance.DrawPos, __instance.Map,
                                "Heal Chance: Success", 6f); // TODO: Translate()?
                            ApplyHealing(__instance, defensiveHealChance.woundLimit);
                        }
                    }
                }
        }

        public static void ApplyHealing(Thing thing, int woundLimit = 0, Thing vampiricTarget = null)
        {
            if (thing is Pawn pawn)
            {
                var maxInjuries = woundLimit;

                foreach (var rec in pawn.health.hediffSet.GetInjuredParts())
                    if (maxInjuries > 0 || woundLimit == 0)
                        foreach (var current in from injury in pawn.health.hediffSet.GetHediffs<Hediff_Injury>()
                                where injury.Part == rec
                                select injury)
                            if (current.CanHealNaturally() && !current.IsPermanent()) // isOld // basically check for scars and old wounds
                            {
                                current.Heal((int)current.Severity + 1);
                                maxInjuries--;
                            }

                if (vampiricTarget != null)
                {
                    var maxInjuriesToMake = woundLimit;
                    if (woundLimit == 0) maxInjuriesToMake = 2;

                    var vampiricPawn = vampiricTarget as Pawn;
                    foreach (var rec in vampiricPawn.health.hediffSet.GetNotMissingParts().InRandomOrder())
                        if (maxInjuriesToMake > 0)
                        {
                            vampiricPawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, new IntRange(5, 10).RandomInRange,
                                1f, 1, vampiricPawn, rec));

                            maxInjuriesToMake--;
                        }
                }
            }
        }
        //=================================== COMPSLOTLOADABLE

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

        // RimWorld.Verb_MeleeAttackDamage
        private const float MeleeDamageRandomFactorMin = 0.8f;
        private const float MeleeDamageRandomFactorMax = 1.2f;
        public static void DamageInfosToApply_PostFix(Verb_MeleeAttack __instance, ref IEnumerable<DamageInfo> __result,
            LocalTargetInfo target)
        {
            var equipmentSource = __instance.EquipmentSource;
            if (equipmentSource == null) return;
            var casterPawn = __instance.CasterPawn;
            if (casterPawn == null) return;

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
                            var damageInfo = new DamageInfo(damageDef, damageAmount, slotBonusProps.armorPenetration,
                                -1f, casterPawn, hitPart: null, equipmentSource.def);
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
                            //Log.Message("vampiricHealingCalled: randValue = " + randValue.ToString());
                            if (randValue <= vampiricEffect.chance)
                            {
                                MoteMaker.ThrowText(casterPawn.DrawPos, casterPawn.Map, "Vampiric Effect: Success", 6f); // TODO: Translate()?
                                //MoteMaker.ThrowText(casterPawn.DrawPos, casterPawn.Map, "Success".Translate(), 6f);
                                ApplyHealing(casterPawn, vampiricEffect.woundLimit, target.Thing);
                            }
                        }
                    }
                }
            }
        }

        public static void GetStatValue_PostFix(ref float __result, Thing thing, StatDef stat)
        {
            __result += SlotLoadableUtility.CheckThingSlotsForStatAugment(thing, stat);
        }

        public static void GetGizmos_PostFix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.Faction == Faction.OfPlayer)
            {
                var compSlotLoadable = __instance.equipment?.Primary?.GetCompSlotLoadable();
                if (compSlotLoadable != null && compSlotLoadable.GizmosOnEquip)
                {
                    __result = __result.Concat(compSlotLoadable.EquippedGizmos());
                }
            }
        }
    }
}
