namespace CompToggleDef
{
    public class ITab_ToggleDef: ITab
    {
        public override bool IsVisible
        {
            get
            {
                ThingWithComps selected = this.SelThing as ThingWithComps;
                if ( selected != null ) {
                    CompToggleDef td = selected.GetComp<CompToggleDef>();
                    if (  td != null ) {
//                        Log.Message("ITab_isvisible");
                        this.labelKey = td.LabelKey; // defined by the Comp
                        return true;
                    }
                }
                return false;
            }
        }

        public ITab_ToggleDef()
        {
            this.size = ToggleDefCardUtility.CardSize + new Vector2(17f, 17f) * 2f;
        }

        protected override void FillTab()
        {
            ThingWithComps selected = Find.Selector.SingleSelectedThing as ThingWithComps;
            CompToggleDef td = selected.GetComp<CompToggleDef>();
            if ( td == null ) { Log.Warning("selected thing has no CompToggleDef for ITab_ToggleDef"); }
            this.labelKey = ((CompProperties_ToggleDef)td.props).labelKey;//"UM_TabToggleDef";//.Translate();
            if ( this.labelKey == null ) this.labelKey = "TOGGLEDEF";
            Rect rect = new Rect(17f, 17f, ToggleDefCardUtility.CardSize.x, ToggleDefCardUtility.CardSize.y);
            ToggleDefCardUtility.DrawCard(rect, selected);
        }
    }
}
