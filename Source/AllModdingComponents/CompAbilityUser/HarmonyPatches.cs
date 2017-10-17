using Harmony;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
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

            // when the Pawn_EquipmentTracker is notified of a new item, see if that has CompAbilityItem.
            harmony.Patch(AccessTools.Method(typeof(Verse.Pawn_EquipmentTracker),"Notify_EquipmentAdded"),null,
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_EquipmentAdded_PostFix")),null);
            // when the Pawn_EquipmentTracker is notified of one less item, see if that has CompAbilityItem.
            harmony.Patch(AccessTools.Method(typeof(Verse.Pawn_EquipmentTracker),"Notify_EquipmentRemoved"),null,
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_EquipmentRemoved_PostFix")),null);

            // when the Pawn_ApparelTracker is notified of a new item, see if that has CompAbilityItem.
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Pawn_ApparelTracker),"Notify_ApparelAdded"),null,
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_ApparelAdded_PostFix")),null);
            // when the Pawn_ApparelTracker is notified of one less item, see if that has CompAbilityItem.
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Pawn_ApparelTracker),"Notify_ApparelRemoved"),null,
                new HarmonyMethod(typeof(HarmonyPatches).GetMethod("Notify_ApparelRemoved_PostFix")),null);
        }

        public static void Notify_EquipmentAdded_PostFix(Verse.Pawn_EquipmentTracker __instance, ThingWithComps eq) {

            foreach( CompAbilityItem cai in eq.GetComps<CompAbilityItem>() ) //((Pawn)__instance.ParentHolder).GetComps<CompAbilityItem>() )
            {
                //Log.Message("Notify_EquipmentAdded_PostFix 1 : "+eq.ToString());
                //Log.Message("  Found CompAbilityItem, for CompAbilityUser of "+cai.Props.AbilityUserClass.ToString());

                foreach( CompAbilityUser cau in ((Pawn)__instance.ParentHolder).GetComps<CompAbilityUser>() )
                {
                    //Log.Message("  Found CompAbilityUser, "+cau.ToString() +" : "+ cau.GetType()+":"+cai.Props.AbilityUserClass ); //Props.AbilityUserTarget.ToString());
                    if( cau.GetType() == cai.Props.AbilityUserClass ) {
                        //Log.Message("  and they match types " );
                        cai.AbilityUserTarget= cau;
                        foreach ( AbilityDef abdef in cai.Props.Abilities ) { cau.AddWeaponAbility(abdef); }
                    }
                }

            }
        }

        public static void Notify_EquipmentRemoved_PostFix(Verse.Pawn_EquipmentTracker __instance, ThingWithComps eq) {

            foreach( CompAbilityItem cai in eq.GetComps<CompAbilityItem>() ) //((Pawn)__instance.ParentHolder).GetComps<CompAbilityItem>() )
            {
                //Log.Message("Notify_EquipmentAdded_PostFix 1 : "+eq.ToString());
                //Log.Message("  Found CompAbilityItem, for CompAbilityUser of "+cai.Props.AbilityUserClass.ToString());

                foreach( CompAbilityUser cau in ((Pawn)__instance.ParentHolder).GetComps<CompAbilityUser>() )
                {
                    //Log.Message("  Found CompAbilityUser, "+cau.ToString() +" : "+ cau.GetType()+":"+cai.Props.AbilityUserClass ); //Props.AbilityUserTarget.ToString());
                    if( cau.GetType() == cai.Props.AbilityUserClass ) {
                        //Log.Message("  and they match types " );
                        foreach ( AbilityDef abdef in cai.Props.Abilities ) { cau.RemoveWeaponAbility(abdef); }
                    }
                }

            }
        }
        public static void Notify_ApparelAdded_PostFix(RimWorld.Pawn_ApparelTracker __instance, Apparel apparel) {

            foreach( CompAbilityItem cai in apparel.GetComps<CompAbilityItem>() ) //((Pawn)__instance.ParentHolder).GetComps<CompAbilityItem>() )
            {
                foreach( CompAbilityUser cau in ((Pawn)__instance.ParentHolder).GetComps<CompAbilityUser>() )
                {
                    if( cau.GetType() == cai.Props.AbilityUserClass ) {
                        cai.AbilityUserTarget= cau;
                        foreach ( AbilityDef abdef in cai.Props.Abilities ) { cau.AddApparelAbility(abdef); }
                    }
                }
            }
        }

        public static void Notify_ApparelRemoved_PostFix(RimWorld.Pawn_ApparelTracker __instance, Apparel apparel) {

            foreach( CompAbilityItem cai in apparel.GetComps<CompAbilityItem>() ) //((Pawn)__instance.ParentHolder).GetComps<CompAbilityItem>() )
            {
                foreach( CompAbilityUser cau in ((Pawn)__instance.ParentHolder).GetComps<CompAbilityUser>() )
                {
                    if( cau.GetType() == cai.Props.AbilityUserClass ) {
                        foreach ( AbilityDef abdef in cai.Props.Abilities ) { cau.RemoveApparelAbility(abdef); }
                    }
                }
            }
        }

        // RimWorld.Targeter
        public static bool ProcessInputEvents_PreFix(Targeter __instance)
        {
            if (__instance == null) return false;
            if (__instance.targetingVerb is Verb_UseAbility v && v.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetSelf)
            {
                Pawn caster = (Pawn)__instance.targetingVerb.caster;
                v.Ability.TryCastAbility(AbilityContext.Player, caster); // caster, source.First<LocalTargetInfo>(), caster.GetComp<CompAbilityUser>(), (Verb_UseAbility)__instance.targetingVerb, ((Verb_UseAbility)(__instance.targetingVerb)).ability.powerdef as AbilityDef)?.Invoke();
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                __instance.StopTargeting();
                Event.current.Use();
                return false;
            }
            AccessTools.Method(typeof(Targeter), "ConfirmStillValid").Invoke(__instance, null);
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0 && __instance.IsTargeting)
                {

                    if (__instance.targetingVerb != null)
                    {

                        if (__instance.targetingVerb is Verb_UseAbility)
                        {
                            Verb_UseAbility abilityVerb = __instance.targetingVerb as Verb_UseAbility;
                            if (abilityVerb.Ability.Def.MainVerb.AbilityTargetCategory != AbilityTargetCategory.TargetSelf)
                            {
                                TargetingParameters targetParams = abilityVerb.Ability.Def.MainVerb.targetParams;
                                if (targetParams != null)
                                {
                                    IEnumerable<LocalTargetInfo> source = GenUI.TargetsAtMouse(targetParams, false);
                                    if (source != null && source.Count<LocalTargetInfo>() > 0)
                                    {
                                        if (source.Any<LocalTargetInfo>())
                                        {
                                            Pawn caster = (Pawn)__instance.targetingVerb.caster;
                                            abilityVerb.Ability.TryCastAbility(AbilityContext.Player, source.First<LocalTargetInfo>());// caster, source.First<LocalTargetInfo>(), caster.GetComp<CompAbilityUser>(), (Verb_UseAbility)__instance.targetingVerb, ((Verb_UseAbility)(__instance.targetingVerb)).ability.powerdef as AbilityDef)?.Invoke();
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
                                Pawn caster = (Pawn)__instance.targetingVerb.caster;
                                abilityVerb.Ability.TryCastAbility(AbilityContext.Player, null);// caster.GetComp<CompAbilityUser>(), (Verb_UseAbility)__instance.targetingVerb, ((Verb_UseAbility)(__instance.targetingVerb)).ability.powerdef as AbilityDef)?.Invoke();
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
                    if (targetVerb.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties != null)
                    {

                        ////Log.Message("4");
                        if (targetVerb.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.range > 0)
                        {

                            ////Log.Message("6");
                            GenDraw.DrawRadiusRing(UI.MouseCell(), targetVerb.UseAbilityProps.abilityDef.MainVerb.TargetAoEProperties.range);

                        }
                    }
                }
            }
        }

        public static void InitializeComps_PostFix(ThingWithComps __instance)
        {
            if (__instance is Pawn p) HarmonyPatches.InternalAddInAbilityUsers(p);
        }

        //// Catches loading of Pawns
        //public static void ExposeData_PostFix(Pawn __instance)
        //{ HarmonyPatches.internalAddInAbilityUsers(__instance); }

        //// Catches generation of Pawns
        //public static void GeneratePawn_PostFix(PawnGenerationRequest request, Pawn __result)
        //{ HarmonyPatches.internalAddInAbilityUsers(__result); }

        // Add in any AbilityUser Components, if the Pawn is accepting
        public static void InternalAddInAbilityUsers(Pawn pawn)
        {
            //            Log.Message("Trying to add AbilityUsers to Pawn");
            if ( pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
            { AbilityUserUtility.TransformPawn(pawn); }
        }

    }
}
