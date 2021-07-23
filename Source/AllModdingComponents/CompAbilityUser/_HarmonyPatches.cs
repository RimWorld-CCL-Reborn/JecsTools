//#define DEBUGLOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var type = typeof(AbilityUserMod);

            harmony.Patch(AccessTools.Method(typeof(Targeter), nameof(Targeter.TargeterUpdate)),
                postfix: new HarmonyMethod(type, nameof(TargeterUpdate_PostFix)));
            harmony.Patch(AccessTools.Method(typeof(Targeter), nameof(Targeter.ProcessInputEvents)),
                prefix: new HarmonyMethod(type, nameof(ProcessInputEvents_PreFix)));
            harmony.Patch(AccessTools.Method(typeof(Targeter), "ConfirmStillValid"),
                prefix: new HarmonyMethod(type, nameof(ConfirmStillValid)));

            // Initializes the AbilityUsers on Pawns
            harmony.Patch(AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.InitializeComps)),
                postfix: new HarmonyMethod(type, nameof(InitializeComps_PostFix)));

            harmony.Patch(AccessTools.Method(typeof(ShortHashGiver), "GiveShortHash"),
                prefix: new HarmonyMethod(type, nameof(GiveShortHash_PrePatch)));

            harmony.Patch(AccessTools.Method(typeof(PawnGroupKindWorker), nameof(PawnGroupKindWorker.GeneratePawns),
                    new[] { typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(bool) }),
                postfix: new HarmonyMethod(type, nameof(GeneratePawns_PostFix)));

            harmony.Patch(AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.UIIcon)),
                prefix: new HarmonyMethod(type, nameof(get_UIIcon)));

            harmony.Patch(AccessTools.PropertyGetter(typeof(Verb_LaunchProjectile), nameof(Verb_LaunchProjectile.Projectile)),
                prefix: new HarmonyMethod(type, nameof(get_Projectile_Prefix)));

            harmony.Patch(AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.DirectOwner)),
                prefix: new HarmonyMethod(type, nameof(get_DirectOwner_Prefix)));

            harmony.Patch(AccessTools.Method(typeof(Verb), nameof(Verb.TryStartCastOn),
                    new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(type, nameof(TryStartCastOn_Prefix)));
        }

#if DEBUGLOG
        private const bool isDebugLog = true;
#else
        private const bool isDebugLog = false;
