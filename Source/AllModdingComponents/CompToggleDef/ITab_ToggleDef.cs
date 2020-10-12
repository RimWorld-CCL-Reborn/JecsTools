using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CompToggleDef
{
    public class ITab_ToggleDef : ITab
    {
        // Seperate class since ITab_ToggleDef is loaded (and thus static constructor runs) too early.
        [StaticConstructorOnStartup]
        static class AddITabOnStartup
        {
            static AddITabOnStartup()
            {
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.category == ThingCategory.Pawn && def.inspectorTabsResolved is List<InspectTabBase> tabs)
                    {
                        var toggleDefTab = InspectTabManager.GetSharedInstance(typeof(ITab_ToggleDef));
                        var gearTabIndex = tabs.FindIndex(tab => tab is ITab_Pawn_Gear);
                        if (gearTabIndex < 0)
                            tabs.Add(toggleDefTab);
                        else
                            tabs.Insert(gearTabIndex + 1, toggleDefTab);
                    }
                }
            }
        }

        public override bool IsVisible
        {
            get
            {
                var compToggleDef = ToggleDefCardUtility.GetCompToggleDef(SelThing);
                if (compToggleDef == null)
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
            var compToggleDef = ToggleDefCardUtility.GetCompToggleDef(SelThing);
            if (compToggleDef == null)
            {
                Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef");
                return;
            }
            size = ToggleDefCardUtility.CardSize(compToggleDef);
        }

        protected override void FillTab()
        {
            var compToggleDef = ToggleDefCardUtility.GetCompToggleDef(SelThing);
            if (compToggleDef == null)
            {
                Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef");
                return;
            }
            labelKey = compToggleDef.Props.labelKey;
            ToggleDefCardUtility.DrawCard(size, compToggleDef);
        }
    }
}
