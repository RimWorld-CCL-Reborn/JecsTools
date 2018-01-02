using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompVehicle
{
    public class ITab_Passengers2 : ITab
    {
        private const float RowHeight = 50f;

        private const float PawnLabelHeight = 18f;

        private const float PawnLabelColumnWidth = 100f;

        private const float SpaceAroundIcon = 4f;

        private const float PawnCapacityColumnWidth = 100f;

        private static readonly List<PawnCapacityDef> capacitiesToDisplay = new List<PawnCapacityDef>();

        private bool compactMode;

        private Vector2 scrollPosition;

        private float scrollViewHeight;

        private Pawn specificHealthTabForPawn;

        public ITab_Passengers2()
        {
            labelKey = "Contents";
        }

        private CompVehicle CompPilot
        {
            get
            {
                CompVehicle result = null;
                if (SelPawn != null)
                {
                    var compPilotable = SelPawn.GetComp<CompVehicle>();
                    if (compPilotable != null)
                        result = compPilotable;
                }
                return result;
            }
        }

        private List<Pawn> Pawns
        {
            get
            {
                List<Pawn> result = null;
                if (SelPawn != null)
                    if (CompPilot != null)
                        result = CompPilot.AllOccupants;
                return result;
            }
        }

        private List<PawnCapacityDef> CapacitiesToDisplay
        {
            get
            {
                capacitiesToDisplay.Clear();
                var allDefsListForReading = DefDatabase<PawnCapacityDef>.AllDefsListForReading;
                for (var i = 0; i < allDefsListForReading.Count; i++)
                    if (allDefsListForReading[i].showOnCaravanHealthTab)
                        capacitiesToDisplay.Add(allDefsListForReading[i]);
                capacitiesToDisplay.SortBy(x => x.listOrder);
                return capacitiesToDisplay;
            }
        }

        private float SpecificHealthTabWidth
        {
            get
            {
                if (specificHealthTabForPawn == null)
                    return 0f;
                return 630f;
            }
        }

        protected override void FillTab()
        {
            Text.Font = GameFont.Small;
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            var rect2 = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
            var num = 0f;
            Widgets.BeginScrollView(rect, ref scrollPosition, rect2, true);
            DoColumnHeaders(ref num);
            DoRows(ref num, rect2, rect);
            if (Event.current.type == EventType.Layout)
                scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            size = GetRawSize(false);
            if (size.x + SpecificHealthTabWidth > UI.screenWidth)
            {
                compactMode = true;
                size = GetRawSize(true);
            }
            else
            {
                compactMode = false;
            }
        }

        protected override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            var localSpecificHealthTabForPawn = specificHealthTabForPawn;
            if (localSpecificHealthTabForPawn != null)
            {
                var tabRect = TabRect;
                var specificHealthTabWidth = SpecificHealthTabWidth;
                var rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificHealthTabWidth, tabRect.height);
                Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
                {
                    if (localSpecificHealthTabForPawn.DestroyedOrNull())
                        return;
                    var outRect = new Rect(0f, 20f, rect.width, rect.height - 20f);
                    HealthCardUtility.DrawPawnHealthCard(outRect, localSpecificHealthTabForPawn, false, true,
                        localSpecificHealthTabForPawn);
                    if (Widgets.CloseButtonFor(rect.AtZero()))
                    {
                        specificHealthTabForPawn = null;
                        SoundDefOf.TabClose.PlayOneShotOnCamera(null);
                    }
                }, true, false, 1f);
            }
        }

        private void DoColumnHeaders(ref float curY)
        {
            if (!compactMode)
            {
                var num = 135f;
                Text.Anchor = TextAnchor.UpperCenter;
                GUI.color = Widgets.SeparatorLabelColor;
                Widgets.Label(new Rect(num, 3f, 100f, 100f), "Pain".Translate());
                var list = CapacitiesToDisplay;
                for (var i = 0; i < list.Count; i++)
                {
                    num += 100f;
                    Widgets.Label(new Rect(num, 3f, 100f, 100f), list[i].LabelCap);
                }
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        private void DoRows(ref float curY, Rect scrollViewRect, Rect scrollOutRect)
        {
            var pawns = Pawns;
            if (specificHealthTabForPawn != null && !pawns.Contains(specificHealthTabForPawn))
                specificHealthTabForPawn = null;
            if (CompPilot.handlers != null && CompPilot.handlers.Count > 0)
                foreach (var group in CompPilot.handlers)
                {
                    var flag = false;
                    for (var i = 0; i < pawns.Count; i++)
                    {
                        var pawn = pawns[i];
                        if (group.handlers.Contains(pawn))
                        {
                            if (!flag)
                            {
                                Widgets.ListSeparator(ref curY, scrollViewRect.width,
                                    group.role.labelPlural.CapitalizeFirst());
                                flag = true;
                            }
                            DoRow(ref curY, scrollViewRect, scrollOutRect, pawn);
                        }
                    }
                }
        }

        private Vector2 GetRawSize(bool compactMode)
        {
            var num = 100f;
            if (!compactMode)
            {
                num += 100f;
                num += CapacitiesToDisplay.Count * 100f;
            }
            Vector2 result;
            result.x = 127f + num + 16f;
            result.y = Mathf.Min(550f, PaneTopY - 30f);
            return result;
        }

        private void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Pawn p)
        {
            var num = scrollPosition.y - 50f;
            var num2 = scrollPosition.y + scrollOutRect.height;
            if (curY > num && curY < num2)
                DoRow(new Rect(0f, curY, viewRect.width, 50f), p);
            curY += 50f;
        }

        private void DoRow(Rect rect, Pawn p)
        {
            GUI.BeginGroup(rect);
            var rect2 = rect.AtZero();
            //CaravanPeopleAndItemsTabUtility.DoAbandonButton(rect2, p, base.SelCaravan);
            rect2.width -= 24f;
            Widgets.InfoCardButton(rect2.width - 24f, (rect.height - 24f) / 2f, p);
            rect2.width -= 24f;
            //CaravanPeopleAndItemsTabUtility.DoOpenSpecificTabButton(rect2, p, ref this.specificHealthTabForPawn);
            rect2.width -= 24f;
            if (Mouse.IsOver(rect2))
                Widgets.DrawHighlight(rect2);
            var rect3 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
            Widgets.ThingIcon(rect3, p, 1f);
            var bgRect = new Rect(rect3.xMax + 4f, 16f, 100f, 18f);
            GenMapUI.DrawPawnLabel(p, bgRect, 1f, 100f, null, GameFont.Small, false, false);
            if (!compactMode)
            {
                var num = bgRect.xMax;
                if (p.RaceProps.IsFlesh)
                {
                    var rect4 = new Rect(num, 0f, 100f, 50f);
                    DoPain(rect4, p);
                }
                var list = CapacitiesToDisplay;
                for (var i = 0; i < list.Count; i++)
                {
                    num += 100f;
                    var rect5 = new Rect(num, 0f, 100f, 50f);
                    if (!p.RaceProps.Humanlike || list[i].showOnHumanlikes)
                        if (!p.RaceProps.Animal || list[i].showOnAnimals)
                            if (!p.RaceProps.IsMechanoid || list[i].showOnMechanoids)
                                if (PawnCapacityUtility.BodyCanEverDoCapacity(p.RaceProps.body, list[i]))
                                    DoCapacity(rect5, p, list[i]);
                }
            }
            if (p.Downed)
            {
                GUI.color = new Color(1f, 0f, 0f, 0.5f);
                Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
                GUI.color = Color.white;
            }
            GUI.EndGroup();
        }

        private void DoPain(Rect rect, Pawn pawn)
        {
            var painLabel = HealthCardUtility.GetPainLabel(pawn);
            var painTip = HealthCardUtility.GetPainTip(pawn);
            if (Mouse.IsOver(rect))
                Widgets.DrawHighlight(rect);
            GUI.color = painLabel.Second;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, painLabel.First);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, painTip);
        }

        private void DoCapacity(Rect rect, Pawn pawn, PawnCapacityDef capacity)
        {
            var efficiencyLabel = HealthCardUtility.GetEfficiencyLabel(pawn, capacity);
            var pawnCapacityTip = HealthCardUtility.GetPawnCapacityTip(pawn, capacity);
            if (Mouse.IsOver(rect))
                Widgets.DrawHighlight(rect);
            GUI.color = efficiencyLabel.Second;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, efficiencyLabel.First);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, pawnCapacityTip);
        }
    }
}