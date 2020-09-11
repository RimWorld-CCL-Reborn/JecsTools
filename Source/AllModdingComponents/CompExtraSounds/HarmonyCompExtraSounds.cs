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
            var type = typeof(HarmonyCompExtraSounds);

            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundMiss"),
                postfix: new HarmonyMethod(type, nameof(SoundMissPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitPawn"),
                postfix: new HarmonyMethod(type, nameof(SoundHitPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "SoundHitBuilding"),
                postfix: new HarmonyMethod(type, nameof(SoundHitBuildingPostfix)));
        }

        //=================================== COMPEXTRASOUNDS
        public static void SoundHitPawnPostfix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                if (pawn.kindDef?.GetModExtensionExtraSounds()?.soundHitPawn is SoundDef modExtSoundHitPawn)
                    __result = modExtSoundHitPawn;

                if (pawn.equipment?.Primary?.GetCompExtraSounds()?.Props.soundHitPawn is SoundDef soundHitPawn)
                    __result = soundHitPawn;
            }
        }

        public static void SoundMissPostfix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                if (pawn.equipment?.Primary?.GetCompExtraSounds()?.Props.soundMiss is SoundDef soundMiss)
                    __result = soundMiss;
            }
        }

        public static void SoundHitBuildingPostfix(ref SoundDef __result, Verb_MeleeAttack __instance)
        {
            if (__instance.caster is Pawn pawn)
            {
                if (pawn.equipment?.Primary?.GetCompExtraSounds()?.Props.soundHitBuilding is SoundDef soundHitBuilding)
                    __result = soundHitBuilding;
            }
        }
    }
}
