using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CompVehicle
{
    public class ITab_Passengers : ITab
    {
        private static readonly List<Need> tmpNeeds = new List<Need>();

        private bool doNeeds;
        private Vector2 scrollPosition;

        private float scrollViewHeight;

        private Pawn specificNeedsTabForPawn;

        private Vector2 thoughtScrollPosition;

        public ITab_Passengers()
        {
            labelKey = "Contents";
        }

        private float SpecificNeedsTabWidth
        {
            get
            {
                if (specificNeedsTabForPawn == null || specificNeedsTabForPawn.Destroyed)
                    return 0f;
                return NeedsCardUtility.GetSize(specificNeedsTabForPawn).x;
            }
        }

        protected override void FillTab()
        {
            DoRows(size, SelPawn?.GetComp<CompVehicle>(), ref scrollPosition, ref scrollViewHeight, false,
                ref specificNeedsTabForPawn, doNeeds);
        }

        private static void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Vector2 scrollPosition,
            Thing thing, CompVehicle vehicle, ref Pawn specificNeedsTabForPawn, bool doNeeds)
        {
            var num = !(thing is Pawn) ? 30f : 50f;
            var num2 = scrollPosition.y - num;
            var num3 = scrollPosition.y + scrollOutRect.height;
            if (curY > num2 && curY < num3)
                DoRow(new Rect(0f, curY, viewRect.width, num), thing, vehicle, ref specificNeedsTabForPawn, doNeeds);
            curY += num;
        }

        private static void GetNeedsToDisplay(Pawn p, List<Need> outNeeds)
        {
            outNeeds.Clear();
            var allNeeds = p.needs.AllNeeds;
            for (var i = 0; i < allNeeds.Count; i++)
            {
                var need = allNeeds[i];
                if (need.def.showForCaravanMembers)
                    outNeeds.Add(need);
            }
            PawnNeedsUIUtility.SortInDisplayOrder(outNeeds);
        }

        private static void DoRow(Rect rect, Thing thing, CompVehicle vehicle, ref Pawn specificNeedsTabForPawn,
            bool doNeeds)
        {
            GUI.BeginGroup(rect);
            var rect2 = rect.AtZero();
            var pawn = thing as Pawn;
            //if (listingUsesAbandonSpecificCountButtons)
            //{
            //    if (thing.stackCount != 1)
            //    {
            //        CaravanPeopleAndItemsTabUtility.DoAbandonSpecificCountButton(rect2, thing, caravan);
            //    }
            //    rect2.width -= 24f;
            //}
            //CaravanPeopleAndItemsTabUtility.DoAbandonButton(rect2, thing, caravan);
            rect2.width -= 24f;
            Widgets.InfoCardButton(rect2.width - 24f, (rect.height - 24f) / 2f, thing);
            rect2.width -= 24f;
            if (pawn != null && !pawn.Dead)
            {
                //CaravanPeopleAndItemsTabUtility.DoOpenSpecificTabButton(rect2, pawn, ref specificNeedsTabForPawn);
                rect2.width -= 24f;
            }
            if (pawn == null)
            {
                var rect3 = rect2;
                rect3.xMin = rect3.xMax - 60f;
                //CaravanPeopleAndItemsTabUtility.TryDrawMass(thing, rect3);
                rect2.width -= 60f;
            }
            if (Mouse.IsOver(rect2))
                Widgets.DrawHighlight(rect2);
            var rect4 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
            Widgets.ThingIcon(rect4, thing, 1f);
            if (pawn != null)
            {
                var bgRect = new Rect(rect4.xMax + 4f, 16f, 100f, 18f);
                GenMapUI.DrawPawnLabel(pawn, bgRect, 1f, 100f, null, GameFont.Small, false, false);
                if (doNeeds)
                {
                    GetNeedsToDisplay(pawn, tmpNeeds);
                    var xMax = bgRect.xMax;
                    for (var i = 0; i < tmpNeeds.Count; i++)
                    {
                        var need = tmpNeeds[i];
                        var maxThresholdMarkers = 0;
                        var doTooltip = true;
                        var rect5 = new Rect(xMax, 0f, 100f, 50f);
#pragma warning disable IDE0019 // Use pattern matching
                        var mood = need as Need_Mood;
#pragma warning restore IDE0019 // Use pattern matching
                        if (mood != null)
                        {
                            maxThresholdMarkers = 1;
                            doTooltip = false;
                            //TooltipHandler.TipRegion(rect5, new TipSignal(() => CaravanPeopleAndItemsTabUtility.CustomMoodNeedTooltip(mood), rect5.GetHashCode()));
                        }
                        need.DrawOnGUI(rect5, maxThresholdMarkers, 10f, false, doTooltip);
                        xMax = rect5.xMax;
                    }
                }
                if (pawn.Downed)
                {
                    GUI.color = new Color(1f, 0f, 0f, 0.5f);
                    Widgets.DrawLineHorizontal(0f, rect.height / 2f, rect.width);
                    GUI.color = Color.white;
                }
            }
            else
            {
                var rect6 = new Rect(rect4.xMax + 4f, 0f, 300f, 30f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Widgets.Label(rect6, thing.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
            }
            GUI.EndGroup();
        }

        private static bool AnyItemOrEmpty(List<Thing> things)
        {
            return things.Any(x => !(x is Pawn)) || !things.Any();
        }

        public static void DoRows(Vector2 size, CompVehicle vehicle, ref Vector2 scrollPosition,
            ref float scrollViewHeight, bool alwaysShowItemsSection, ref Pawn specificNeedsTabForPawn,
            bool doNeeds = true)
        {
            //if (specificNeedsTabForPawn != null && (!thing.Contains(specificNeedsTabForPawn) || specificNeedsTabForPawn.Dead))
            //{
            //    specificNeedsTabForPawn = null;
            //}
            Text.Font = GameFont.Small;
            var rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            var viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
            //bool listingUsesAbandonSpecificCountButtons = AnyItemOrEmpty(things);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
            var num = 0f;

            if (!vehicle.handlers.NullOrEmpty())
                foreach (var group in vehicle.handlers)
                    if ((group?.handlers?.Count ?? 0) > 0)
                    {
                        var flag = false;
                        for (var i = 0; i < group.handlers.Count; i++)
                        {
                            var pawn = group.handlers[i];
                            if (!flag)
                            {
                                Widgets.ListSeparator(ref num, viewRect.width, group.role.label.CapitalizeFirst());
                                flag = true;
                            }
                            DoRow(ref num, viewRect, rect, scrollPosition, pawn, vehicle, ref specificNeedsTabForPawn,
                                doNeeds);
                        }
                    }
            //bool flag2 = false;
            //for (int j = 0; j < things.Count; j++)
            //{
            //    Pawn pawn2 = things[j] as Pawn;
            //    if (pawn2 != null && !pawn2.IsColonist)
            //    {
            //        if (!flag2)
            //        {
            //            Widgets.ListSeparator(ref num, viewRect.width, "CaravanPrisonersAndAnimals".Translate());
            //            flag2 = true;
            //        }
            //        DoRow(ref num, viewRect, rect, scrollPosition, pawn2, vehicle, ref specificNeedsTabForPawn, doNeeds, listingUsesAbandonSpecificCountButtons);
            //    }
            //}
            //bool flag3 = false;
            if (alwaysShowItemsSection)
                Widgets.ListSeparator(ref num, viewRect.width, "CaravanItems".Translate());

            //for (int k = 0; k < vehicle.Pawn.inventory.innerContainer.Count; k++)
            //{
            //    if (!(things[k] is Pawn))
            //    {
            //        if (!flag3)
            //        {
            //            if (!alwaysShowItemsSection)
            //            {
            //                Widgets.ListSeparator(ref num, viewRect.width, "CaravanItems".Translate());
            //            }
            //            flag3 = true;
            //        }
            //        DoRow(ref num, viewRect, rect, scrollPosition, things[k], vehicle, ref specificNeedsTabForPawn, doNeeds, listingUsesAbandonSpecificCountButtons);
            //    }
            //}
            //if (alwaysShowItemsSection && !flag3)
            //{
            //    GUI.color = Color.gray;
            //    Text.Anchor = TextAnchor.UpperCenter;
            //    Widgets.Label(new Rect(0f, num, viewRect.width, 25f), "NoneBrackets".Translate());
            //    Text.Anchor = TextAnchor.UpperLeft;
            //    num += 25f;
            //    GUI.color = Color.white;
            //}
            if (Event.current.type == EventType.Layout)
                scrollViewHeight = num + 30f;
            Widgets.EndScrollView();
        }

        // RimWorld.Planet.CaravanPeopleAndItemsTabUtility
        private static int MaxNeedsCount(List<Thing> things)
        {
            var num = 0;
            for (var i = 0; i < things.Count; i++)
            {
#pragma warning disable IDE0019 // Use pattern matching
                var pawn = things[i] as Pawn;
#pragma warning restore IDE0019 // Use pattern matching
                if (pawn != null)
                {
                    GetNeedsToDisplay(pawn, tmpNeeds);
                    num = Mathf.Max(num, tmpNeeds.Count);
                }
            }
            return num;
        }


        // RimWorld.Planet.CaravanPeopleAndItemsTabUtility
        public static Vector2 GetSize(List<Pawn> pawns, float paneTopY, bool doNeeds = true)
        {
            var num = 0f;

            //Being super lazy...
            var things = new List<Thing>();
            if (!pawns.NullOrEmpty())
                foreach (var p in pawns) things.Add(p);

            if (things.Any(x => x is Pawn))
            {
                num = 100f;
                if (doNeeds)
                    num += MaxNeedsCount(things) * 100f;
                num += 24f;
            }
            var num2 = 0f;
            if (AnyItemOrEmpty(things))
            {
                num2 = 300f;
                num2 += 24f;
                num2 += 60f;
            }
            Vector2 result;
            result.x = 103f + Mathf.Max(num, num2) + 16f;
            result.y = Mathf.Min(550f, paneTopY - 30f);
            return result;
        }


        protected override void UpdateSize()
        {
            base.UpdateSize();
            size = GetSize(SelPawn?.GetComp<CompVehicle>()?.AllOccupants, PaneTopY, true);
            if (size.x + SpecificNeedsTabWidth > UI.screenWidth)
            {
                doNeeds = false;
                size = GetSize(SelPawn?.GetComp<CompVehicle>().AllOccupants, PaneTopY, false);
            }
            else
            {
                doNeeds = true;
            }
            size.y = Mathf.Max(size.y, NeedsCardUtility.FullSize.y);
        }

        protected override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            var localSpecificNeedsTabForPawn = specificNeedsTabForPawn;
            if (localSpecificNeedsTabForPawn != null)
            {
                var tabRect = TabRect;
                var specificNeedsTabWidth = SpecificNeedsTabWidth;
                var rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificNeedsTabWidth, tabRect.height);
                Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
                {
                    if (localSpecificNeedsTabForPawn.DestroyedOrNull())
                        return;
                    NeedsCardUtility.DoNeedsMoodAndThoughts(rect.AtZero(), localSpecificNeedsTabForPawn,
                        ref thoughtScrollPosition);
                    if (Widgets.CloseButtonFor(rect.AtZero()))
                    {
                        specificNeedsTabForPawn = null;
                        SoundDefOf.TabClose.PlayOneShotOnCamera(null);
                    }
                }, true, false, 1f);
            }
        }
    }
}