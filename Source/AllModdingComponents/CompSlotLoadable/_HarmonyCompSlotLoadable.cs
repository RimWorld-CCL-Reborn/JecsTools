using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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
<<<<<<< Updated upstream
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)), null,
                new HarmonyMethod(type, nameof(GetGizmos_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue)), null,
                new HarmonyMethod(type, nameof(GetStatValue_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"), null,
                new HarmonyMethod(type, nameof(DamageInfosToApply_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "DrawThingRow"), null,
                new HarmonyMethod(type, nameof(DrawThingRow_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PostApplyDamage)), null,
                new HarmonyMethod(type, nameof(PostApplyDamage_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(StatWorker), "StatOffsetFromGear"), null,
                new HarmonyMethod(type, nameof(StatOffsetFromGear_PostFix)));
        }

        // debugging
        /*
        public static void TicksPerMove_PostFix(Pawn __instance, ref float __result, bool diagonal) {
            if ( __instance.IsColonist )  {
                float num = __instance.GetStatValue(StatDefOf.MoveSpeed, true);
                Log.Message("move speed : "+__instance.Name+ " : (GetStatValue of MoveSpeed:" +num+") (TicksPerMove:"+__result+")");
            }
        } */

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
        /// <param name="__instance"></param>
        /// <param name="dinfo"></param>
        /// <param name="totalDamageDealt"></param>
        public static void PostApplyDamage_PostFix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null) return;
            if (__instance.Dead || __instance.equipment == null) return;
            var thingWithComps = __instance.equipment.Primary;
            if (thingWithComps != null)
            {
                var comp = thingWithComps.AllComps.FirstOrDefault(x => x is CompSlotLoadable);
                if (comp != null)
                {
                    var compSlotLoadable = comp as CompSlotLoadable;
                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                        foreach (var slot in compSlotLoadable.Slots)
                            if (!slot.IsEmpty())
                            {
                                var slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                                if (slotBonus != null)
                                    if (slotBonus.Props != null)
                                    {
                                        var defensiveHealChance = slotBonus.Props.defensiveHealChance;
                                        if (defensiveHealChance != null)
                                        {
                                            //Log.Message("defensiveHealingCalled");
                                            var randValue = Rand.Value;
                                            //Log.Message("randValue = " + randValue.ToString());
                                            if (randValue <= defensiveHealChance.chance)
                                            {
                                                MoteMaker.ThrowText(__instance.DrawPos, __instance.Map,
                                                    "Heal Chance: Success", 6f);
                                                ApplyHealing(__instance, defensiveHealChance.woundLimit);
                                            }
                                        }
                                    }
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
                            //if (maxInjuriesPerBodypart > 0)
                            //{
                            if (current.CanHealNaturally() && !current.IsPermanent()) // isOld // basically check for scars and old wounds
                            {
                                current.Heal((int) current.Severity + 1);
                                maxInjuries--;
                                //maxInjuriesPerBodypart--;
                            }
                        //}

                //
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

        public static void DrawThingRow_PostFix(ITab_Pawn_Gear __instance, ref float y, float width, Thing thing,
            bool inventory = false)
        {
            //Log.Message("1");
            if (thing is ThingWithComps thingWithComps)
            {
                var comp = thingWithComps.AllComps.FirstOrDefault(x => x is CompSlotLoadable);
                if (comp != null)
                {
                    var compSlotLoadable = comp as CompSlotLoadable;
                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                        foreach (var slot in compSlotLoadable.Slots)
                            if (!slot.IsEmpty())
                            {
                                var rect = new Rect(0f, y, width, 28f);
                                Widgets.InfoCardButton(rect.width - 24f, y, slot.SlotOccupant);
                                rect.width -= 24f;
                                //bool CanControl = (bool)AccessTools.Method(typeof(ITab_Pawn_Gear), "get_CanControl").Invoke(__instance, null);
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
=======

            harmony.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos)),
                postfix: new HarmonyMethod(type, nameof(GetGizmos_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"),
                postfix: new HarmonyMethod(type, nameof(DamageInfosToApply_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "DrawThingRow"),
                postfix: new HarmonyMethod(type, nameof(DrawThingRow_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PostApplyDamage)),
                postfix: new HarmonyMethod(type, nameof(PostApplyDamage_PostFix)));
            // TODO: Patch StatWorker.GetExplanationUnfinalized to include stat augment explanation?
            harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValue), new[] { typeof(Thing), typeof(bool), typeof(int) }),
                postfix: new HarmonyMethod(type, nameof(StatWorker_GetValue_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.StatOffsetFromGear)),
                postfix: new HarmonyMethod(type, nameof(StatOffsetFromGear_PostFix)));
        }

        // Pawn_EquipmentTracker.GetGizmos
        public static void GetGizmos_PostFix(Pawn_EquipmentTracker __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.pawn.Faction == Faction.OfPlayer)
            {
                var compSlotLoadable = __instance.Primary?.GetCompSlotLoadable();
                if (compSlotLoadable != null && compSlotLoadable.GizmosOnEquip)
                {
                    __result = __result.Concat(compSlotLoadable.EquippedGizmos());
>>>>>>> Stashed changes
                }
            }
        }

<<<<<<< Updated upstream
        // RimWorld.Verb_MeleeAttack
        public static void DamageInfosToApply_PostFix(Verb_MeleeAttack __instance, ref IEnumerable<DamageInfo> __result,
            LocalTargetInfo target)
        {
            var newList = new List<DamageInfo>();
            //__result = null;
            var EquipmentSource = __instance.EquipmentSource;
            if (EquipmentSource != null)
            {
                //Log.Message("1");
                var comp = EquipmentSource.AllComps.FirstOrDefault(x => x is CompSlotLoadable);
                if (comp != null)
                {
                    //Log.Message("2");
                    var compSlotLoadable = comp as CompSlotLoadable;
                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                    {
                        //Log.Message("3");
                        var statSlots = compSlotLoadable.Slots.FindAll(z =>
                            !z.IsEmpty() && ((SlotLoadableDef) z.def).doesChangeStats);
                        if (statSlots != null && statSlots.Count > 0)
                            foreach (var slot in statSlots)
                            {
                                //Log.Message("5");
                                var slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                                if (slotBonus != null)
                                {
                                    //Log.Message("6");
                                    var superClass = __instance.GetType().BaseType;
                                    if (slotBonus.Props.damageDef != null)
                                    {
                                        //Log.Message("7");
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

                                        //Log.Message("9");
                                        ThingDef def2;
                                        if (__instance.EquipmentSource != null)
                                            def2 = __instance.EquipmentSource.def;
                                        else
                                            def2 = __instance.CasterPawn.def;

                                        //Log.Message("10");
                                        var angle = (target.Thing.Position - __instance.CasterPawn.Position)
                                            .ToVector3();

                                        //Log.Message("11");
                                        var caster = __instance.caster;

                                        //Log.Message("12");
                                        var newdamage = GenMath.RoundRandom(num);
//                                        Log.Message("applying damage "+newdamage+" out of "+num);
                                        var damageInfo = new DamageInfo(slotBonus.Props.damageDef, newdamage, slotBonus.Props.armorPenetration, -1f,
                                            caster, null, def2);
                                        damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                                        damageInfo.SetWeaponBodyPartGroup(weaponBodyPartGroup);
                                        damageInfo.SetWeaponHediff(weaponHediff);
                                        damageInfo.SetAngle(angle);

                                        //Log.Message("13");
                                        newList.Add(damageInfo);

                                        __result = newList.AsEnumerable();
                                    }
                                    var vampiricEffect = slotBonus.Props.vampiricHealChance;
                                    if (vampiricEffect != null)
                                    {
                                        //Log.Message("vampiricHealingCalled");
                                        var randValue = Rand.Value;
                                        //Log.Message("randValue = " + randValue.ToString());

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
            }
        }

        public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
=======
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
>>>>>>> Stashed changes
        {
            var c = IntVec3.FromVector3(clickPos);

            var slotLoadable =
                pawn.equipment.AllEquipmentListForReading.FirstOrDefault(x => x.TryGetComp<CompSlotLoadable>() != null);
            if (slotLoadable != null)
            {
                var compSlotLoadable = slotLoadable.GetComp<CompSlotLoadable>();
                if (compSlotLoadable != null)
                {
                    var thingList = c.GetThingList(pawn.Map);

                    foreach (var slot in compSlotLoadable.Slots)
                    {
                        var loadableThing = thingList.FirstOrDefault(y => slot.CanLoad(y.def));
                        if (loadableThing != null)
                        {
                            FloatMenuOption itemSlotLoadable;
                            var labelShort = loadableThing.Label;
                            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                itemSlotLoadable = new FloatMenuOption(
                                    "CannotEquip".Translate(labelShort) + " (" + "Incapable".Translate() + ")", null,
                                    MenuOptionPriority.Default, null, null, 0f, null, null);
                            }
                            else if (!pawn.CanReach(loadableThing, PathEndMode.ClosestTouch, Danger.Deadly))
                            {
                                itemSlotLoadable = new FloatMenuOption(
                                    "CannotEquip".Translate(labelShort) + " (" + "NoPath".Translate() + ")", null,
                                    MenuOptionPriority.Default, null, null, 0f, null, null);
                            }
                            else if (!pawn.CanReserve(loadableThing, 1))
                            {
                                itemSlotLoadable = new FloatMenuOption(
                                    "CannotEquip".Translate(labelShort) + " (" +
                                    "ReservedBy".Translate(pawn.Map.physicalInteractionReservationManager
                                        .FirstReserverOf(loadableThing).LabelShort) + ")", null,
                                    MenuOptionPriority.Default, null, null, 0f, null, null);
                            }
                            else
                            {
                                var text2 = "Equip".Translate(labelShort);
                                itemSlotLoadable = new FloatMenuOption(text2, delegate
                                {
                                    loadableThing.SetForbidden(false, true);
                                    pawn.jobs.TryTakeOrderedJob(new Job(DefDatabase<JobDef>.GetNamed("GatherSlotItem"),
                                        loadableThing));
                                    MoteMaker.MakeStaticMote(loadableThing.DrawPos, loadableThing.Map,
                                        ThingDefOf.Mote_FeedbackEquip, 1f);
                                    //PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                                }, MenuOptionPriority.High, null, null, 0f, null, null);
                            }
                            opts.Add(itemSlotLoadable);
                        }
                    }
                }
            }
        }

        public static void GetStatValue_PostFix(ref float __result, Thing thing, StatDef stat, bool applyPostProcess)
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

        public static void Get_Graphic_PostFix(Thing __instance, ref Graphic __result)
        {
            if (__instance is ThingWithComps thingWithComps)
            {
                //Log.Message("3");
                var CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    //ThingComp activatableEffect = thingWithComps.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString() == "CompActivatableEffect.CompActivatableEffect");

                    var slot = CompSlotLoadable.ColorChangingSlot;
                    if (slot != null)
                        if (!slot.IsEmpty())
                        {
                            var slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                            if (slotBonus != null)
                            {
                                //if (activatableEffect != null)
                                //{
                                //    AccessTools.Field(activatableEffect.GetType(), "overrideColor").SetValue(activatableEffect, slot.SlotOccupant.DrawColor);
                                //    Log.ErrorOnce("GraphicPostFix_Called_Activatable", 1866);
                                //}
                                //else
                                //{
                                var tempGraphic = (Graphic) AccessTools.Field(typeof(Thing), "graphicInt")
                                    .GetValue(__instance);
                                if (tempGraphic != null)
                                    if (tempGraphic.Shader != null)
                                    {
                                        tempGraphic = tempGraphic.GetColoredVersion(tempGraphic.Shader,
                                            slotBonus.Props.color,
                                            slotBonus.Props.color); //slot.SlotOccupant.DrawColor;
                                        __result = tempGraphic;
                                        //Log.Message("SlotLoadableDraw");
                                    }
                            }
                            //Log.ErrorOnce("GraphicPostFix_Called_5", 1866);
                            //}
                        }
                }
            }
        }

        public static void DrawColorPostFix(ThingWithComps __instance, ref Color __result)
        {
            if (__instance is ThingWithComps thingWithComps)
            {
                //Log.Message("3");
                var CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    var slot = CompSlotLoadable.ColorChangingSlot;
                    if (slot != null)
                        if (!slot.IsEmpty())
                        {
                            __result = slot.SlotOccupant.DrawColor;
                            __instance.Graphic.color = slot.SlotOccupant.DrawColor;
                        }
                }
            }
        }

<<<<<<< Updated upstream
        public static void DrawColorTwoPostFix(Thing __instance, ref Color __result)
        {
            if (__instance is ThingWithComps thingWithComps)
            {
                //Log.Message("3");
                var CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    var slot = CompSlotLoadable.SecondColorChangingSlot;
                    if (slot != null)
                        if (!slot.IsEmpty())
                        {
                            __result = slot.SlotOccupant.DrawColor;
                            __instance.Graphic.colorTwo = slot.SlotOccupant.DrawColor;
                        }
                }
            }
        }

        public static IEnumerable<Gizmo> GizmoGetter(CompSlotLoadable CompSlotLoadable)
        {
            //Log.Message("5");
            if (CompSlotLoadable.GizmosOnEquip)
            {
                //Log.Message("6");
                //Iterate EquippedGizmos
                var enumerator = CompSlotLoadable.EquippedGizmos().GetEnumerator();
                while (enumerator.MoveNext())
                {
                    //Log.Message("7");
                    var current = enumerator.Current;
                    yield return current;
                }
            }
        }

        public static void GetGizmos_PostFix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            //Log.Message("1");
            var pawn_EquipmentTracker = __instance.equipment;
            if (pawn_EquipmentTracker != null)
            {
                //Log.Message("2");
                var thingWithComps =
                    pawn_EquipmentTracker
                        .Primary; //(ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

                if (thingWithComps != null)
                {
                    //Log.Message("3");
                    var CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                    if (CompSlotLoadable != null)
                        if (GizmoGetter(CompSlotLoadable).Count() > 0)
                            if (__instance != null)
                                if (__instance.Faction == Faction.OfPlayer)
                                    __result = __result.Concat(GizmoGetter(CompSlotLoadable));
=======
        //ITab_Pawn_Gear.DrawThingRow
        public static void DrawThingRow_PostFix(ref float y, float width, Thing thing, bool inventory)
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


        //Pawn.PostApplyDamage
        /// <summary>
        ///     Applies the special properties to the slot loadable.
        /// </summary>
        public static void PostApplyDamage_PostFix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
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

        public static void StatWorker_GetValue_PostFix(ref float __result, Thing thing, StatDef ___stat)
        {
            __result += SlotLoadableUtility.CheckThingSlotsForStatAugment(thing, ___stat);
        }

        public static void StatOffsetFromGear_PostFix(ref float __result, Thing gear, StatDef stat)
        {
            __result += SlotLoadableUtility.CheckThingSlotsForStatAugment(gear, stat);
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
>>>>>>> Stashed changes
                }
            }
        }






    }
}