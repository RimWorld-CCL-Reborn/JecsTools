using Harmony;
using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace CompSlotLoadable
{
    [StaticConstructorOnStartup]
    static class HarmonyCompSlotLoadable
    {
        static HarmonyCompSlotLoadable()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.slotloadable");

            harmony.Patch(AccessTools.Method(typeof(Pawn), "GetGizmos"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("GetGizmos_PostFix")));
            //harmony.Patch(AccessTools.Method(typeof(Thing), "get_Graphic"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("get_Graphic_PostFix")));
            harmony.Patch(AccessTools.Method(typeof(StatExtension), "GetStatValue"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("GetStatValue_PostFix")));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("AddHumanlikeOrders_PostFix")));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "DamageInfosToApply"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("DamageInfosToApply_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(ITab_Pawn_Gear), "DrawThingRow"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("DrawThingRow_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn), "PostApplyDamage"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("PostApplyDamage_PostFix")), null);


            harmony.Patch(AccessTools.Method(typeof(StatWorker),"StatOffsetFromGear"),null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("StatOffsetFromGear_PostFix")));

            // Test
            //harmony.Patch(AccessTools.Method(typeof(Pawn),"TicksPerMove"), null,new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("TicksPerMove_PostFix")) );

            //Color postfixes
            //harmony.Patch(typeof(ThingWithComps).GetMethod("get_DrawColor"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("DrawColorPostFix")));
            //harmony.Patch(typeof(Thing).GetMethod("get_DrawColorTwo"), null, new HarmonyMethod(typeof(HarmonyCompSlotLoadable).GetMethod("DrawColorTwoPostFix")));
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
        public static void StatOffsetFromGear_PostFix(ref float __result, Thing gear, StatDef stat) => __result += CompSlotLoadable.CheckThingSlotsForStatAugment(gear, stat);



        /// <summary>
        /// Applies the special properties to the slot loadable.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="dinfo"></param>
        /// <param name="totalDamageDealt"></param>
        public static void PostApplyDamage_PostFix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (__instance == null) return;
            if (__instance.Dead || __instance.equipment == null) return;
            ThingWithComps thingWithComps = __instance.equipment.Primary;
            if (thingWithComps != null)
            {
                ThingComp comp = thingWithComps.AllComps.FirstOrDefault((ThingComp x) => x is CompSlotLoadable);
                if (comp != null)
                {
                    CompSlotLoadable compSlotLoadable = comp as CompSlotLoadable;
                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                    {
                        foreach (SlotLoadable slot in compSlotLoadable.Slots)
                        {
                            if (!slot.IsEmpty())
                            {
                                CompSlottedBonus slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                                if (slotBonus != null)
                                {
                                    if (slotBonus.Props != null)
                                    {
                                        SlotBonusProps_DefensiveHealChance defensiveHealChance = slotBonus.Props.defensiveHealChance;
                                        if (defensiveHealChance != null)
                                        {
                                            //Log.Message("defensiveHealingCalled");
                                            float randValue = Rand.Value;
                                            //Log.Message("randValue = " + randValue.ToString());
                                            if (randValue <= defensiveHealChance.chance)
                                            {
                                                MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, "Heal Chance: Success", 6f);
                                                ApplyHealing(__instance, defensiveHealChance.woundLimit);
                                            }
                                        }
                                    }
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
                int maxInjuries = woundLimit;

                foreach (BodyPartRecord rec in pawn.health.hediffSet.GetInjuredParts())
                {
                    if (maxInjuries > 0 || woundLimit == 0)
                    {
                        //maxInjuriesPerBodypart = 2;
                        foreach (Hediff_Injury current in from injury in pawn.health.hediffSet.GetHediffs<Hediff_Injury>() where injury.Part == rec select injury)
                        {
                            //if (maxInjuriesPerBodypart > 0)
                            //{
                            if (current.CanHealNaturally() && !current.IsOld()) // basically check for scars and old wounds
                            {
                                current.Heal((int)current.Severity + 1);
                                maxInjuries--;
                                //maxInjuriesPerBodypart--;
                            }
                            //}
                        }
                    }
                }

                //
                if (vampiricTarget != null)
                {
                    int maxInjuriesToMake = woundLimit;
                    if (woundLimit == 0) maxInjuriesToMake = 2;

                    Pawn vampiricPawn = vampiricTarget as Pawn;
                    foreach (BodyPartRecord rec in vampiricPawn.health.hediffSet.GetNotMissingParts().InRandomOrder<BodyPartRecord>())
                    {
                        if (maxInjuriesToMake > 0)
                        {
                            vampiricPawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, new IntRange(5, 10).RandomInRange, -1, vampiricPawn, rec));
                            maxInjuriesToMake--;
                        }
                    }
                }
            }
        }

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        //=================================== COMPSLOTLOADABLE

        public static void DrawThingRow_PostFix(ITab_Pawn_Gear __instance, ref float y, float width, Thing thing, bool inventory = false)
        {
            //Log.Message("1");
            if (thing is ThingWithComps thingWithComps)
            {
                ThingComp comp = thingWithComps.AllComps.FirstOrDefault((ThingComp x) => x is CompSlotLoadable);
                if (comp != null)
                {
                    CompSlotLoadable compSlotLoadable = comp as CompSlotLoadable;
                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                    {
                        foreach (SlotLoadable slot in compSlotLoadable.Slots)
                        {
                            if (!slot.IsEmpty())
                            {
                                Rect rect = new Rect(0f, y, width, 28f);
                                Widgets.InfoCardButton(rect.width - 24f, y, slot.SlotOccupant);
                                rect.width -= 24f;
                                //bool CanControl = (bool)AccessTools.Method(typeof(ITab_Pawn_Gear), "get_CanControl").Invoke(__instance, null);
                                if (Mouse.IsOver(rect))
                                {
                                    GUI.color = HarmonyCompSlotLoadable.HighlightColor;
                                    GUI.DrawTexture(rect, TexUI.HighlightTex);
                                }
                                if (slot.SlotOccupant.def.DrawMatSingle != null && slot.SlotOccupant.def.DrawMatSingle.mainTexture != null)
                                {
                                    Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), slot.SlotOccupant, 1f);
                                }
                                Text.Anchor = TextAnchor.MiddleLeft;
                                GUI.color = HarmonyCompSlotLoadable.ThingLabelColor;
                                Rect rect4 = new Rect(36f, y, width - 36f, 28f);
                                string text = slot.SlotOccupant.LabelCap;
                                Widgets.Label(rect4, text);
                                y += 28f;
                            }
                        }
                    }
                }
            }
        }

        // RimWorld.Verb_MeleeAttack
        public static void DamageInfosToApply_PostFix(Verb_MeleeAttack __instance, ref IEnumerable<DamageInfo> __result, LocalTargetInfo target)
        {
            List<DamageInfo> newList = new List<DamageInfo>();
            //__result = null;
            ThingWithComps ownerEquipment = __instance.ownerEquipment;
            if (ownerEquipment != null)
            {

                //Log.Message("1");
                ThingComp comp = ownerEquipment.AllComps.FirstOrDefault((ThingComp x) => x is CompSlotLoadable);
                if (comp != null)
                {

                    //Log.Message("2");
                    CompSlotLoadable compSlotLoadable = comp as CompSlotLoadable;
                    if (compSlotLoadable.Slots != null && compSlotLoadable.Slots.Count > 0)
                    {

                        //Log.Message("3");
                        List<SlotLoadable> statSlots = compSlotLoadable.Slots.FindAll((SlotLoadable z) => !z.IsEmpty() && ((SlotLoadableDef)z.def).doesChangeStats == true);
                        if (statSlots != null && statSlots.Count > 0)
                        {

                            //Log.Message("4");
                            foreach (SlotLoadable slot in statSlots)
                            {

                                //Log.Message("5");
                                CompSlottedBonus slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                                if (slotBonus != null)
                                {

                                    //Log.Message("6");
                                    Type superClass = __instance.GetType().BaseType;
                                    if (slotBonus.Props.damageDef != null)
                                    {

                                        //Log.Message("7");
                                        float num = __instance.verbProps.AdjustedMeleeDamageAmount(__instance, __instance.CasterPawn, __instance.ownerEquipment);
                                        DamageDef def = __instance.verbProps.meleeDamageDef;
                                        BodyPartGroupDef weaponBodyPartGroup = null;
                                        HediffDef weaponHediff = null;
                                        if (__instance.CasterIsPawn)
                                        {

                                            //Log.Message("8");
                                            if (num >= 1f)
                                            {
                                                weaponBodyPartGroup = __instance.verbProps.linkedBodyPartsGroup;
                                                if (__instance.ownerHediffComp != null)
                                                {
                                                    weaponHediff = __instance.ownerHediffComp.Def;
                                                }
                                            }
                                            else
                                            {
                                                num = 1f;
                                                def = DamageDefOf.Blunt;
                                            }
                                        }

                                        //Log.Message("9");
                                        ThingDef def2;
                                        if (__instance.ownerEquipment != null)
                                        {
                                            def2 = __instance.ownerEquipment.def;
                                        }
                                        else
                                        {
                                            def2 = __instance.CasterPawn.def;
                                        }

                                        //Log.Message("10");
                                        Vector3 angle = (target.Thing.Position - __instance.CasterPawn.Position).ToVector3();

                                        //Log.Message("11");
                                        Thing caster = __instance.caster;

                                        //Log.Message("12");
                                        int newdamage = GenMath.RoundRandom(num);
//                                        Log.Message("applying damage "+newdamage+" out of "+num);
                                        DamageInfo damageInfo = new DamageInfo(slotBonus.Props.damageDef, newdamage, -1f, caster, null, def2);
                                        damageInfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                                        damageInfo.SetWeaponBodyPartGroup(weaponBodyPartGroup);
                                        damageInfo.SetWeaponHediff(weaponHediff);
                                        damageInfo.SetAngle(angle);

                                        //Log.Message("13");
                                        newList.Add(damageInfo);

                                        __result = newList.AsEnumerable<DamageInfo>();
                                    }
                                    SlotBonusProps_VampiricEffect vampiricEffect = slotBonus.Props.vampiricHealChance;
                                    if (vampiricEffect != null)
                                    {

                                        //Log.Message("vampiricHealingCalled");
                                        float randValue = Rand.Value;
                                        //Log.Message("randValue = " + randValue.ToString());

                                        if (randValue <= vampiricEffect.chance)
                                        {
                                            MoteMaker.ThrowText(__instance.CasterPawn.DrawPos, __instance.CasterPawn.Map, "Vampiric Effect: Success", 6f);
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
        }

        public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);

            ThingWithComps slotLoadable = pawn.equipment.AllEquipmentListForReading.FirstOrDefault((ThingWithComps x) => x.TryGetComp<CompSlotLoadable>() != null);
            if (slotLoadable != null)
            {
                CompSlotLoadable compSlotLoadable = slotLoadable.GetComp<CompSlotLoadable>();
                if (compSlotLoadable != null)
                {
                    List<Thing> thingList = c.GetThingList(pawn.Map);

                    foreach (SlotLoadable slot in compSlotLoadable.Slots)
                    {
                        Thing loadableThing = thingList.FirstOrDefault((Thing y) => slot.CanLoad(y.def));
                        if (loadableThing != null)
                        {
                            FloatMenuOption itemSlotLoadable;
                            string labelShort = loadableThing.Label;
                            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                itemSlotLoadable = new FloatMenuOption("CannotEquip".Translate(new object[]
                                {
                                    labelShort
                                    }) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                                }
                                else if (!pawn.CanReach(loadableThing, PathEndMode.ClosestTouch, Danger.Deadly))
                                {
                                    itemSlotLoadable = new FloatMenuOption("CannotEquip".Translate(new object[]
                                    {
                                        labelShort
                                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                                    }
                                    else if (!pawn.CanReserve(loadableThing, 1))
                                    {
                                        itemSlotLoadable = new FloatMenuOption("CannotEquip".Translate(new object[]
                                        {
                                            labelShort
                                            }) + " (" + "ReservedBy".Translate(new object[]
                                            {
                                                pawn.Map.physicalInteractionReservationManager.FirstReserverOf(loadableThing).LabelShort
                                                }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                                            }
                                            else
                                            {
                                                string text2 = "Equip".Translate(new object[]
                                                {
                                                    labelShort
                                                    });
                                                    itemSlotLoadable = new FloatMenuOption(text2, delegate
                                                    {
                                                        loadableThing.SetForbidden(false, true);
                                                        pawn.jobs.TryTakeOrderedJob(new Job(DefDatabase<JobDef>.GetNamed("GatherSlotItem"), loadableThing));
                                                        MoteMaker.MakeStaticMote(loadableThing.DrawPos, loadableThing.Map, ThingDefOf.Mote_FeedbackEquip, 1f);
                                                        //PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                                                        }, MenuOptionPriority.High, null, null, 0f, null, null);
                                                    }
                                                    opts.Add(itemSlotLoadable);
                                                }
                                            }


                                        }
                                    }
                                }

        public static void GetStatValue_PostFix(ref float __result, Thing thing, StatDef stat, bool applyPostProcess) => __result += CompSlotLoadable.CheckThingSlotsForStatAugment(thing, stat);

        public static void Get_Graphic_PostFix(Thing __instance, ref Graphic __result)
    {
            if (__instance is ThingWithComps thingWithComps)
            {
                //Log.Message("3");
                CompSlotLoadable CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    //ThingComp activatableEffect = thingWithComps.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString() == "CompActivatableEffect.CompActivatableEffect");

                    SlotLoadable slot = CompSlotLoadable.ColorChangingSlot;
                    if (slot != null)
                    {
                        if (!slot.IsEmpty())
                        {
                            CompSlottedBonus slotBonus = slot.SlotOccupant.TryGetComp<CompSlottedBonus>();
                            if (slotBonus != null)
                            {
                                //if (activatableEffect != null)
                                //{
                                //    AccessTools.Field(activatableEffect.GetType(), "overrideColor").SetValue(activatableEffect, slot.SlotOccupant.DrawColor);
                                //    Log.ErrorOnce("GraphicPostFix_Called_Activatable", 1866);
                                //}
                                //else
                                //{
                                Graphic tempGraphic = (Graphic)AccessTools.Field(typeof(Thing), "graphicInt").GetValue(__instance);
                                if (tempGraphic != null)
                                {
                                    if (tempGraphic.Shader != null)
                                    {
                                        tempGraphic = tempGraphic.GetColoredVersion(tempGraphic.Shader, slotBonus.Props.color, slotBonus.Props.color); //slot.SlotOccupant.DrawColor;
                                        __result = tempGraphic;
                                        //Log.Message("SlotLoadableDraw");

                                    }
                                }
                            }
                            //Log.ErrorOnce("GraphicPostFix_Called_5", 1866);
                            //}
                        }
                    }
                }
            }

        }

    public static void DrawColorPostFix(ThingWithComps __instance, ref Color __result)
    {
            if (__instance is ThingWithComps thingWithComps)
            {
                //Log.Message("3");
                CompSlotLoadable CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    SlotLoadable slot = CompSlotLoadable.ColorChangingSlot;
                    if (slot != null)
                    {
                        if (!slot.IsEmpty())
                        {
                            __result = slot.SlotOccupant.DrawColor;
                            __instance.Graphic.color = slot.SlotOccupant.DrawColor;
                        }
                    }
                }
            }

        }

    public static void DrawColorTwoPostFix(Thing __instance, ref Color __result)
    {
            if (__instance is ThingWithComps thingWithComps)
            {
                //Log.Message("3");
                CompSlotLoadable CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    SlotLoadable slot = CompSlotLoadable.SecondColorChangingSlot;
                    if (slot != null)
                    {
                        if (!slot.IsEmpty())
                        {
                            __result = slot.SlotOccupant.DrawColor;
                            __instance.Graphic.colorTwo = slot.SlotOccupant.DrawColor;
                        }
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
            IEnumerator<Gizmo> enumerator = CompSlotLoadable.EquippedGizmos().GetEnumerator();
            while (enumerator.MoveNext())
            {
                //Log.Message("7");
                Gizmo current = enumerator.Current;
                yield return current;
            }
        }
    }

    public static void GetGizmos_PostFix(Pawn __instance, ref IEnumerable<Gizmo> __result)
    {
        //Log.Message("1");
        Pawn_EquipmentTracker pawn_EquipmentTracker = __instance.equipment;
        if (pawn_EquipmentTracker != null)
        {
            //Log.Message("2");
            ThingWithComps thingWithComps = pawn_EquipmentTracker.Primary; //(ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

            if (thingWithComps != null)
            {
                //Log.Message("3");
                CompSlotLoadable CompSlotLoadable = thingWithComps.GetComp<CompSlotLoadable>();
                if (CompSlotLoadable != null)
                {
                    if (GizmoGetter(CompSlotLoadable).Count<Gizmo>() > 0)
                    {
                        //Log.Message("4");
                        if (__instance != null)
                        {
                            if (__instance.Faction == Faction.OfPlayer)
                            {
                                __result = __result.Concat<Gizmo>(GizmoGetter(CompSlotLoadable));
                            }
                        }
                    }
                }
            }
        }
    }
}
}
