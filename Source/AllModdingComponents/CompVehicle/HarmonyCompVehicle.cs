using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;
using System.Reflection.Emit;
using RimWorld.Planet;
using System.Runtime.CompilerServices;
using RimWorld.BaseGen;

namespace CompVehicle
{

    [StaticConstructorOnStartup]
    static class HarmonyCompVehicle
    {
        static HarmonyCompVehicle()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.comps.pilotable");
            harmony.Patch(AccessTools.Method(typeof(DamageWorker_AddInjury), "FinalizeAndAddInjury"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "FinalizeAndAddInjury_PostFix"));
            harmony.Patch(AccessTools.Method(typeof(Pawn_PathFollower), "StartPath"), new HarmonyMethod(typeof(HarmonyCompVehicle), "StartPath_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(Verb_Shoot), "TryCastShot"), new HarmonyMethod(typeof(HarmonyCompVehicle), "TryCastShot_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(HealthUtility), "GetGeneralConditionLabel"), new HarmonyMethod(typeof(HarmonyCompVehicle), "GetGeneralConditionLabel_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(HealthCardUtility), "DrawOverviewTab"), new HarmonyMethod(typeof(HarmonyCompVehicle), "DrawOverviewTab_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn_HealthTracker), "ShouldBeDowned"), new HarmonyMethod(typeof(HarmonyCompVehicle), "ShouldBeDowned_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(PawnDownedWiggler), "WigglerTick"), new HarmonyMethod(typeof(HarmonyCompVehicle), "WigglerTick_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(Pawn), "get_IsColonistPlayerControlled"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "IsColonistPlayerControlled_PostFix"));
            harmony.Patch(AccessTools.Method(typeof(Pawn), "CurrentlyUsable"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "CurrentlyUsable_PostFix"));
            harmony.Patch(typeof(Building_CrashedShipPart).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(mi => mi.HasAttribute<CompilerGeneratedAttribute>() && mi.ReturnType == typeof(bool) && mi.GetParameters().Count() == 1 && mi.GetParameters()[0].ParameterType == typeof(PawnKindDef)), null, new HarmonyMethod(typeof(HarmonyCompVehicle), nameof(MechanoidsFixer)));
            harmony.Patch(AccessTools.Method(typeof(JobDriver_Wait), "CheckForAutoAttack"), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), nameof(CheckForAutoAttackTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(VerbTracker).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).First(), "MoveNext"), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), nameof(GetVerbsCommandsTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetRangedAttackAction)), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), nameof(FightActionTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.GetMeleeAttackAction)), null, null, new HarmonyMethod(typeof(HarmonyCompVehicle), nameof(FightActionTranspiler)));
            harmony.Patch(AccessTools.Method(typeof(CaravanUIUtility), "AddPawnsSections"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "AddPawnsSections_PostFix"));
            harmony.Patch(AccessTools.Method(typeof(CaravanUtility), "IsOwner"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "IsOwner_PostFix"));
            harmony.Patch(AccessTools.Method(typeof(Dialog_FormCaravan), "CheckForErrors"), new HarmonyMethod(typeof(HarmonyCompVehicle), "CheckForErrors_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(SymbolResolver_RandomMechanoidGroup), "<Resolve>m__271"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), nameof(MechanoidsFixerAncient)));
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), "UpdateAllDuties"), new HarmonyMethod(typeof(HarmonyCompVehicle), "UpdateAllDuties_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherAnimals), "UpdateAllDuties"), new HarmonyMethod(typeof(HarmonyCompVehicle), "UpdateAllDutiesTwo_Prefix"), null);
            harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherSlaves), "LordToilTick"), new HarmonyMethod(typeof(HarmonyCompVehicle), "LordToilTick_PreFix"), null);
            harmony.Patch(AccessTools.Method(typeof(CaravanExitMapUtility), "CanExitMapAndJoinOrCreateCaravanNow"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "CanExit_PostFix"), null);
            harmony.Patch(AccessTools.Method(typeof(ThinkNode_ConditionalColonist), "Satisfied"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "Satisfied_PostFix"), null);

            //harmony.Patch(AccessTools.Method(typeof(LordToil_PrepareCaravan_GatherItems), "LordToilTick"), new HarmonyMethod(typeof(HarmonyCompVehicle), "LordToilTick_PreFix"), null);

            //harmony.Patch(AccessTools.Method(typeof(Dialog_FormCaravan), "Dialog_FormCaravan.<CheckForErrors>c__AnonStorey3F6.<>m__569"), null, new HarmonyMethod(typeof(HarmonyCompVehicle), "CanLoad_PostFix"));
        }

        // RimWorld.LordToil_PrepareCaravan_GatherSlaves
        public static void LordToilTick_PreFix(LordToil_PrepareCaravan_GatherSlaves __instance)
        {
            Log.Message("1Three");
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                Log.Message("2Three");

                var meetingPoint = Traverse.Create(__instance).Field("meetingPoint").GetValue<IntVec3>();
                GatherAnimalsAndSlavesForCaravanUtility.CheckArrived(__instance.lord, meetingPoint, "AllSlavesGathered", (Pawn x) => (!x.IsColonist && !(x.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.MovementHandlerAvailable)) && !x.RaceProps.Animal, (Pawn x) => GatherAnimalsAndSlavesForCaravanUtility.IsFollowingAnyone(x));
            }
        }


        //public class ThinkNode_ConditionalColonist : ThinkNode_Conditional
        public static void Satisfied_PostFix(Pawn pawn, ref bool __result)
        {
            __result = pawn.IsColonist || (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.MovementHandlerAvailable);
        }


        // RimWorld.Planet.CaravanExitMapUtility
        public static void CanExit_PostFix(Pawn pawn, ref bool __result)
        {
            __result = pawn.Spawned && pawn.Map.exitMapGrid.MapUsesExitGrid && ((pawn.IsColonist || (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.MovementHandlerAvailable)) || CaravanExitMapUtility.FindCaravanToJoinFor(pawn) != null);
        }

        // RimWorld.LordToil_PrepareCaravan_GatherAnimals
        public static void UpdateAllDutiesTwo_Prefix(LordToil_PrepareCaravan_GatherAnimals __instance)
        {
            Log.Message("Two1");
            if (__instance.lord.ownedPawns is List<Pawn> pawns && !pawns.NullOrEmpty() && pawns.FirstOrDefault(x => x.GetComp<CompVehicle>() != null) != null)
            {
                Log.Message("Two2");

                for (int i = 0; i < __instance.lord.ownedPawns.Count; i++)
                {
                    Pawn pawn = __instance.lord.ownedPawns[i];
                    if (pawn.IsColonist || pawn.RaceProps.Animal || (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.MovementHandlerAvailable))
                    {
                        var meetingPoint = Traverse.Create(__instance).Field("meetingPoint").GetValue<IntVec3>();

                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherPawns, meetingPoint, -1f);
                        pawn.mindState.duty.pawnsToGather = PawnsToGather.Animals;
                    }
                    else
                    {
                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait);
                    }
                }
            }
        }



        // RimWorld.LordToil_PrepareCaravan_GatherItems
        public static bool UpdateAllDuties_PreFix(LordToil_PrepareCaravan_GatherItems __instance)
        {
            Log.Message("1");
            if (__instance.lord.ownedPawns is List<Pawn> pawns && !pawns.NullOrEmpty() && pawns.FirstOrDefault(x => x.GetComp<CompVehicle>() != null) != null)
            {
                Log.Message("2");

                for (int i = 0; i < pawns.Count; i++)
                {
                    Pawn pawn = pawns[i];
                    if (pawn.IsColonist || pawn.GetComp<CompVehicle>() is CompVehicle comp && comp.MovementHandlerAvailable)
                    {
                        Log.Message("3");

                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherItems);
                    }
                    else if (pawn.RaceProps.Animal)
                    {
                        var meetingPoint = Traverse.Create(__instance).Field("meetingPoint").GetValue<IntVec3>();
                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, meetingPoint, -1f);
                    }
                    else
                    {
                        pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait);
                    }
                }
                return false;
            }
            return true;
        }


        // RimWorld.Dialog_FormCaravan
        public static bool CheckForErrors_PreFix(List<Pawn> pawns, ref bool __result)
        {
            if (pawns.FindAll((x) => x.GetComp<CompVehicle>() != null) is List<Pawn> vehicles)
            {
                if (vehicles.Any((y) => y.GetComp<CompVehicle>() is CompVehicle vehicle && vehicle.MovementHandlerAvailable))
                {
                    __result = true;
                    return false;
                }
            }
            return true;
        }
        
        // RimWorld.Planet.CaravanUtility
        public static void IsOwner_PostFix(Pawn pawn, Faction caravanFaction, ref bool __result)
        {
            if (pawn.GetComp<CompVehicle>() is CompVehicle compVehicle)
            {
                __result = compVehicle.MovementHandlerAvailable && pawn.Faction == caravanFaction && pawn.HostFaction == null;
            }
        }
        

        // RimWorld.CaravanUIUtility
        public static void AddPawnsSections_PostFix(TransferableOneWayWidget widget, List<TransferableOneWay> transferables)
        {
            IEnumerable<TransferableOneWay> source = from x in transferables
                                                     where x.ThingDef.category == ThingCategory.Pawn
                                                     select x;
            widget.AddSection("CompVehicle_VehicleSection".Translate(), from x in source
                                                            where ((Pawn)x.AnyThing).GetComp<CompVehicle>() != null &&
                                                            ((Pawn)x.AnyThing).GetComp<CompVehicle>().MovementHandlerAvailable
                                                            select x);
        }


        public static IEnumerable<CodeInstruction> FightActionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            FieldInfo storyInfo = AccessTools.Field(typeof(Pawn), nameof(Pawn.story));
            bool done = false;
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (!done && instruction.operand == storyInfo)
                {
                    yield return instruction;
                    yield return new CodeInstruction(instructionList[i + 3]);
                    yield return new CodeInstruction(instructionList[i - 2]) { labels = new List<Label>() };
                    yield return new CodeInstruction(instructionList[i - 1]);
                    instruction = new CodeInstruction(instruction);
                    done = true;
                }

                yield return instruction;
            }
        }

        public static IEnumerable<CodeInstruction> GetVerbsCommandsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo storyInfo = AccessTools.Field(typeof(Pawn), nameof(Pawn.story));
            bool done = false;
            List<CodeInstruction> instructionList = instructions.ToList();
            for (int i = 0; i < instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(!done && instruction.operand == storyInfo)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Brfalse_S, instructionList[i + 3].operand);
                    yield return new CodeInstruction(instructionList[i - 3]);
                    yield return new CodeInstruction(instructionList[i - 2]);
                    yield return new CodeInstruction(instructionList[i - 1]);
                    instruction = new CodeInstruction(instruction);
                    done = true;
                }

                yield return instruction;
            }
        }


        public static IEnumerable<CodeInstruction> CheckForAutoAttackTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo playerFactionInfo = AccessTools.Property(typeof(Faction), nameof(Faction.OfPlayer)).GetGetMethod();
            bool done = false;
            List<CodeInstruction> instructionList = instructions.ToList();
            for(int i = 0; i<instructionList.Count; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if(!done && instruction.operand == playerFactionInfo)
                {
                    done = true;
                    yield return instruction;
                    yield return instructionList[i + 1];
                    yield return instructionList[i + 2];
                    yield return instructionList[i + 3];
                    yield return instructionList[i + 4];
                    instruction = new CodeInstruction(OpCodes.Brfalse_S, instructionList[i + 1].operand);
                    i++;
                }

                yield return instruction;
            }
        }

        // Verse.Pawn
        public static bool DropAndForbidEverything_PreFix(Pawn __instance) => __instance?.def?.GetCompProperties<CompProperties_Vehicle>() == null;

        // Verse.Pawn
        public static void CurrentlyUsable_PostFix(Pawn __instance, ref bool __result)
        {
            CompVehicle vehicle = __instance.GetComp<CompVehicle>();
            if (vehicle != null)
            {
                if (!__instance.pather.MovingNow) __result = true;
            }
        }


        public static void IsColonistPlayerControlled_PostFix(Pawn __instance, ref bool __result)
        {
            CompVehicle vehicle = __instance.GetComp<CompVehicle>();
            if (vehicle != null)
            {
                if (__instance.Faction == Faction.OfPlayer) __result = true;
            }
        }

        // RimWorld.Building_CrashedShipPart
        public static void MechanoidsFixerAncient(ref bool __result, PawnKindDef kind)
        {
            //Log.Message("1");
            if (kind.race.HasComp(typeof(CompVehicle))) __result = false;
        }

        // RimWorld.Building_CrashedShipPart
        public static void MechanoidsFixer(ref bool __result, PawnKindDef def)
        {
            //Log.Message("1");
            if (def.race.HasComp(typeof(CompVehicle))) __result = false;
        }
        
        // Verse.PawnDownedWiggler
        public static bool WigglerTick_PreFix(PawnDownedWiggler __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (!compPilotable.Props.canWiggleWhenDowned) return false;
                }
            }
            return true;
        }

        // Verse.Pawn_HealthTracker
        public static bool ShouldBeDowned_PreFix(Pawn_HealthTracker __instance, ref bool __result)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_HealthTracker), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (!compPilotable.Props.canBeDowned)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        // RimWorld.HealthCardUtility
        public static bool DrawOverviewTab_PreFix(ref float __result, Rect leftRect, Pawn pawn, float curY)
        {
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    curY += 4f;
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = new Color(0.9f, 0.9f, 0.9f);
                    string text = StringOf.Movement;
                    if (compPilotable.movingStatus == MovingState.able)
                    {
                        text = text + ": " + StringOf.On;
                    }
                    else
                    {
                        text = text + ": " + StringOf.Off;
                    }
                    Rect rect = new Rect(0f, curY, leftRect.width, 34f);
                    Widgets.Label(rect, text.CapitalizeFirst());
                    //TooltipHandler.TipRegion(rect, () => pawn.ageTracker.AgeTooltipString, 73412);
                    //if (Mouse.IsOver(rect))
                    //{
                    //    Widgets.DrawHighlight(rect);
                    //}
                    curY += 34f;
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = new Color(0.9f, 0.9f, 0.9f);
                    string text2 = StringOf.Weapons;
                    if (compPilotable.weaponStatus == WeaponState.able)
                    {
                        text2 = text2 + ": " + StringOf.On;
                    }
                    else
                    {
                        text2 = text2 + ": " + StringOf.Off;
                    }
                    Rect rect2 = new Rect(0f, curY, leftRect.width, 34f);
                    Widgets.Label(rect2, text2.CapitalizeFirst());
                    curY += 34f;
                    __result = curY;
                    return false;
                }
            }
            return true;
        }

        // Verse.HealthUtility
        public static bool GetGeneralConditionLabel_PreFix(ref string __result, Pawn pawn, bool shortVersion = false)
        {
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (pawn.Downed || !pawn.health.capacities.CanBeAwake)
                    {
                        __result = compPilotable.Props.labelInoperable;
                        return false;
                    }
                    if (pawn.Dead)
                    {
                        __result = compPilotable.Props.labelBroken;
                        return false;
                    }
                    __result = compPilotable.Props.labelUndamaged;
                    return false;
                }
            }
            return true;
        }

        // Verse.Verb_Shoot
        public static bool TryCastShot_PreFix(Verb_Shoot __instance, ref bool __result)
        {
            Pawn pawn = __instance.CasterPawn;
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (compPilotable.weaponStatus == WeaponState.frozen)
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            return true;
        }

        // Verse.AI.Pawn_PathFollower
        public static bool StartPath_PreFix(Pawn_PathFollower __instance, LocalTargetInfo dest, PathEndMode peMode)
        {
            Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_PathFollower), "pawn").GetValue(__instance);
            if (pawn != null)
            {
                CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
                if (compPilotable != null)
                {
                    if (compPilotable.movingStatus == MovingState.frozen) return false;
                }
            }
            return true;
        }

            // Verse.DamageWorker_AddInjury
            public static void FinalizeAndAddInjury_PostFix(DamageWorker_AddInjury __instance, Pawn pawn, Hediff_Injury injury, DamageInfo dinfo)
        {
            CompVehicle compPilotable = pawn.GetComp<CompVehicle>();
            if (compPilotable != null)
            {
                List<Pawn> affectedPawns = new List<Pawn>();
                
                if (compPilotable.handlers != null && compPilotable.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in compPilotable.handlers)
                    {
                        if (group.OccupiedParts != null && (group.handlers != null && group.handlers.Count > 0))
                        {
                            if (group.OccupiedParts.Contains(injury.Part))
                            {
                                affectedPawns.AddRange(group.handlers);
                            }
                        }
                    }
                }

                //Attack the seatholder
                if (affectedPawns != null && affectedPawns.Count > 0)
                {
                    DamageInfo newDamageInfo = new DamageInfo(dinfo);
                    float criticalBonus = 0f;
                    if (Rand.Value < compPilotable.Props.seatHitCriticalHitChance) criticalBonus = dinfo.Amount * 2;
                    float newDamFloat = (dinfo.Amount * compPilotable.Props.seatHitDamageFactor) + criticalBonus;
                    newDamageInfo.SetAmount((int)newDamFloat);
                    affectedPawns.RandomElement<Pawn>().TakeDamage(newDamageInfo);
                }
            }
        }
    }
}
