using Harmony;
using RimWorld;
using Verse;

namespace CompExtraSounds
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompExtraSounds
    {
        static HarmonyCompExtraSounds()
        {
            var harmony = HarmonyInstance.Create("rimworld.jecrell.comps.sounds");


            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundMiss"), null,
                new HarmonyMethod(typeof(HarmonyCompExtraSounds), "SoundMissPrefix"));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitPawn"), null,
                new HarmonyMethod(typeof(HarmonyCompExtraSounds), "SoundHitPawnPrefix"));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitBuilding"), null,
                new HarmonyMethod(typeof(HarmonyCompExtraSounds), "SoundHitBuildingPrefix"));
        }

        //=================================== COMPEXTRASOUNDS
        public static void SoundHitPawnPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                var pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {
                    //Log.Message("2");
                    var thingWithComps =
                        pawn_EquipmentTracker
                            .Primary; // (ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

                    if (thingWithComps != null)
                    {
                        //Log.Message("3");
                        var CompExtraSounds = thingWithComps.GetComp<CompExtraSounds>();

                        if (CompExtraSounds != null)
                            if (CompExtraSounds.Props.soundHitPawn != null)
                                __result = CompExtraSounds.Props.soundHitPawn;
                    }
                }
            }
        }

        public static void SoundMissPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                var pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {
                    //Log.Message("2");
                    var thingWithComps =
                        pawn_EquipmentTracker
                            .Primary; // (ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

                    if (thingWithComps != null)
                    {
                        //Log.Message("3");
                        var CompExtraSounds = thingWithComps.GetComp<CompExtraSounds>();
                        if (CompExtraSounds != null)
                            if (CompExtraSounds.Props.soundMiss != null)
                                __result = CompExtraSounds.Props.soundMiss;
                    }
                }
            }
        }

        public static void SoundHitBuildingPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                var pawn_EquipmentTracker = pawn.equipment;
                if (pawn_EquipmentTracker != null)
                {
                    //Log.Message("2");
                    var thingWithComps =
                        pawn_EquipmentTracker
                            .Primary; // (ThingWithComps)AccessTools.Field(typeof(Pawn_EquipmentTracker), "primaryInt").GetValue(pawn_EquipmentTracker);

                    if (thingWithComps != null)
                    {
                        //Log.Message("3");
                        var CompExtraSounds = thingWithComps.GetComp<CompExtraSounds>();
                        if (CompExtraSounds != null)
                            if (CompExtraSounds.Props.soundHitBuilding != null)
                            {
                                __result = CompExtraSounds.Props.soundHitBuilding;
                            }
                    }
                }
            }
        }
    }
}