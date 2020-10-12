using RimWorld;
using Verse;

namespace CompToggleDef
{
    public class ITab_ToggleDef : ITab
    {
        public override bool IsVisible
        {
            get
            {
                var compToggleDef = SelThing.TryGetCompToggleDef();
                if (!ToggleDefCardUtility.CanShowCard(compToggleDef))
                    return false;
                // InspectPaneUtility calls IsVisible before drawing the tab text (labelKey.Translate()),
                // so this is a convenient hook to set labelKey.
                labelKey = compToggleDef.Props.labelKey;
                return true;
            }
        }

        protected override void UpdateSize()
        {
            base.UpdateSize();
            var compToggleDef = SelThing.TryGetCompToggleDef();
            if (!ToggleDefCardUtility.CanShowCard(compToggleDef))
            {
                Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef");
                return;
            }
            size = ToggleDefCardUtility.CardSize(compToggleDef);
        }

        protected override void FillTab()
        {
            var compToggleDef = SelThing.TryGetCompToggleDef();
            if (!ToggleDefCardUtility.CanShowCard(compToggleDef))
            {
                Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef");
                return;
            }
            labelKey = compToggleDef.Props.labelKey;
            ToggleDefCardUtility.DrawCard(size, compToggleDef);
        }
    }
}
