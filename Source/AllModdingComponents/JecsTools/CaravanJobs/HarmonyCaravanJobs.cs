using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    internal static class HarmonyCaravanPatches
    {
        static HarmonyCaravanPatches()
        {
            var harmony = new Harmony("jecstools.jecrell.caravanjobs");

            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetInspectString"), null,
                new HarmonyMethod(typeof(HarmonyCaravanPatches), nameof(GetInspectString_Jobs)), null);
            harmony.Patch(AccessTools.Method(typeof(WorldSelector), "AutoOrderToTileNow"), null,
                new HarmonyMethod(typeof(HarmonyCaravanPatches), nameof(AutoOrderToTileNow_Jobs)), null);
            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetGizmos"), null,
                new HarmonyMethod(typeof(HarmonyCaravanPatches), nameof(GetGizmos_Jobs)), null);
            harmony.Patch(
                AccessTools.Method(typeof(WorldSelector), "SelectableObjectsUnderMouse",
                    new[] {typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType()}),
                null, new HarmonyMethod(typeof(HarmonyCaravanPatches),
                    nameof(SelectableObjectsUnderMouse_InvisHandler)), null);
        }

        // RimWorld.Planet.WorldSelector
        public static void SelectableObjectsUnderMouse_InvisHandler(ref bool clickedDirectlyOnCaravan,
            ref bool usedColonistBar, ref IEnumerable<WorldObject> __result)
        {
            var objects = new List<WorldObject>(__result);
            if (!objects.NullOrEmpty())
            {
                var temp = new HashSet<WorldObject>(objects);
                foreach (var o in temp)
                    if (!o.SelectableNow)
                        objects.Remove(o);
            }
            __result = objects;
        }


        // RimWorld.Planet.Caravan

        public static void GetGizmos_Jobs(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.IsPlayerControlled)
            {
                var curTile = Find.WorldGrid[__instance.Tile];
                if (Find.World.GetComponent<CaravanJobGiver>().CurJob(__instance) != null)
                    __result = __result.Concat(new[]
                    {
                        new Command_Action
                        {
                            defaultLabel = "CommandCancelConstructionLabel".Translate(),
                            defaultDesc = "CommandClearPrioritizedWorkDesc".Translate(),
                            icon = TexCommand.ClearPrioritizedWork,
                            action = delegate
                            {
                                Find.World.GetComponent<CaravanJobGiver>().Tracker(__instance).StopAll();
                            }
                        }
                    });
            }
        }

        // RimWorld.Planet.WorldSelector
        public static void AutoOrderToTileNow_Jobs(Caravan c, int tile)
        {
            Find.World.GetComponent<CaravanJobGiver>().Tracker(c).StopAll();
        }

        // RimWorld.Planet.Caravan

        public static void GetInspectString_Jobs(Caravan __instance, ref string __result)
        {
            if (Find.World.GetComponent<CaravanJobGiver>()?.Tracker(__instance)?.curDriver?.GetReport() is string s &&
                __result.Contains("CaravanWaiting".Translate()))
                __result = __result.Replace("CaravanWaiting".Translate(), s.CapitalizeFirst());
        }
    }
}