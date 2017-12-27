using RimWorld;
using UnityEngine;
using Verse;

namespace CompToggleDef
{
    public class ITab_ToggleDef : ITab
    {
        public ITab_ToggleDef()
        {
            size = ToggleDefCardUtility.CardSize + new Vector2(17f, 17f) * 2f;
        }

        public override bool IsVisible
        {
            get
            {
#pragma warning disable IDE0019 // Use pattern matching
                var selected = SelThing as ThingWithComps;
#pragma warning restore IDE0019 // Use pattern matching
                if (selected != null)
                {
                    var td = selected.GetComp<CompToggleDef>();
                    if (td != null)
                    {
                        //Log.Message("ITab_isvisible");
                        labelKey = td.LabelKey; // defined by the Comp
                        return true;
                    }
                }
                return false;
            }
        }

        protected override void FillTab()
        {
            var selected = Find.Selector.SingleSelectedThing as ThingWithComps;
            var td = selected.GetComp<CompToggleDef>();
            if (td == null) Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef");
            labelKey = ((CompProperties_ToggleDef) td.props).labelKey; //"UM_TabToggleDef";//.Translate();
            if (labelKey == null) labelKey = "TOGGLEDEF";
            var rect = new Rect(17f, 17f, ToggleDefCardUtility.CardSize.x, ToggleDefCardUtility.CardSize.y);
            ToggleDefCardUtility.DrawCard(rect, selected);
        }
    }
}