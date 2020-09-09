using HarmonyLib;
using RimWorld;
using Verse;

namespace CompExtraSounds
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCompExtraSounds
    {
        static HarmonyCompExtraSounds()
        {
            var harmony = new Harmony("jecstools.jecrell.comps.sounds");

            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundMiss"), null,
                new HarmonyMethod(typeof(HarmonyCompExtraSounds), nameof(SoundMissPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitPawn"), null,
                new HarmonyMethod(typeof(HarmonyCompExtraSounds), nameof(SoundHitPawnPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitBuilding"), null,
                new HarmonyMethod(typeof(HarmonyCompExtraSounds), nameof(SoundHitBuildingPrefix)));
        }

        //=================================== COMPEXTRASOUNDS
        public static void SoundHitPawnPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                if (pawn.kindDef?.GetModExtensionExtraSounds()?.soundHitPawn is SoundDef modExtSoundHitPawn)
                    __result = modExtSoundHitPawn;

                if (pawn.equipment?.Primary?.GetCompExtraSounds()?.Props.soundHitPawn is SoundDef soundHitPawn)
                    __result = soundHitPawn;
            }
        }

        public static void SoundMissPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                if (pawn.equipment?.Primary?.GetCompExtraSounds()?.Props.soundMiss is SoundDef soundMiss)
                    __result = soundMiss;
            }
        }

        public static void SoundHitBuildingPrefix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                if (pawn.equipment?.Primary?.GetCompExtraSounds()?.Props.soundHitBuilding is SoundDef soundHitBuilding)
                    __result = soundHitBuilding;
            }
        }
    }
}
