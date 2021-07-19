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
                prefix: new HarmonyMethod(type, nameof(DrawSelectionBracketFor_PreFix)));
        }

        private static bool DrawSelectionBracketFor_PreFix(object obj, Vector3[] ___bracketLocs, Material ___SelectionBracketMat,
            Dictionary<object, float> ___selectTimes)
        {
            if (obj is ThingWithComps thing && thing.def?.GetModExtensionBigBox() is DefModExtension_BigBox bigBox)
            {
                //Use public variables from DefModExtension_BigBox
                var drawPos = thing.DrawPos;
                Vector2 drawSize;
                if (!bigBox.directionBased)
                {
                    drawPos += bigBox.offset;
                    drawSize = bigBox.size;
                }
                else
                {
                    if (thing.Rotation == Rot4.East)
                    {
                        drawPos += bigBox.eastOffset;
                        drawSize = bigBox.eastSize;
                    }
                    else if (thing.Rotation == Rot4.North)
                    {
                        drawPos += bigBox.northOffset;
                        drawSize = bigBox.northSize;
                    }
                    else if (thing.Rotation == Rot4.West)
                    {
                        drawPos += bigBox.westOffset;
                        drawSize = bigBox.westSize;
                    }
                    else
                    {
                        drawPos += bigBox.southOffset;
                        drawSize = bigBox.southSize;
                    }
                }

                SelectionDrawerUtility.CalculateSelectionBracketPositionsWorld(___bracketLocs, thing, drawPos, drawSize, ___selectTimes, Vector2.one);
                var num = 0;
                for (var i = 0; i < 4; i++)
                {
                    var rotation = Quaternion.AngleAxis(num, Vector3.up);
                    Graphics.DrawMesh(MeshPool.plane10, ___bracketLocs[i], rotation, ___SelectionBracketMat, 0);
                    num -= 90;
                }
                return false;
            }
            return true;
        }

    }
}
