using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

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

        private static bool DrawSelectionBracketFor_PreFix(object obj, Vector3[] ___bracketLocs, Material ___SelectionBracketMat,
            Dictionary<object, float> ___selectTimes)
        {
            if (obj is ThingWithComps thing && thing.def?.GetModExtensionBigBox() is DefModExtension_BigBox bigBox)
            {
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

                SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld(___bracketLocs, thing, drawPos, drawSize, ___selectTimes, Vector2.one);
                int num = 0;
                for (int i = 0; i < 4; i++)
                {
                    Quaternion rotation = Quaternion.AngleAxis(num, Vector3.up);
                    Graphics.DrawMesh(MeshPool.plane10, ___bracketLocs[i], rotation, ___SelectionBracketMat, 0);
                    num -= 90;
                }
                return false;
            }
            return true;
        }

    }
}
