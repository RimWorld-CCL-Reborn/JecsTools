using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CompToggleDef
{
    public static class ToggleDefCardUtility
    {
        private const int MaxRows = 10;
        private const float CardPadding = GenUI.GapSmall;
        private const float ExtraTopPadding = 20f;
        private const float RowHeight = Widgets.RadioButtonSize;
        private const float RowGap = GenUI.GapSmall;
        private const float DefIconMargin = 2f;
        private const float DefIconSize = RowHeight - DefIconMargin * 2;
        private const float DefLabelOffsetX = 6f;

        public static CompToggleDef GetCompToggleDef(Thing thing)
        {
            // TODO: Support all the equipment/apparel/carried things on a Pawn somehow?
            // Would require showing multiple CompToggleDefs and some way to switch between them in the GUI.
            if (thing is Pawn pawn && pawn.IsColonistPlayerControlled)
                thing = pawn.equipment?.Primary;
            var compToggleDef = thing.TryGetCompToggleDef();
            if (compToggleDef == null)
                return null;
            var toggleDefs = compToggleDef.Props.toggleDefs;
            if (toggleDefs == null || toggleDefs.Count <= 1)
                return null;
            return compToggleDef;
        }

        public static Vector2 CardSize(CompToggleDef compToggleDef)
        {
            var width = InspectPaneUtility.PaneWidthFor((MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow);
            var rowCount = Math.Min(compToggleDef.Props.toggleDefs.Count, MaxRows);
            return new Vector2(width, TotalRowHeight(rowCount) + CardPadding * 2 + ExtraTopPadding);
        }

        private static float TotalRowHeight(int rowCount) => rowCount * (RowHeight + RowGap) - RowGap;

        private static ThingWithComps lastSelectedThing;
        private static Vector2 scrollPosition = Vector2.zero;

        public static void DrawCard(Vector2 size, CompToggleDef compToggleDef)
        {
            var selectedThing = compToggleDef.parent;
            var toggleDefs = compToggleDef.Props.toggleDefs;

            var rect = new Rect(0f, ExtraTopPadding, size.x, size.y - ExtraTopPadding).ContractedBy(CardPadding);

            var rowCount = toggleDefs.Count;
            var yMin = -RowHeight;
            var yMax = rect.height;
            if (rowCount > MaxRows)
            {
                if (lastSelectedThing != selectedThing)
                {
                    lastSelectedThing = selectedThing;
                    scrollPosition.y = toggleDefs.IndexOf(selectedThing.def) * (RowHeight + RowGap);
                }
                var viewRect = new Rect(0f, 0f, rect.width - GenUI.ScrollBarWidth - CardPadding, TotalRowHeight(rowCount));
                Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
                rect.width = viewRect.width;
                yMin += scrollPosition.y;
                yMax += scrollPosition.y;
            }
            else
            {
                GUI.BeginGroup(rect);
            }

            // Add the rows for each toggle def.
            var y = 0f;
            foreach (var toggleDef in toggleDefs)
            {
                if (y > yMax)
                    break;
                if (y >= yMin)
                {
                    var rowRect = new Rect(0f, y, rect.width, RowHeight);
                    var iconAndLabelRect = rowRect.LeftPartPixels(rowRect.width - (Widgets.RadioButtonSize + DefLabelOffsetX));
                    Widgets.DefLabelWithIcon(iconAndLabelRect, toggleDef, DefIconMargin, DefLabelOffsetX);
                    var iconRect = new Rect(DefIconMargin, y + DefIconMargin, DefIconSize, DefIconSize);
                    if (Widgets.ButtonInvisible(iconRect))
                        Find.WindowStack.Add(new Dialog_InfoCard(toggleDef));
                    var isSelected = selectedThing.def == toggleDef;
                    RadioButtonDraw(rowRect.width - Widgets.RadioButtonSize, y, isSelected);
                    if (!isSelected && Widgets.ButtonInvisible(rowRect.RightPartPixels(rowRect.width - iconRect.xMax - DefLabelOffsetX)))
                    {
                        SwapThing(selectedThing, toggleDef);
                        break;
                    }
                }
                y += RowHeight + RowGap;
            }

            if (rowCount > MaxRows)
                Widgets.EndScrollView();
            else
                GUI.EndGroup();
        }

        private static void SwapThing(ThingWithComps thing, ThingDef newDef)
        {
            var map = thing.Map;
            var loc = thing.Position;
            var rot = thing.Rotation;

            var eqTracker = thing.ParentHolder as Pawn_EquipmentTracker;
            if (eqTracker != null)
                eqTracker.Remove(thing);
            else
                thing.DeSpawn();

            thing.def = newDef;

            // Refresh verbs.
            foreach (var comp in thing.AllComps)
            {
                if (comp is IVerbOwner verbOwner && verbOwner.VerbTracker is VerbTracker verbTracker)
                {
                    VerbsNeedReinitOnLoad(verbTracker);
                    _ = verbTracker.AllVerbs;
                }
            }

            // Refresh graphics.
            thing.Notify_ColorChanged();

            if (eqTracker != null)
                eqTracker.AddEquipment(thing);
            else if (GenSpawn.Spawn(thing, loc, map, rot) != null)
                Find.Selector.Select(thing, playSound: false);
        }

        private static readonly Action<float, float, bool> RadioButtonDraw =
            (Action<float, float, bool>)AccessTools.Method(typeof(Widgets), "RadioButtonDraw")
                .CreateDelegate(typeof(Action<float, float, bool>));
        private static readonly Action<VerbTracker> VerbsNeedReinitOnLoad = InitVerbsNeedReinitOnLoad();

        private static Action<VerbTracker> InitVerbsNeedReinitOnLoad()
        {
            // VerbTracker.VerbsNeedReinitOnLoad is only available in RW 1.2+
            var method = AccessTools.Method(typeof(VerbTracker), "VerbsNeedReinitOnLoad");
            if (method != null)
                return (Action<VerbTracker>)method.CreateDelegate(typeof(Action<VerbTracker>));
            var verbsRef = AccessTools.FieldRefAccess<VerbTracker, List<Verb>>("verbs");
            return verbTracker => verbsRef(verbTracker) = null;
        }
    }
}
