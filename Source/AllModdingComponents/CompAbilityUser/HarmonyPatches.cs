using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using System.Reflection;
using UnityEngine;
using Verse.Sound;

namespace AbilityUser
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.abilityuser");
            harmony.Patch(AccessTools.Method(typeof(Targeter), "TargeterUpdate"), new HarmonyMethod(typeof(HarmonyPatches).GetMethod("TargeterUpdate_PostFix")), null);
            harmony.Patch(AccessTools.Method(typeof(Targeter), "ProcessInputEvents"), new HarmonyMethod(typeof(HarmonyPatches).GetMethod("ProcessInputEvents_PreFix")), null);

            // Initializes the AbilityUsers on Pawns
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), "InitializeComps"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("InitializeComps_PostFix")), null);
            //harmony.Patch(AccessTools.Method(typeof(Pawn), "ExposeData"), null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("ExposeData_PostFix")), null);
            //harmony.Patch(
            //    AccessTools.Method(typeof(Verse.PawnGenerator),"GeneratePawn",new Type[] { typeof(Verse.PawnGenerationRequest) }),
            //    null, new HarmonyMethod(typeof(HarmonyPatches).GetMethod("GeneratePawn_PostFix")), null);
        }

        // RimWorld.Targeter
        public static bool ProcessInputEvents_PreFix(Targeter __instance)
        {
            AccessTools.Method(typeof(Targeter), "ConfirmStillValid").Invoke(__instance, null);
            if (Event.current.type == EventType.MouseDown)
            {
                ////Log.Message("1");
                if (Event.current.button == 0 && __instance.IsTargeting)
                {

                    ////Log.Message("2");
                    if (__instance.targetingVerb != null)
                    {

                        ////Log.Message("3");
                        if (__instance.targetingVerb is Verb_UseAbility)
                        {
                            Verb_UseAbility abilityVerb = __instance.targetingVerb as Verb_UseAbility;
                            ////Log.Message("4");
                            //if (((Action<LocalTargetInfo>)AccessTools.Field(typeof(Targeter), "action").GetValue(__instance)) != null)
                            //{

                            ////Log.Message("5");
                            //TargetingParameters targetParams = (TargetingParameters)AccessTools.Field(typeof(Targeter), "targetParams").GetValue(__instance);
                            if (abilityVerb.ability.powerdef.MainVerb.AbilityTargetCategory != AbilityTargetCategory.TargetSelf)
                            {
                                TargetingParameters targetParams = abilityVerb.ability.powerdef.MainVerb.targetParams;
                                if (targetParams != null)
                                {
                                    ////Log.Message("6");
                                    IEnumerable<LocalTargetInfo> source = GenUI.TargetsAtMouse(targetParams, false);
                                    if (source != null && source.Count<LocalTargetInfo>() > 0)
                                    {
                                        ////Log.Message("7");
                                        if (source.Any<LocalTargetInfo>())
                                        {
                                            //Pawn caster = (Pawn)AccessTools.Field(typeof(Targeter), "caster").GetValue(__instance);
                                            Pawn caster = (Pawn)__instance.targetingVerb.caster;
                                            Action attackAction = CompAbilityUser.TryCastAbility(caster, source.First<LocalTargetInfo>(), caster.GetComp<CompAbilityUser>(), (Verb_UseAbility)__instance.targetingVerb, ((Verb_UseAbility)(__instance.targetingVerb)).ability.powerdef as AbilityDef);
                                            if (attackAction != null)
                                            {
                                                attackAction();
                                                //PostCastAbilityEffects(newVerb); //Another hook for modders
                                            }

                                            ////Log.Message("8");
                                            //AccessTools.Method(typeof(Targeter), "action").Invoke(__instance, new object[] {  });
                                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                                            __instance.StopTargeting();
                                            Event.current.Use();
                                            return false;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //Pawn caster = (Pawn)AccessTools.Field(typeof(Targeter), "caster").GetValue(__instance);
                                Pawn caster = (Pawn)__instance.targetingVerb.caster;
                                Action attackAction = CompAbilityUser.TryCastAbility(caster, null, caster.GetComp<CompAbilityUser>(), (Verb_UseAbility)__instance.targetingVerb, ((Verb_UseAbility)(__instance.targetingVerb)).ability.powerdef as AbilityDef);
                                if (attackAction != null)
                                {
                                    attackAction();
                                    //PostCastAbilityEffects(newVerb); //Another hook for modders
                                }

                                ////Log.Message("8");
                                //AccessTools.Method(typeof(Targeter), "action").Invoke(__instance, new object[] {  });
                                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                                __instance.StopTargeting();
                                Event.current.Use();
                                return false;
                            }

                            //}
                        }
                    }
                }
            }
            return true;
        }

        public static void TargeterUpdate_PostFix(Targeter __instance)
        {
            if (Find.Targeter.targetingVerb != null)
            {
                ////Log.Message("2");
                if (Find.Targeter.targetingVerb is Verb_UseAbility)
                {

                    ////Log.Message("3");
                    Verb_UseAbility targetVerb = Find.Targeter.targetingVerb as Verb_UseAbility;
                    if (targetVerb.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties != null)
                    {

                        ////Log.Message("4");
                        if (targetVerb.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties.range > 0)
                        {

                                ////Log.Message("6");
                                GenDraw.DrawRadiusRing(UI.MouseCell(), targetVerb.useAbilityProps.abilityDef.MainVerb.TargetAoEProperties.range);

                        }
                    }
                }
            }
        }

        public static void InitializeComps_PostFix(ThingWithComps __instance)
        {
            Pawn p = __instance as Pawn;
            if (p != null) HarmonyPatches.internalAddInAbilityUsers(p);
        }

        //// Catches loading of Pawns
        //public static void ExposeData_PostFix(Pawn __instance)
        //{ HarmonyPatches.internalAddInAbilityUsers(__instance); }

        //// Catches generation of Pawns
        //public static void GeneratePawn_PostFix(PawnGenerationRequest request, Pawn __result)
        //{ HarmonyPatches.internalAddInAbilityUsers(__result); }

        // Add in any AbilityUser Components, if the Pawn is accepting
        public static void internalAddInAbilityUsers(Pawn pawn)
        {
//            Log.Message("Trying to add AbilityUsers to Pawn");
            if ( pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            { AbilityUserUtility.TransformPawn(pawn); }
        }

    }
}
