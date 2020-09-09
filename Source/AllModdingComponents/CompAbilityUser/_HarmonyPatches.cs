using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AbilityUser
{
    public class AbilityUserMod : Mod
    {
        public AbilityUserMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("jecstools.jecrell.abilityuser");
            harmony.Patch(AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)), null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(TargeterUpdate_PostFix)), null);
            harmony.Patch(AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)),
                new HarmonyMethod(typeof(AbilityUserMod), nameof(ProcessInputEvents_PreFix)), null);
            harmony.Patch(AccessTools.Method(typeof(Targeter), "ConfirmStillValid"),
                new HarmonyMethod(typeof(AbilityUserMod), nameof(ConfirmStillValid)), null);

            // Initializes the AbilityUsers on Pawns
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps)), null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(InitializeComps_PostFix)), null);

            // when the Pawn_EquipmentTracker is notified of a new item, see if that has CompAbilityItem.
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded)),
                null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(Notify_EquipmentAdded_PostFix)), null);
            // when the Pawn_EquipmentTracker is notified of one less item, see if that has CompAbilityItem.
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_EquipmentTracker),
                    nameof(Pawn_EquipmentTracker.Notify_EquipmentRemoved)), null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(Notify_EquipmentRemoved_PostFix)), null);

            // when the Pawn_ApparelTracker is notified of a new item, see if that has CompAbilityItem.
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded)), null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(Notify_ApparelAdded_PostFix)), null);
            // when the Pawn_ApparelTracker is notified of one less item, see if that has CompAbilityItem.
            harmony.Patch(
                AccessTools.Method(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelRemoved)),
                null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(Notify_ApparelRemoved_PostFix)), null);

            harmony.Patch(AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash"),
                new HarmonyMethod(typeof(AbilityUserMod), nameof(GiveShortHash_PrePatch)), null);

            harmony.Patch(AccessTools.Method(typeof(PawnGroupKindWorker), nameof(PawnGroupKindWorker.GeneratePawns),
                    new Type[] { typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(bool) }), null,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(GeneratePawns_PostFix)));

            harmony.Patch(AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.UIIcon)),
                new HarmonyMethod(typeof(AbilityUserMod), nameof(get_UIIcon)), null);

            harmony.Patch(AccessTools.PropertyGetter(typeof(Verb_LaunchProjectile), nameof(Verb_LaunchProjectile.Projectile)),
                new HarmonyMethod(typeof(AbilityUserMod), nameof(get_Projectile_Prefix)), null);

            harmony.Patch(AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.DirectOwner)),
                new HarmonyMethod(typeof(AbilityUserMod), nameof(get_DirectOwner_Prefix)), null);

            harmony.Patch(
                AccessTools.Method(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool) })
                ,
                new HarmonyMethod(typeof(AbilityUserMod), nameof(TryStartCastOn_Prefix), null));
        }


        public static bool TryStartCastOn_Prefix(Verb __instance, LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack, bool canHitNonTargetPawns, ref bool __result)
        {
            if (!(__instance is Verb_UseAbility vua))
                return true;
            else
            {
                var result = vua.PreCastShot(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns);
                __result = result;
                return false;
            }
        }

        public static bool get_DirectOwner_Prefix(Verb __instance, ref IVerbOwner __result)
        {
            if (__instance is Verb_UseAbility vua)
            {
                __result = __instance.CasterPawn;
                return false;
            }
            return true;
        }
        public static bool get_Projectile_Prefix(Verb_LaunchProjectile __instance, ref ThingDef __result)
        {
            if (__instance is Verb_UseAbility vua)
            {
                __result = __instance.verbProps.defaultProjectile;
                return false;
            }
            return true;
        }

        //Verb
        public static bool get_UIIcon(Verb __instance, ref Texture2D __result)
        {
            if (__instance is Verb_UseAbility abilityVerb && abilityVerb.Ability.PowerButton is Texture2D tex)
            {
                __result = tex;
                return false;
            }
            return true;
        }

        // RimWorld.PawnGroupKindWorker_Normal
        public static void GeneratePawns_PostFix(PawnGroupMakerParms parms, ref List<Pawn> __result)
        {
            //Anyone special?
            if (__result.NullOrEmpty()) return;
            var specialPawns = __result.FindAll(x => x.GetCompAbilityUser() is CompAbilityUser cu && cu.CombatPoints() > 0);
            if (specialPawns.Count > 0)
            {
                //Log.Message("Special Pawns Detected");
                //Log.Message("------------------");

                //Points
                var previousPoints = parms.points;
                //Log.Message("Points: " +  previousPoints);

                //Log.Message("Average Characters");
                //Log.Message("------------------");

                //Anyone average?
                var averagePawns = __result.FindAll(x => x.GetCompAbilityUser() == null);
                int avgPawns = averagePawns.Count;
                var avgCombatPoints = new Dictionary<Pawn, float>();
                averagePawns.ForEach(x =>
                {
                    avgCombatPoints.Add(x, x.kindDef.combatPower);
                    //Log.Message(x.LabelShort + " : " + x.kindDef.combatPower);
                });

                //Log.Message("------------------");
                //Log.Message("Special Characters");
                //Log.Message("------------------");

                //What's your powers?
                var specCombatPoints = new Dictionary<Pawn, float>();
                specialPawns.ForEach(x =>
                {
                    var combatValue = x.kindDef.combatPower;
                    foreach (var compAbilityUser in x.GetCompAbilityUsers())
                    {
                        combatValue += compAbilityUser.CombatPoints();
                    }
                    specCombatPoints.Add(x, combatValue);
                    //Log.Message(x.LabelShort + " : " + combatValue);
                });

                //Special case -- single raider/character should not be special to avoid problems (e.g. Werewolf raid destroys everyone).
                if (avgPawns == 0 && specialPawns.Count == 1 && specCombatPoints.Sum(x => x.Value) > 0)
                {
                    //Log.Message("Special case called: Single character");
                    specialPawns[0].GetCompAbilityUser().DisableAbilityUser();
                    return;
                }

                //Special case -- no special characters.
                if (specialPawns.Count == 0)
                    return;

                //Should we rebalance?
                int tryLimit = avgPawns + specialPawns.Count + 1;
                int initTryLimit = tryLimit;
                var tempAvgCombatPoints = new Dictionary<Pawn, float>(avgCombatPoints);
                var tempSpecCombatPoints = new Dictionary<Pawn, float>(specCombatPoints);
                var removedCharacters = new List<Pawn>();
                while (previousPoints < tempAvgCombatPoints.Sum(x => x.Value) + tempSpecCombatPoints.Sum(x => x.Value))
                {
                    //Log.Message("------------------");
                    //Log.Message("Rebalance Attempt # " + (initTryLimit - tryLimit + 1));
                    //Log.Message("------------------");
                    //Log.Message("Scenario Points: " + previousPoints + ". Total Points: " + tempAvgCombatPoints.Sum(x => x.Value) + tempSpecCombatPoints.Sum(x => x.Value));

                    //In-case some stupid stuff occurs
                    --tryLimit;
                    if (tryLimit < 0)
                        break;

                    //If special characters outnumber the avg characters, try removing some of the special characters instead.
                    if (tempSpecCombatPoints.Count >= tempAvgCombatPoints.Count)
                    {
                        var toRemove = tempSpecCombatPoints.Keys.RandomElement();
                        if (toRemove != null)
                        {
                            //Log.Message("Removed: " + toRemove.LabelShort + " : " + tempSpecCombatPoints[toRemove]);
                            removedCharacters.Add(toRemove);
                            tempSpecCombatPoints.Remove(toRemove);
                        }
                    }
                    //If average characters outnumber special characters, then check if the combat value of avg is greater.
                    else if (tempSpecCombatPoints.Count < tempAvgCombatPoints.Count)
                    {
                        //Remove a random average character if the average characters have more combat points for a score
                        if (tempAvgCombatPoints.Sum(x => x.Value) > tempSpecCombatPoints.Sum(x => x.Value))
                        {
                            var toRemove = tempAvgCombatPoints.Keys.RandomElement();
                            if (toRemove != null)
                            {
                                //Log.Message("Removed: " + toRemove.LabelShort + " : " + tempSpecCombatPoints[toRemove]);
                                removedCharacters.Add(toRemove);
                                tempAvgCombatPoints.Remove(toRemove);
                            }
                        }
                        else
                        {
                            var toRemove = tempSpecCombatPoints.Keys.RandomElement();
                            //Log.Message("Removed: " + toRemove.LabelShort + " : " + tempSpecCombatPoints[toRemove]);
                            if (toRemove != null)
                            {
                                removedCharacters.Add(toRemove);
                                tempSpecCombatPoints.Remove(toRemove);
                            }
                        }
                    }
                }
                avgCombatPoints = tempAvgCombatPoints;
                specCombatPoints = tempSpecCombatPoints;

                //Log.Message("------------");
                //Log.Message("Final Report");
                //Log.Message("------------");
                //Log.Message("Scenario Points: " + previousPoints + ". Total Points: " + tempAvgCombatPoints.Sum(x => x.Value) + tempSpecCombatPoints.Sum(x => x.Value));
                //Log.Message("------------");
                //Log.Message("Characters");
                //Log.Message("------------------");
                __result.ForEach(x =>
                {
                    var combatValue = x.kindDef.combatPower + x.GetCompAbilityUser()?.CombatPoints() ?? 0f;
                    //Log.Message(x.LabelShort + " : " + combatValue);
                });
                foreach (var x in removedCharacters)
                {
                    if (x.GetCompAbilityUser() is CompAbilityUser cu && cu.CombatPoints() > 0)
                        cu.DisableAbilityUser();
                    else x.DestroyOrPassToWorld();
                }
                removedCharacters.Clear();
                avgCombatPoints.Clear();
                specCombatPoints.Clear();
            }
        }

        //Verse.ShortHashGiver
        public static bool GiveShortHash_PrePatch(Def def, Type defType)
        {
            //Log.Message("Shorthash called");
            if (def.shortHash != 0)
                if (defType.IsAssignableFrom(typeof(AbilityDef)) || defType == typeof(AbilityDef) ||
                    def is AbilityDef)
                    return false;
            return true;
        }

        public static void Notify_EquipmentAdded_PostFix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            //Log.Message("Notify_EquipmentAdded_PostFix 1 : " + eq);
            var compAbilityUsers = ((Pawn)__instance.ParentHolder).GetCompAbilityUsers();
            foreach (var cai in eq.GetCompAbilityItems())
            {
                //Log.Message("  Found CompAbilityItem, for CompAbilityUser of " + cai.Props.AbilityUserClass);
                foreach (var cau in compAbilityUsers)
                {
                    //Log.Message("  Found CompAbilityUser, " + cau + " : " + cau.GetType() + ":" + cai.Props.AbilityUserClass);
                    if (cau.GetType() == cai.Props.AbilityUserClass)
                    {
                        //Log.Message("  and they match types");
                        cai.AbilityUserTarget = cau;
                        foreach (var abdef in cai.Props.Abilities) cau.AddWeaponAbility(abdef);
                    }
                }
            }
        }

        public static void Notify_EquipmentRemoved_PostFix(Pawn_EquipmentTracker __instance, ThingWithComps eq)
        {
            //Log.Message("Notify_EquipmentRemoved_PostFix : " + eq);
            var compAbilityUsers = ((Pawn)__instance.ParentHolder).GetCompAbilityUsers();
            foreach (var cai in eq.GetCompAbilityItems())
            {
                //Log.Message("  Found CompAbilityItem, for CompAbilityUser of " + cai.Props.AbilityUserClass);
                foreach (var cau in compAbilityUsers)
                {
                    //Log.Message("  Found CompAbilityUser, " + cau + " : " + cau.GetType() + ":" + cai.Props.AbilityUserClass);
                    if (cau.GetType() == cai.Props.AbilityUserClass)
                    {
                        //Log.Message("  and they match types");
                        foreach (var abdef in cai.Props.Abilities) cau.RemoveWeaponAbility(abdef);
                    }
                }
            }
        }

        public static void Notify_ApparelAdded_PostFix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            //Log.Message("Notify_ApparelAdded_PostFix : " + apparel);
            var compAbilityUsers = ((Pawn)__instance.ParentHolder).GetCompAbilityUsers();
            foreach (var cai in apparel.GetCompAbilityItems())
            {
                //Log.Message("  Found CompAbilityItem, for CompAbilityUser of " + cai.Props.AbilityUserClass);
                foreach (var cau in compAbilityUsers)
                {
                    //Log.Message("  Found CompAbilityUser, " + cau + " : " + cau.GetType() + ":" + cai.Props.AbilityUserClass);
                    if (cau.GetType() == cai.Props.AbilityUserClass)
                    {
                        //Log.Message("  and they match types");
                        cai.AbilityUserTarget = cau;
                        foreach (var abdef in cai.Props.Abilities) cau.AddApparelAbility(abdef);
                    }
                }
            }
        }

        public static void Notify_ApparelRemoved_PostFix(Pawn_ApparelTracker __instance, Apparel apparel)
        {
            //Log.Message("Notify_ApparelRemoved_PostFix 1 : " + apparel);
            var compAbilityUsers = ((Pawn)__instance.ParentHolder).GetCompAbilityUsers();
            foreach (var cai in apparel.GetCompAbilityItems())
            {
                //Log.Message("  Found CompAbilityItem, for CompAbilityUser of " + cai.Props.AbilityUserClass);
                foreach (var cau in compAbilityUsers)
                {
                    //Log.Message("  Found CompAbilityUser, " + cau + " : " + cau.GetType() + ":" + cai.Props.AbilityUserClass);
                    if (cau.GetType() == cai.Props.AbilityUserClass)
                    {
                        //Log.Message("  and they match types");
                        foreach (var abdef in cai.Props.Abilities) cau.RemoveApparelAbility(abdef);
                    }
                }
            }
        }

        // RimWorld.Targeter
        public static bool ConfirmStillValid(Targeter __instance, Pawn ___caster)
        {
            if (__instance.targetingSource is Verb_UseAbility)
            {
                if (___caster != null && (___caster.Map != Find.CurrentMap || ___caster.Destroyed ||
                                       !Find.Selector.IsSelected(___caster) ||
                                       ___caster.Faction != Faction.OfPlayerSilentFail))
                    __instance.StopTargeting();
                if (__instance.targetingSource != null)
                {
                    var selector = Find.Selector;
                    if (__instance.targetingSource.CasterPawn.Map != Find.CurrentMap ||
                        __instance.targetingSource.CasterPawn.Destroyed ||
                        !selector.IsSelected(__instance.targetingSource.CasterPawn))
                    {
                        __instance.StopTargeting();
                    }
                    else
                    {
                        if (__instance.targetingSourceAdditionalPawns != null)
                            foreach (var additionalPawn in __instance.targetingSourceAdditionalPawns)
                                if (additionalPawn.Destroyed || !selector.IsSelected(additionalPawn))
                                {
                                    __instance.StopTargeting();
                                    break;
                                }
                    }
                }
                return false;
            }
            return true;
        }

        // RimWorld.Targeter
        public static bool ProcessInputEvents_PreFix(Targeter __instance)
        {
            if (__instance.targetingSource is Verb_UseAbility v)
            {
                if (v.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetSelf)
                {
                    var caster = __instance.targetingSource.CasterPawn;
                    v.Ability.TryCastAbility(AbilityContext.Player, caster);
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    __instance.StopTargeting();
                    Event.current.Use();
                    return false;
                }
                targeterConfirmStillValidMethod(__instance);
                if (Event.current.type == EventType.MouseDown)
                    if (Event.current.button == 0 && __instance.IsTargeting)
                    {
                        var obj = targeterCurrentTargetUnderMouseMethod(__instance, false);
                        if (obj.IsValid)
                            v.Ability.TryCastAbility(AbilityContext.Player, obj);
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        __instance.StopTargeting();
                        Event.current.Use();
                        return false;
                    }
            }
            return true;
        }

        // Note: These are open instance delegates where the first argument is the instance.
        private static readonly Action<Targeter> targeterConfirmStillValidMethod =
            (Action<Targeter>)AccessTools.Method(typeof(Targeter), "ConfirmStillValid").CreateDelegate(typeof(Action<Targeter>));
        private static readonly Func<Targeter, bool, LocalTargetInfo> targeterCurrentTargetUnderMouseMethod =
            (Func<Targeter, bool, LocalTargetInfo>)AccessTools.Method(typeof(Targeter), "CurrentTargetUnderMouse")
            .CreateDelegate(typeof(Func<Targeter, bool, LocalTargetInfo>));

        public static void TargeterUpdate_PostFix(Targeter __instance)
        {
            if (__instance.targetingSource is Verb_UseAbility tVerb &&
                tVerb.verbProps is VerbProperties_Ability tVerbProps)
            {
                if (tVerbProps.range > 0)
                    GenDraw.DrawRadiusRing(tVerb.CasterPawn.PositionHeld, tVerbProps.range);
                if (tVerbProps.TargetAoEProperties?.range > 0 && Find.CurrentMap is Map map &&
                    UI.MouseCell().InBounds(map))
                    GenDraw.DrawRadiusRing(UI.MouseCell(), tVerbProps.TargetAoEProperties.range);
            }
        }

        public static void InitializeComps_PostFix(ThingWithComps __instance)
        {
            if (__instance is Pawn p) InternalAddInAbilityUsers(p);
        }

        // Add in any AbilityUser Components, if the Pawn is accepting
        public static void InternalAddInAbilityUsers(Pawn pawn)
        {
            //Log.Message("Trying to add AbilityUsers to Pawn");
            if (pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                AbilityUserUtility.TransformPawn(pawn);
        }
    }
}
