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
            var retValue = 0.0f;
            try
            {
                retValue = SlotLoadableUtility.CheckThingSlotsForStatAugment(gear, stat);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to add stats for " + gear.Label + "\n" + e.ToString());
            }
            __result += retValue;
        }

        /// <summary>
        ///     Applies the special properties to the slot loadable.
        /// </summary>
        public static void PostApplyDamage_PostFix(Pawn __instance)
        {
            if (__instance.Dead) return;
            var slots = __instance.equipment?.Primary?.GetCompSlotLoadable()?.Slots;
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
                                "Heal Chance: Success", 6f);
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
            var slots = thing.TryGetCompSlotLoadable()?.Slots;
            if (slots != null)
                foreach (var slot in slots)
                    if (!slot.IsEmpty())
                    {
                        var rect = new Rect(0f, y, width, 28f);
                        Widgets.InfoCardButton(rect.width - 24f, y, slot.SlotOccupant);
                        rect.width -= 24f;
                        if (Mouse.IsOver(rect))
                        {
                            GUI.color = HighlightColor;
                            GUI.DrawTexture(rect, TexUI.HighlightTex);
                        }
                        if (slot.SlotOccupant.def.DrawMatSingle != null &&
                            slot.SlotOccupant.def.DrawMatSingle.mainTexture != null)
                            Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), slot.SlotOccupant, 1f);
                        Text.Anchor = TextAnchor.MiddleLeft;
                        GUI.color = ThingLabelColor;
                        var rect4 = new Rect(36f, y, width - 36f, 28f);
                        var text = slot.SlotOccupant.LabelCap;
                        Widgets.Label(rect4, text);
                        y += 28f;
                    }
        }

        // RimWorld.Verb_MeleeAttack
        public static void DamageInfosToApply_PostFix(Verb_MeleeAttack __instance, ref IEnumerable<DamageInfo> __result,
            LocalTargetInfo target)
        {
            var slots = __instance.EquipmentSource?.GetCompSlotLoadable()?.Slots;
            if (slots != null)
            {
                List<DamageInfo> newList = null;
                var statSlots = slots.FindAll(z =>
                    !z.IsEmpty() && ((SlotLoadableDef)z.def).doesChangeStats);
                foreach (var slot in statSlots)
                {
                    var slotBonus = slot.SlotOccupant?.TryGetCompSlottedBonus();
                    if (slotBonus != null)
                    {
                        if (slotBonus.Props.damageDef != null)
                        {
                            var num = __instance.verbProps.AdjustedMeleeDamageAmount(__instance,
                                __instance.CasterPawn);
                            var def = __instance.verbProps.meleeDamageDef;
                            BodyPartGroupDef weaponBodyPartGroup = null;
                            HediffDef weaponHediff = null;
                            if (__instance.CasterIsPawn)
                                if (num >= 1f)
                                {
                                    weaponBodyPartGroup = __instance.verbProps.linkedBodyPartsGroup;
                                    if (__instance.HediffCompSource != null)
                                        weaponHediff = __instance.HediffCompSource.Def;
                                }
                                else
                                {
                                    num = 1f;
                                    def = DamageDefOf.Blunt;
                                }

                            ThingDef def2;
                            if (__instance.EquipmentSource != null)
                                def2 = __instance.EquipmentSource.def;
                            else
                                def2 = __instance.CasterPawn.def;

                            var angle = (target.Thing.Position - __instance.CasterPawn.Position)
                                .ToVector3();

                            var caster = __instance.caster;

                            var newdamage = GenMath.RoundRandom(num);
                            //Log.Message("applying damage " + newdamage + " out of "+num);
                            var damageInfo = new DamageInfo(slotBonus.Props.damageDef, newdamage, slotBonus.Props.armorPenetration, -1f,
                                caster, null, def2);
                            damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                            damageInfo.SetWeaponBodyPartGroup(weaponBodyPartGroup);
                            damageInfo.SetWeaponHediff(weaponHediff);
                            damageInfo.SetAngle(angle);

                            if (newList == null)
                                newList = new List<DamageInfo>();
                            newList.Add(damageInfo);

                            __result = newList.AsEnumerable();
                        }
                        var vampiricEffect = slotBonus.Props.vampiricHealChance;
                        if (vampiricEffect != null)
                        {
                            var randValue = Rand.Value;
                            //Log.Message("vampiricHealingCalled: randValue = " + randValue.ToString());

                            if (randValue <= vampiricEffect.chance)
                            {
                                MoteMaker.ThrowText(__instance.CasterPawn.DrawPos,
                                    __instance.CasterPawn.Map, "Vampiric Effect: Success", 6f);
                                //MoteMaker.ThrowText(__instance.CasterPawn.DrawPos, __instance.CasterPawn.Map, "Success".Translate(), 6f);
                                ApplyHealing(__instance.caster, vampiricEffect.woundLimit, target.Thing);
                            }
                        }
                    }
                }
            }
        }

        public static void GetStatValue_PostFix(ref float __result, Thing thing, StatDef stat)
        {
            var retValue = 0.0f;
            try
            {
                retValue = SlotLoadableUtility.CheckThingSlotsForStatAugment(thing, stat);
            }
            catch (Exception e)
            {
                Log.Warning("Failed to add stats for " + thing.Label + "\n" + e.ToString());
            }
            __result += retValue;
        }

        public static IEnumerable<Gizmo> GizmoGetter(CompSlotLoadable CompSlotLoadable)
        {
            if (CompSlotLoadable.GizmosOnEquip)
            {
                foreach (var current in CompSlotLoadable.EquippedGizmos())
                    yield return current;
            }
        }

        public static void GetGizmos_PostFix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.Faction == Faction.OfPlayer)
            {
                var compSlotLoadable = __instance.equipment?.Primary?.GetCompSlotLoadable();
                if (compSlotLoadable != null)
                {
                    var gizmos = GizmoGetter(compSlotLoadable);
                    if (gizmos.Any())
                        __result = __result.Concat(gizmos);
                }
            }
        }
    }
}