#endif

        [Conditional("DEBUGLOG")]
        private static void DebugMessage(string s) => Log.Message(s);

        public static bool TryStartCastOn_Prefix(Verb __instance, LocalTargetInfo castTarg, LocalTargetInfo destTarg,
            bool surpriseAttack, bool canHitNonTargetPawns, bool preventFriendlyFire, ref bool __result)
        {
            if (__instance is Verb_UseAbility vua)
            {
                __result = vua.PreCastShot(castTarg, destTarg, surpriseAttack, canHitNonTargetPawns, preventFriendlyFire);
                return false;
            }
            return true;
        }

        public static bool get_DirectOwner_Prefix(Verb __instance, ref IVerbOwner __result)
        {
            if (__instance is Verb_UseAbility)
            {
                __result = __instance.CasterPawn;
                return false;
            }
            return true;
        }
        public static bool get_Projectile_Prefix(Verb_LaunchProjectile __instance, ref ThingDef __result)
        {
            if (__instance is Verb_UseAbility)
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

        private struct PawnAbilityPointsEntry
        {
            public readonly Pawn pawn;
            public readonly CompAbilityUser[] comps;
            public readonly float basePoints;
            public readonly float points;

            private PawnAbilityPointsEntry(Pawn pawn, CompAbilityUser[] comps, float basePoints, float points)
            {
                this.pawn = pawn;
                this.comps = comps;
                this.basePoints = basePoints;
                this.points = points;
            }

            public bool IsSpecial => points > basePoints;

            public static PawnAbilityPointsEntry For(Pawn pawn)
            {
                var comps = pawn.GetCompAbilityUsers().ToArray();
                var basePoints = pawn.kindDef.combatPower;
                var points = basePoints;
                foreach (var comp in comps)
                    points += comp.CombatPoints();
                return new PawnAbilityPointsEntry(pawn, comps, basePoints, points);
            }

            public PawnAbilityPointsEntry DisableAbilityUser()
            {
                var basePoints = pawn.kindDef.combatPower;
                var points = basePoints;
                foreach (var comp in comps)
                {
                    comp.DisableAbilityUser();
                    points += comp.CombatPoints();
                }
                return new PawnAbilityPointsEntry(pawn, comps, basePoints, points);
            }

            public override string ToString()
            {
                var pointStr = points == basePoints ? $"{points}" : $"{points}={basePoints}+{points - basePoints}";
                return $"({pawn}, kindDef={pawn.kindDef}, role={pawn.GetTraderCaravanRole()}, #comps={comps.Length}, points={pointStr})";
            }
        }

        private struct PawnAbilityPointsEntries
        {
            public List<PawnAbilityPointsEntry> list; // allocated only if necessary
            public int count;
            public float points;

            public void Add(PawnAbilityPointsEntry entry, bool addToList = true)
            {
                if (addToList)
                {
                    list ??= new List<PawnAbilityPointsEntry>();
                    list.Add(entry);
                }
                count++;
                points += entry.points;
            }

            public void Remove(PawnAbilityPointsEntry entry)
            {
                if (list?.Remove(entry) ?? false)
                {
                    count--;
                    points -= entry.points;
                }
            }

            public override string ToString()
            {
                var str = $"#pawns = {count}, points = {points}";
                ToStringAppendList(ref str);
                return str;
            }

            [Conditional("DEBUGLOG")]
            private void ToStringAppendList(ref string str)
            {
                if (list != null)
                    str += list.Join(entry => $"\n\t{entry}", "");
            }
        }

        [Conditional("DEBUGLOG")]
        private static void DebugAdd(ref PawnAbilityPointsEntries entries, PawnAbilityPointsEntry entry, bool addToList = true) =>
            entries.Add(entry, addToList);

        [Conditional("DEBUGLOG")]
        private static void DebugAdd(ref int x, int y) => x += y;

        [Conditional("DEBUGLOG")]
        private static void DebugProfileStart(ref long startTimestamp) => startTimestamp = Stopwatch.GetTimestamp();

        [Conditional("DEBUGLOG")]
        private static void DebugProfileStop(string format, long startTimestamp) =>
            DebugMessage(string.Format(format, (Stopwatch.GetTimestamp() - startTimestamp) * 1000 / Stopwatch.Frequency));

        // RimWorld.PawnGroupKindWorker
        public static void GeneratePawns_PostFix(PawnGroupMakerParms parms, List<Pawn> __result)
        {
            // PawnGroupKindWorker.GeneratePawns is about the earliest we can patch, since we need pawns
            // to be generated (yet not yet spawned) so that CompAbilityUsers are available on these pawns.
            // This is why we can't simply patch PawnGenOption.Cost or PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints.

            if (__result.Count == 0)
                return;

            var startTimestamp = 0L;
            DebugProfileStart(ref startTimestamp);
            RebalanceGeneratedPawns(parms, __result);
            DebugProfileStop(rgpMsgPrefix + "Elapsed time: {0} msecs", startTimestamp);
        }

        private const string rgpMsgPrefix = nameof(CompAbilityUser) + "." + nameof(RebalanceGeneratedPawns) + ": ";

        private static void RebalanceGeneratedPawns(PawnGroupMakerParms parms, List<Pawn> pawns)
        {
            // PawnGroupKindWorker does not guaranteed that the sum of pawn points (pawn.kindDef.combatPower) <= parms.points.
            // Chattel pawns (slaves and animals) are not included in parms.points yet are in __result.
            // Also, PawnGroupKindWorker_Trader decrements parms.points for trader pawns, effectively likewise excluding them.
            var targetPoints = parms.points; // parms.points + special trader points (see below)
            var targetCount = 0; // debug only, represents the pawns contributing to targetPoints
            var origCount = 0; // debug only, represents the pawns contributing to parms.points

            // Partition into special and normal pawns, calculating combat points for each.
            // Note: entry lists are allocated only if necessary.
            var specials = new PawnAbilityPointsEntries();
            var normals = new PawnAbilityPointsEntries();
            var excluded = new PawnAbilityPointsEntries(); // debug only
            foreach (var pawn in pawns)
            {
                var entry = PawnAbilityPointsEntry.For(pawn);
                // parms.points is not used for slaves and animals, so exclude them.
                // Using GetTraderCaravanRole() for this, since Humanoid Alien Races patches this method to account for alienslavekinds.
                var traderCaravanRole = pawn.GetTraderCaravanRole();
                if (traderCaravanRole == TraderCaravanRole.Chattel || traderCaravanRole == TraderCaravanRole.Carrier)
                {
                    DebugAdd(ref excluded, entry);
                }
                // Not using TraderCaravanRole.Trader for determining traders - non-null pawn.traders is the authoritative source.
                else if (pawn.trader == null)
                {
                    if (entry.IsSpecial)
                        specials.Add(entry);
                    else
                        normals.Add(entry);
                    DebugAdd(ref origCount, 1);
                    DebugAdd(ref targetCount, 1);
                }
                else
                {
                    // PawnGroupKindWorker_Trader reduces parms.points by trader's cost, so exclude them as well.
                    if (entry.IsSpecial)
                    {
                        // Not excluding 'special' traders yet, to allow them to be disabled into normal traders in below rebalancing loop.
                        specials.Add(entry);
                        // Since they're not excluded yet, include them in targetPoints (and targetCount);
                        // this will be "undone" if disabled in below rebalancing loop.
                        DebugAdd(ref targetCount, 1);
                        targetPoints += entry.basePoints;
                    }
                    else
                        DebugAdd(ref excluded, entry, isDebugLog);
                }
            }

            if (specials.count > 0)
            {
                DebugMessage(rgpMsgPrefix + "Target: " + (targetCount != origCount
                    ? $"#pawns = {origCount} orig + {targetCount - origCount} special trader = {targetCount}, " +
                      $"points = {parms.points} orig + {targetPoints - parms.points} special trader = {targetPoints}"
                    : $"#pawns = {targetCount}, points = {targetPoints}"));
                DebugMessage(rgpMsgPrefix + "Special: " + specials);
                DebugMessage(rgpMsgPrefix + "Normal: " + normals);
                DebugMessage(rgpMsgPrefix + "Excluded: " + excluded);

                // Rebalancing loop:
                // Until # special pawns = 0 or # pawns <= 1 or special pawn + normal pawn points <= target points:
                //   If # special pawns > # normal pawns or special pawn points > normal pawn points:
                //     Try to disable a random special pawn into a normal pawn.
                //     If this fails, remove the special pawn instead.
                //     Except if the special pawn being disabled is a trader, just exclude them like normal traders even if disabling fails.
                //   Else:
                //     Remove a random normal pawn.
                var iterCount = 0; // debug only
                var destroyed = new PawnAbilityPointsEntries(); // debug only
                while (true)
                {
                    var condition = specials.count > 0 && specials.count + normals.count > 1 &&
                        specials.points + normals.points > targetPoints;
                    DebugMessage(rgpMsgPrefix + (condition ? $"Rebalance iteration {++iterCount}" : "Final"));
                    DebugMessage(rgpMsgPrefix + "#pawns: " + (targetCount != origCount
                            ? $"{origCount} orig + {targetCount - origCount} special trader = {targetCount}"
                            : $"{targetCount} orig") +
                        $", {specials.count} special + {normals.count} normal = {specials.count + normals.count}, " +
                        $"{excluded.count} excluded, {destroyed.count} destroyed");
                    DebugMessage(rgpMsgPrefix + "points: " + (targetPoints != parms.points
                            ? $"{parms.points} orig + {targetPoints - parms.points} special trader = {targetPoints}"
                            : $"{targetPoints} orig") +
                        $", {specials.points} special + {normals.points} normal = {specials.points + normals.points}, " +
                        $"{excluded.points} excluded, {destroyed.points} destroyed");
                    if (!condition)
                        break;

                    if (specials.count >= normals.count || specials.points >= normals.points)
                    {
                        var entry = specials.list.RandomElement();
                        var pawn = entry.pawn;
                        specials.Remove(entry);
                        var newEntry = entry.DisableAbilityUser();
                        if (pawn.trader != null)
                        {
                            if (newEntry.IsSpecial)
                            {
                                Log.Warning(rgpMsgPrefix + "DisableAbilityUser on 'special' trader pawn {entry} into " +
                                    $"'normal' trader pawn {newEntry} may not have worked, but keeping this pawn due to trader status");
                                // Even if disabling didn't work, exclude them like normal trader pawns from more rebalancing, but do NOT
                                // "undo" their inclusion in targetPoints, so that rebalancing still has to account for their base points.
                            }
                            else
                            {
                                DebugMessage(rgpMsgPrefix + "Disabled special trader pawn {entry} into normal trader pawn {newEntry}");
                                // Once disabled, exclude them like normal trader pawns from more rebalancing,
                                // and "undo" their inclusion in targetPoints (and targetCount).
                                targetPoints -= newEntry.basePoints;
                                DebugAdd(ref targetCount, -1);
                            }
                            DebugAdd(ref excluded, newEntry, addToList: false);
                        }
                        else
                        {
                            if (newEntry.IsSpecial)
                            {
                                Log.Warning(rgpMsgPrefix + "DisableAbilityUser on 'special' pawn {entry} into 'normal' pawn {newEntry} " +
                                    "may not have worked, so destroying this pawn");
                                pawn.DestroyOrPassToWorld();
                                pawns.Remove(pawn);
                                DebugAdd(ref destroyed, newEntry, addToList: false);
                            }
                            else
                            {
                                DebugMessage(rgpMsgPrefix + "Disabled special non-trader pawn {entry} into normal non-trader pawn {newEntry}");
                                normals.Add(newEntry);
                            }
                        }
                    }
                    else
                    {
                        // Note: Since specCount < avgCount, there's at least one normal pawn.
                        var entry = normals.list.RandomElement();
                        var pawn = entry.pawn;
                        DebugMessage(rgpMsgPrefix + "Destroyed normal non-trader pawn {entry}");
                        normals.Remove(entry);
                        pawn.DestroyOrPassToWorld();
                        pawns.Remove(pawn);
                        DebugAdd(ref destroyed, entry, addToList: false);
                    }
                }
                DebugMessage(rgpMsgPrefix + "Result: #pawns = {pawns.Count}" +
                    pawns.Join(pawn => $"\n\t{PawnAbilityPointsEntry.For(pawn)}", ""));
            }
        }

        // Verse.ShortHashGiver
        public static bool GiveShortHash_PrePatch(Def def, Type defType)
        {
            DebugMessage($"GiveShortHash_PrePatch({def}, {defType})");
            if (def.shortHash != 0)
                if (def is AbilityDef || typeof(AbilityDef).IsAssignableFrom(defType))
                    return false;
            return true;
        }

        // RimWorld.Targeter
        public static bool ConfirmStillValid(Targeter __instance, Pawn ___caster)
        {
            if (__instance.targetingSource is Verb_UseAbility v)
            {
                if (___caster != null && (___caster.Map != Find.CurrentMap || ___caster.Destroyed ||
                                       !Find.Selector.IsSelected(___caster) ||
                                       ___caster.Faction != Faction.OfPlayerSilentFail)) // TODO: is this last condition still needed?
                    __instance.StopTargeting();
                if (v != null)
                {
                    var selector = Find.Selector;
                    if (v.Caster.Map != Find.CurrentMap ||
                        v.Caster.Destroyed ||
                        !selector.IsSelected(v.Caster) ||
                        (!v.GetVerb?.Available() ?? false))
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
        public static bool ProcessInputEvents_PreFix(Targeter __instance, Func<LocalTargetInfo, bool> ___targetValidator,
            bool ___playSoundOnAction, ref bool ___needsStopTargetingCall)
        {
            if (__instance.targetingSource is Verb_UseAbility v)
            {
                if (v.UseAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetSelf)
                {
                    var caster = v.Caster;
                    v.Ability.TryCastAbility(AbilityContext.Player, caster);
                    if (___playSoundOnAction)
                        SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    __instance.StopTargeting();
                    Event.current.Use();
                    return false;
                }
                targeterConfirmStillValidMethod(__instance);
                if (!__instance.IsTargeting)
                    return false;
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    var target = targeterCurrentTargetUnderMouseMethod(__instance, false);
                    ___needsStopTargetingCall = true;
                    if (!v.ValidateTarget(target))
                    {
                        Event.current.Use();
                        return false;
                    }
                    if (___targetValidator != null)
                    {
                        if (___targetValidator(target))
                            v.Ability.TryCastAbility(AbilityContext.Player, target);
                        else
                            ___needsStopTargetingCall = false;
                    }
                    else if (target.IsValid)
                        v.Ability.TryCastAbility(AbilityContext.Player, target);
                    if (___playSoundOnAction)
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    if (v.DestinationSelector != null)
                        __instance.BeginTargeting(v.DestinationSelector, v);
                    else if (v.MultiSelect && Event.current.shift)
                        __instance.BeginTargeting(v);
                    else if (__instance.targetingSourceParent != null && __instance.targetingSourceParent.MultiSelect && Event.current.shift)
                        __instance.BeginTargeting(__instance.targetingSourceParent);
                    if (___needsStopTargetingCall)
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
                    GenDraw.DrawRadiusRing(tVerb.Caster.PositionHeld, tVerbProps.range);
                if (tVerbProps.TargetAoEProperties?.range > 0 && Find.CurrentMap is Map map &&
                    UI.MouseCell().InBounds(map))
                    GenDraw.DrawRadiusRing(UI.MouseCell(), tVerbProps.TargetAoEProperties.range);
            }
        }

        public static void InitializeComps_PostFix(ThingWithComps __instance)
        {
            if (__instance is Pawn p)
                InternalAddInAbilityUsers(p);
        }

        // Add in any AbilityUser Components, if the Pawn is accepting
        public static void InternalAddInAbilityUsers(Pawn pawn)
        {
            if (pawn != null && pawn.RaceProps != null && pawn.RaceProps.Humanlike)
                AbilityUserUtility.TransformPawn(pawn);
        }
    }
}
