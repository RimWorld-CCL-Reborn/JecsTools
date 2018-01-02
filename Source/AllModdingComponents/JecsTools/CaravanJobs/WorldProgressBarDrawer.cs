using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using RimWorld;

namespace JecsTools
{
    public static class WorldProgressBarDrawer
    {
        private const float BaseSelectedTexJump = 25f;

        private const float BaseSelectedTexScale = 0.4f;

        private const float BaseSelectionRectSize = 35f;

        private static Dictionary<WorldObject, float> selectTimes = new Dictionary<WorldObject, float>();

        private static readonly Color HiddenSelectionBracketColor = new Color(1f, 1f, 1f, 0.35f);

        private static Vector2[] bracketLocs = new Vector2[4];

        public static Dictionary<WorldObject, float> SelectTimes
        {
            get
            {
                return WorldProgressBarDrawer.selectTimes;
            }
        }

        public static void Notify_Selected(WorldObject t)
        {
            WorldProgressBarDrawer.selectTimes[t] = Time.realtimeSinceStartup;
        }

        public static void Clear()
        {
            WorldProgressBarDrawer.selectTimes.Clear();
        }

        //public static void SelectionOverlaysOnGUI()
        //{
        //    List<WorldObject> selectedObjects = Find.WorldSelector.SelectedObjects;
        //    for (int i = 0; i < selectedObjects.Count; i++)
        //    {
        //        WorldObject worldObject = selectedObjects[i];
        //        WorldProgressBarDrawer.DrawSelectionBracketOnGUIFor(worldObject);
        //        worldObject.ExtraSelectionOverlaysOnGUI();
        //    }
        //}

        public static void DrawSelectionOverlays()
        {
            List<WorldObject> selectedObjects = Find.WorldSelector.SelectedObjects;
            for (int i = 0; i < selectedObjects.Count; i++)
            {
                WorldObject worldObject = selectedObjects[i];
                worldObject.DrawExtraSelectionOverlays();
            }
        }

        public static void DrawProgressBarOnGUIFor(WorldObject obj, float curProgress)
        {
            Vector2 vector = obj.ScreenPos();
            Rect rect = new Rect(vector.x - 17.5f, vector.y - 17.5f, 35f, 35f);
            Vector2 textureSize = new Vector2((float)SelectionDrawerUtility.SelectedTexGUI.width * 0.4f, (float)SelectionDrawerUtility.SelectedTexGUI.height * 0.4f);
            SelectionDrawerUtility.CalculateSelectionBracketPositionsUI<WorldObject>(WorldProgressBarDrawer.bracketLocs, obj, rect, WorldProgressBarDrawer.selectTimes, textureSize, 25f);
            //if (obj.HiddenBehindTerrainNow())
            //{
            //    GUI.color = WorldProgressBarDrawer.HiddenSelectionBracketColor;
            //}
            //else
            //{
            //    GUI.color = Color.white;
            //}
            //int num = 90;
            //for (int i = 0; i < 4; i++)
            //{
                //if (i == 2)
            DrawScalingTextureRotated(WorldProgressBarDrawer.bracketLocs[2], LearningReadout.ProgressBarFillTex, 90f, curProgress, 0.4f);
                //num += 90;
            //}
            //GUI.color = Color.white;
        }

        // Verse.Widgets
        public static void DrawScalingTextureRotated(Vector2 center, Texture2D tex, float angle, float scaleWidth = 1f, float scaleHeight = 1f)
        {
            float num = (float)tex.width * scaleWidth;
            float num2 = (float)tex.height * scaleHeight;
            Rect rect = new Rect(center.x - num / 2f, center.y - num2 / 2f, num, num2);
            //Widgets.DrawTextureRotated(rect, tex, angle);
        }

    }
}
