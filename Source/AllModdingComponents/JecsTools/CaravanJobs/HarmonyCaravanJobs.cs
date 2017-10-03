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
using RimWorld.Planet;
using System.Diagnostics;
using System.Threading;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.jecrell.caravanjobs");
            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetInspectString"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(GetInspectString_Jobs)), null);
            harmony.Patch(AccessTools.Method(typeof(WorldSelector), "AutoOrderToTileNow"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(AutoOrderToTileNow_Jobs)), null);
            harmony.Patch(AccessTools.Method(typeof(Caravan), "GetGizmos"), null,
                new HarmonyMethod(typeof(HarmonyPatches), nameof(GetGizmos_Jobs)), null);
            harmony.Patch(AccessTools.Method(typeof(RimWorld.Planet.WorldSelector), "SelectableObjectsUnderMouse", new Type[] { typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType() }),
                null, new HarmonyMethod(typeof(HarmonyPatches),
                nameof(SelectableObjectsUnderMouse_InvisHandler)), null);
        }

        // RimWorld.Planet.WorldSelector
        public static void SelectableObjectsUnderMouse_InvisHandler(ref bool clickedDirectlyOnCaravan, ref bool usedColonistBar, ref IEnumerable<WorldObject> __result)
        {
            List<WorldObject> objects = new List<WorldObject>(__result);
            if (!objects.NullOrEmpty())
            {
                HashSet<WorldObject> temp = new HashSet<WorldObject>(objects);
                foreach (WorldObject o in temp)
                {
                    if (!o.SelectableNow)
                        objects.Remove(o);
                }
            }
            __result = objects;
        }


        // RimWorld.Planet.Caravan

        public static void GetGizmos_Jobs(Caravan __instance, ref IEnumerable<Gizmo> __result)
        {
            if (__instance.IsPlayerControlled)
            {
                Tile curTile = Find.WorldGrid[__instance.Tile];
                if (Find.World.GetComponent<CaravanJobGiver>().CurJob(__instance) != null)
                {
                    __result = __result.Concat(new[] {new Command_Action()
                    {
                        defaultLabel = "CommandCancelConstructionLabel".Translate(),
                        defaultDesc = "CommandClearPrioritizedWorkDesc".Translate(),
                        icon = TexCommand.ClearPrioritizedWork,
                        action = delegate
                        {
                            Find.World.GetComponent<CaravanJobGiver>().Tracker(__instance).StopAll();
                        } }
                    });
                }
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
            if (Find.World.GetComponent<CaravanJobGiver>()?.Tracker(__instance)?.curDriver?.GetReport() is string s && __result.Contains("CaravanWaiting".Translate()))
            {
                __result = __result.Replace("CaravanWaiting".Translate(), s.CapitalizeFirst());
            }
        }

    }
}
