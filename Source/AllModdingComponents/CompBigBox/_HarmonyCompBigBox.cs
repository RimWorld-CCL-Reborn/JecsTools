using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DefModExtension_BigBox
{
    [StaticConstructorOnStartup]
    internal static class HarmonyDefModExtension_BigBox
    {
        static HarmonyDefModExtension_BigBox()
        {
            var harmony = new Harmony("jecstools.jecrell.defmodextensions.bigbox");

            var type = typeof(HarmonyDefModExtension_BigBox);
            harmony.Patch(AccessTools.Method(typeof(SelectionDrawer), "DrawSelectionBracketFor"),
                new HarmonyMethod(type, nameof(DrawSelectionBracketFor_PreFix)), null);
        }

        private static bool DrawSelectionBracketFor_PreFix(object obj)
        {
            var thing = obj as ThingWithComps;
            if (thing != null && thing?.def?.GetModExtension<DefModExtension_BigBox>() is DefModExtension_BigBox bigBox)
            {

                var bracketLocs = Traverse.Create(typeof(SelectionDrawer)).Field("bracketLocs").GetValue<Vector3[]>();
                var SelectionBracketMat = Traverse.Create(typeof(SelectionDrawer)).Field("SelectionBracketMat").GetValue<Material>();
                var selectTimes = Traverse.Create(typeof(SelectionDrawer)).Field("selectTimes").GetValue<Dictionary<object, float>>();

                //Use public variables from DefModExtension_BigBox
                Vector3 drawPos = thing.DrawPos;
                Vector2 drawSize = new Vector2(1, 1);
                if (!bigBox.directionBased)
                {
                    drawPos = drawPos + bigBox.offset;
                    drawSize = bigBox.size;
                }
                else
                {
                    if (thing.Rotation == Rot4.East)
                    {
                        drawPos = drawPos + bigBox.eastOffset;
                        drawSize = bigBox.eastSize;
                    }
                    else if (thing.Rotation == Rot4.North)
                    {
                        drawPos = drawPos + bigBox.northOffset;
                        drawSize = bigBox.northSize;
                    }
                    else if (thing.Rotation == Rot4.West)
                    {
                        drawPos = drawPos + bigBox.westOffset;
                        drawSize = bigBox.westSize;
                    }
                    else
                    {
                        drawPos = drawPos + bigBox.southOffset;
                        drawSize = bigBox.southSize;
                    }
                }

                SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld(bracketLocs, thing, drawPos, drawSize, selectTimes, Vector2.one);
                int num = 0;
                for (int i = 0; i < 4; i++)
                {
                    Quaternion rotation = Quaternion.AngleAxis(num, Vector3.up);
                    Graphics.DrawMesh(MeshPool.plane10, bracketLocs[i], rotation, SelectionBracketMat, 0);
                    num -= 90;
                }
                return false;
            }
            return true;
        }

    }
}