//#define DEBUGLOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools
{
    /// <summary>
    ///     HumanlikeOrdersUtility
    ///     I wanted this class to exist so I can have all my float menu
    ///     condition checks occur in one loop through objects, rather
    ///     than having a loop for every component, class, ability user,
    ///     etc, which is intensive on hardware.
    ///     To do this, I've added an extra class (_Condition) and an enum
    ///     (_ConditionType) to make the checks as simple as possible.
    ///     All conditions and their lists of actions (extra checks, etc)
    ///     are loaded into FloatMenuOptionList;
    ///     Next, I've created two harmony patches to make a saved list
    ///     of float menu options with a unique ID tag.
    ///     Using this system should lower
    /// </summary>
    [StaticConstructorOnStartup]
    public static class _HumanlikeOrdersUtility
    {
        private static readonly Dictionary<_Condition, List<Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> floatMenuOptionList;

        //private static int floatMenuID = 0;
        //private static int lastSavedFloatMenuID = -1;
        private static readonly List<FloatMenuOption> savedList = new List<FloatMenuOption>();

        public static string optsID = null;
        public static string lastOptsID = null;

        static _HumanlikeOrdersUtility()
        {
            DebugMessage("FMOL :: Initialized Constructor");
            floatMenuOptionList = new Dictionary<_Condition, List<Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>>();
            foreach (var current in typeof(FloatMenuPatch).AllSubclassesNonAbstract())
            {
                var item = (FloatMenuPatch)Activator.CreateInstance(current);

                DebugMessage("FMOL :: Enter Loop Step");
                var floatMenus = item.GetFloatMenus();
                DebugMessage("FMOL :: Float Menus Variable Declared");

                if (floatMenus != null)
                {
                    DebugMessage("FMOL :: Float Menus Available Check Passed");
                    foreach (var (condition, func) in floatMenus)
                    {
                        DebugMessage("FMOL :: Enter Float Menu Check Loop");
                        if (floatMenuOptionList.ContainsKey(condition))
                        {
                            DebugMessage("FMOL :: Existing condition found for " + condition +
                                         " adding actions to dictionary.");
                            floatMenuOptionList[condition].Add(func);
                        }
                        else
                        {
                            DebugMessage("FMOL :: Existing condition not found for " + condition +
                                         " adding key and actions to dictionary.");
                            floatMenuOptionList.Add(condition,
                                new List<Func<Vector3, Pawn, Thing, List<FloatMenuOption>>> { func });
                        }
                    }
                }
            }

            var harmony = new Harmony("jecstools.jecrell.humanlikeorders");
            var type = typeof(_HumanlikeOrdersUtility);

            harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddHumanlikeOrders"),
                postfix: new HarmonyMethod(type, nameof(AddHumanlikeOrders_PostFix)));
            //harmony.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "CanTakeOrder"),
            //    postfix: new HarmonyMethod(type, nameof(CanTakeOrder_PostFix)));
        }

        [Conditional("DEBUGLOG")]
        private static void DebugMessage(string s)
        {
            Log.Message(s);
        }

        //private static void CanTakeOrder_PostFix(Pawn pawn, bool __result)
        //{
        //    if (__result)
        //    {
        //        floatMenuID = Rand.Range(1000, 9999);
        //        DebugMessage("FMOL :: ID Set to " + floatMenuID);
        //    }
        //}

        // RimWorld.FloatMenuMakerMap
        public static void AddHumanlikeOrders_PostFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            var c = IntVec3.FromVector3(clickPos);

            // Heuristic to prevent computing our custom orders if pawn + location + float menu option labels haven't changed.
            var optsIDBuilder = new StringBuilder(pawn.ThingID).Append(c);
            if (opts != null)
                for (int i = 0, count = opts.Count; i < count; i++)
                    optsIDBuilder.Append(opts[i].Label);
            optsID = optsIDBuilder.ToString();
            if (optsID == lastOptsID)
            {
                opts?.AddRange(savedList);
                return;
            }

            DebugMessage("FMOL :: New list constructed");
            DebugMessage(optsID);
            lastOptsID = optsID;
            savedList.Clear();
            var things = c.GetThingList(pawn.Map);
            if (things.Count > 0)
                foreach (var (condition, funcs) in floatMenuOptionList)
                {
                    if (!funcs.NullOrEmpty())
                        foreach (var passer in things.Where(condition.Passes))
                            foreach (var func in funcs)
                                if (func.Invoke(clickPos, pawn, passer) is List<FloatMenuOption> newOpts &&
                                    newOpts.Count > 0)
                                {
                                    opts?.AddRange(newOpts);
                                    savedList.AddRange(newOpts);
                                }
                }
        }
    }

    public enum _ConditionType
    {
        IsType,
        IsTypeStringMatch,
        ThingHasComp,
        HediffHasComp,
    }

    public struct _Condition : IEquatable<_Condition>
    {
        public _ConditionType Condition;
        public object Data;

        public _Condition(_ConditionType condition, object data)
        {
            Condition = condition;
            Data = data;
        }

        public override string ToString() => "Condition_" + Condition + "_" + Data;

        public bool Equals(_Condition other) => Condition == other.Condition && Equals(Data, other.Data);

        public bool Passes(object toCheck)
        {
            //Log.Message(toCheck.GetType().ToString());
            //Log.Message(Data.ToString());

            switch (Condition)
            {
                case _ConditionType.IsType:
                {
                    var dataType = Data as Type ?? Data.GetType();
                    return dataType.IsInstanceOfType(toCheck);
                }
                case _ConditionType.IsTypeStringMatch:
                    return toCheck.GetType().ToString() == (string)toCheck;
                case _ConditionType.ThingHasComp:
                {
                    if (toCheck is ThingWithComps t)
                    {
                        var dataType = Data is string typeName ? GenTypes.GetTypeInAnyAssembly(typeName) : Data as Type;
                        if (dataType != null)
                        {
                            var comps = t.AllComps;
                            for (int i = 0, count = comps.Count; i < count; i++)
                            {
                                if (dataType.IsAssignableFrom(comps[i].GetType()))
                                    return true;
                            }
                        }
                    }
                    return false;
                }
                default:
                    throw new ArgumentException("Unrecognized condition type " + Condition);
            }
        }
    }

    public abstract class FloatMenuPatch
    {
        public abstract IEnumerable<KeyValuePair<_Condition, Func<Vector3, Pawn, Thing, List<FloatMenuOption>>>> GetFloatMenus();
    }
}
