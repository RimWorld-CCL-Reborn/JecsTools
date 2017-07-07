using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;
using RimWorld.Planet;

namespace CompVehicle
{
    public class ITab_Passengers : ITab
    {
        private Vector2 scrollPosition;

        private float scrollViewHeight;

        private static List<Need> tmpNeeds = new List<Need>();

        private Pawn specificNeedsTabForPawn;

        private Vector2 thoughtScrollPosition;

        private bool doNeeds;

        private float SpecificNeedsTabWidth
        {
            get
            {
                if (this.specificNeedsTabForPawn == null || this.specificNeedsTabForPawn.Destroyed)
                {
                    return 0f;
                }
                return NeedsCardUtility.GetSize(this.specificNeedsTabForPawn).x;
            }
        }

        public ITab_Passengers()
        {
            this.labelKey = "Contents";
        }

        protected override void FillTab()
        {
            DoRows(this.size, base.SelPawn?.GetComp<CompVehicle>(), ref this.scrollPosition, ref this.scrollViewHeight, false, ref this.specificNeedsTabForPawn, this.doNeeds);
        }

        private static void DoRow(ref float curY, Rect viewRect, Rect scrollOutRect, Vector2 scrollPosition, Thing thing, CompVehicle vehicle, ref Pawn specificNeedsTabForPawn, bool doNeeds)
        {
            float num = (!(thing is Pawn)) ? 30f : 50f;
            float num2 = scrollPosition.y - num;
            float num3 = scrollPosition.y + scrollOutRect.height;
            if (curY > num2 && curY < num3)
            {
                DoRow(new Rect(0f, curY, viewRect.width, num), thing, vehicle, ref specificNeedsTabForPawn, doNeeds);
            }
            curY += num;
        }

        private static void GetNeedsToDisplay(Pawn p, List<Need> outNeeds)
        {
            outNeeds.Clear();
            List<Need> allNeeds = p.needs.AllNeeds;
            for (int i = 0; i < allNeeds.Count; i++)
            {
                Need need = allNeeds[i];
                if (need.def.showForCaravanMembers)
                {
                    outNeeds.Add(need);
                }
            }
            PawnNeedsUIUtility.SortInDisplayOrder(outNeeds);
        }
        
