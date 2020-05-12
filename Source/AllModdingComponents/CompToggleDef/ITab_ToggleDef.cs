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
                var td = SelThing.TryGetComp<CompToggleDef>();
                if (td != null)
                {
                    //Log.Message("ITab_isvisible");
                    labelKey = td.LabelKey; // defined by the Comp
                    return true;
                }
                return false;
            }
        }

        protected override void FillTab()
        {
            if (!(Find.Selector.SingleSelectedThing is ThingWithComps selected && selected.GetComp<CompToggleDef>() is CompToggleDef td))
            {
                Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef");
                return;
            }
            labelKey = td.LabelKey ?? "TOGGLEDEF";
            var rect = new Rect(17f, 17f, ToggleDefCardUtility.CardSize.x, ToggleDefCardUtility.CardSize.y);
            ToggleDefCardUtility.DrawCard(rect, selected);
        }
    }
}