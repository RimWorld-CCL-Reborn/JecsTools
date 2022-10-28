using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools;

public static partial class HarmonyPatches
{
    public static void HarmonyPatches_BuildingExtension(Harmony harmony, Type type)
    {
        //BuildingExtension prevents some things from wiping other things when spawned/constructing/blueprinted.
        harmony.Patch(AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.SpawningWipes)),
            postfix: new HarmonyMethod(type, nameof(SpawningWipes_PostFix)));
        harmony.Patch(AccessTools.Method(typeof(GenConstruct), nameof(GenConstruct.CanPlaceBlueprintOver)),
            postfix: new HarmonyMethod(type, nameof(CanPlaceBlueprintOver_PostFix)));

        //Ignores all structures as part of objects that disallow being fired through.
        harmony.Patch(AccessTools.Method(typeof(Projectile), "CanHit"),
            postfix: new HarmonyMethod(type, nameof(CanHit_PostFix)));
        
        //Allows a bullet to pass through walls when fired.
        harmony.Patch(AccessTools.Method(typeof(Verb), "CanHitCellFromCellIgnoringRange"),
            prefix: new HarmonyMethod(type, nameof(CanHitCellFromCellIgnoringRange_Prefix)));
    }
    
    
    public static void SpawningWipes_PostFix(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
    {
        // If SpawningWipes is already returning true, don't need to do anything.
        if (__result == false && newEntDef is ThingDef newDef && oldEntDef is ThingDef oldDef)
        {
            if (HasSharedWipeCategory(newDef, oldDef))
                __result = true;
        }
    }
    
    
    public static void CanPlaceBlueprintOver_PostFix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
    {
        // If CanPlaceBlueprintOver is already returning false, don't need to do anything.
        if (__result == true && newDef is ThingDef thingDef)
        {
            if (HasSharedWipeCategory(thingDef, oldDef))
                __result = false;
        }
    }
    
    //Check wipe categories on BuildingExtension between two defs
    private static bool HasSharedWipeCategory(ThingDef newDef, ThingDef oldDef)
    {
        static HashSet<string> GetWipeCategories(ThingDef thingDef)
        {
            var buildingExtension = GenConstruct.BuiltDefOf(thingDef)?.GetBuildingExtension();
            if (buildingExtension == null)
                return null;
            var wipeCategorySet = buildingExtension.WipeCategories;
            return wipeCategorySet == null || wipeCategorySet.Count == 0 ? null : wipeCategorySet;
        }

        var wipeCategoriesA = GetWipeCategories(newDef);
        DebugMessage($"{newDef} wipeCategoriesA: {wipeCategoriesA.ToStringSafeEnumerable()}");
        var wipeCategoriesB = GetWipeCategories(oldDef);
        DebugMessage($"{oldDef} wipeCategoriesB: {wipeCategoriesB.ToStringSafeEnumerable()}");
        if (wipeCategoriesB == null && wipeCategoriesA == null)
        {
            DebugMessage("both wipeCategories null => false");
            return false;
        }
        else if (wipeCategoriesA != null && wipeCategoriesB == null)
        {
            DebugMessage("wipeCategoriesB null => false");
            return false;
        }
        else if (wipeCategoriesB != null && wipeCategoriesA == null)
        {
            DebugMessage("wipeCategoriesA null => false");
            return false;
        }
        else
        {
            foreach (var strB in wipeCategoriesB)
            {
                if (wipeCategoriesA.Contains(strB))
                {
                    DebugMessage($"found shared wipeCategories ({strB}) => true");
                    return true;
                }
            }
            DebugMessage("no shared wipeCategories => false");
            return false;
        }
    }

    
    
    //Added B19, Oct 2019
    //ProjectileExtension check
    //Ignores all structures as part of objects that disallow being fired through.
    public static void CanHit_PostFix(Projectile __instance, Thing thing, ref bool __result)
    {
        // TODO: This patch looks pointless since it can only change __result from false to ... false.
        if (__result == false && __instance.def?.GetProjectileExtension() is ProjectileExtension ext)
        {
            if (ext.passesWalls)
            {
                //Mods will often have their own walls, so we cannot do a def check for ThingDefOf.Wall
                //Most "walls" should either be in the structure category or be able to hold walls.
                // TODO: In RW 1.3+, it seems like BuildingProperties.isPlaceOverableWall indicates whether something is a "wall",
                // but it may be better to just look at ThingDef.Fillage/fillPercent instead,
                // or maybe use PlaceWorker_OnTopOfWalls's heuristic of checking whether the defName contains "Wall"?
                if (thing?.def is ThingDef def && (def.designationCategory == DesignationCategoryDefOf.Structure || def.holdsRoof))
                {
                    __result = false;
                    return;
                }
            }
        }
    }
    
    
    //Added B19, Oct 2019
    //ProjectileExtension check
    //Allows a bullet to pass through walls when fired.
    public static bool CanHitCellFromCellIgnoringRange_Prefix(Verb __instance, ref bool __result)
    {
        if (__instance.EquipmentCompSource?.PrimaryVerb?.verbProps?.defaultProjectile?.GetProjectileExtension() is ProjectileExtension ext)
        {
            if (ext.passesWalls)
            {
                // TODO: While this does bypass the line-of-sight checks (and should it really bypass all LOS checks?),
                // this also bypasses non-LOS checks, which doesn't look right.
                __result = true;
            }
            return false;
        }
        return true;
    }
    
}