        private static void DoRow(Rect rect, Thing thing, CompVehicle vehicle, ref Pawn specificNeedsTabForPawn, bool doNeeds)
        {
            GUI.BeginGroup(rect);
            Rect rect2 = rect.AtZero();
            Pawn pawn = thing as Pawn;
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
                CaravanPeopleAndItemsTabUtility.DoOpenSpecificTabButton(rect2, pawn, ref specificNeedsTabForPawn);
                rect2.width -= 24f;
            }
            if (pawn == null)
            {
                Rect rect3 = rect2;
                rect3.xMin = rect3.xMax - 60f;
                CaravanPeopleAndItemsTabUtility.TryDrawMass(thing, rect3);
                rect2.width -= 60f;
            }
            if (Mouse.IsOver(rect2))
            {
                Widgets.DrawHighlight(rect2);
            }
            Rect rect4 = new Rect(4f, (rect.height - 27f) / 2f, 27f, 27f);
            Widgets.ThingIcon(rect4, thing, 1f);
            if (pawn != null)
            {
                Rect bgRect = new Rect(rect4.xMax + 4f, 16f, 100f, 18f);
                GenMapUI.DrawPawnLabel(pawn, bgRect, 1f, 100f, null, GameFont.Small, false, false);
                if (doNeeds)
                {
                    GetNeedsToDisplay(pawn, tmpNeeds);
                    float xMax = bgRect.xMax;
                    for (int i = 0; i < tmpNeeds.Count; i++)
                    {
                        Need need = tmpNeeds[i];
                        int maxThresholdMarkers = 0;
                        bool doTooltip = true;
                        Rect rect5 = new Rect(xMax, 0f, 100f, 50f);
                        Need_Mood mood = need as Need_Mood;
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
                Rect rect6 = new Rect(rect4.xMax + 4f, 0f, 300f, 30f);
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
            return things.Any((Thing x) => !(x is Pawn)) || !things.Any<Thing>();
        }

        public static void DoRows(Vector2 size, CompVehicle vehicle, ref Vector2 scrollPosition, ref float scrollViewHeight, bool alwaysShowItemsSection, ref Pawn specificNeedsTabForPawn, bool doNeeds = true)
        {
            //if (specificNeedsTabForPawn != null && (!thing.Contains(specificNeedsTabForPawn) || specificNeedsTabForPawn.Dead))
            //{
            //    specificNeedsTabForPawn = null;
            //}
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, scrollViewHeight);
            //bool listingUsesAbandonSpecificCountButtons = AnyItemOrEmpty(things);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect, true);
            float num = 0f;
            
            if (!vehicle.handlers.NullOrEmpty())
            {
                foreach (VehicleHandlerGroup group in vehicle.handlers)
                {
                    var flag = false;
                    for (int i = 0; i < group.handlers.Count; i++)
                    {
                        Pawn pawn = group.handlers[i] as Pawn;
                        if (!flag)
                        {
                            Widgets.ListSeparator(ref num, viewRect.width, group.role.label.CapitalizeFirst());
                            flag = true;
                        }
                        DoRow(ref num, viewRect, rect, scrollPosition, pawn, vehicle, ref specificNeedsTabForPawn, doNeeds);
                    }

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
            {
                Widgets.ListSeparator(ref num, viewRect.width, "CaravanItems".Translate());
            }

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
            {
                scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
        }

        // RimWorld.Planet.CaravanPeopleAndItemsTabUtility
        private static int MaxNeedsCount(List<Thing> things)
        {
            int num = 0;
            for (int i = 0; i < things.Count; i++)
            {
                Pawn pawn = things[i] as Pawn;
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
            float num = 0f;
            
            //Being super lazy...
            List<Thing> things = new List<Thing>();
            if (!pawns.NullOrEmpty())
            {
                foreach (Pawn p in pawns) things.Add(p as Thing);
            }

            if (things.Any((Thing x) => x is Pawn))
            { 
                num = 100f;
                if (doNeeds)
                {
                    num += (float)MaxNeedsCount(things) * 100f;
                }
                num += 24f;
            }
            float num2 = 0f;
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
            this.size = GetSize(base.SelPawn?.GetComp<CompVehicle>()?.AllOccupants, this.PaneTopY, true);
            if (this.size.x + this.SpecificNeedsTabWidth > (float)UI.screenWidth)
            {
                this.doNeeds = false;
                this.size = GetSize(base.SelPawn?.GetComp<CompVehicle>().AllOccupants, this.PaneTopY, false);
            }
            else
            {
                this.doNeeds = true;
            }
            this.size.y = Mathf.Max(this.size.y, NeedsCardUtility.FullSize.y);
        }

        protected override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            Pawn localSpecificNeedsTabForPawn = this.specificNeedsTabForPawn;
            if (localSpecificNeedsTabForPawn != null)
            {
                Rect tabRect = base.TabRect;
                float specificNeedsTabWidth = this.SpecificNeedsTabWidth;
                Rect rect = new Rect(tabRect.xMax - 1f, tabRect.yMin, specificNeedsTabWidth, tabRect.height);
                Find.WindowStack.ImmediateWindow(1439870015, rect, WindowLayer.GameUI, delegate
                {
                    if (localSpecificNeedsTabForPawn.DestroyedOrNull())
                    {
                        return;
                    }
                    NeedsCardUtility.DoNeedsMoodAndThoughts(rect.AtZero(), localSpecificNeedsTabForPawn, ref this.thoughtScrollPosition);
                    if (Widgets.CloseButtonFor(rect.AtZero()))
                    {
                        this.specificNeedsTabForPawn = null;
                        SoundDefOf.TabClose.PlayOneShotOnCamera(null);
                    }
                }, true, false, 1f);
            }
        }
        
    }
}
