namespace CompActivatableEffect
{
    public class CompProperties_ActivatableEffect : CompProperties
    {
        public GraphicData graphicData;

        public AltitudeLayer altitudeLayer;

        public string ActivateLabel;
        public string DeactivateLabel;

        public string uiIconPathActivate;
        public string uiIconPathDeactivate;

        public SoundDef activateSound;
        public SoundDef sustainerSound;
        public SoundDef deactivateSound;

        public bool gizmosOnEquip = false;

        public bool autoActivateOnDraft = true;

        public bool draftToUseGizmos = true;

        public float Altitude => Altitudes.AltitudeFor(this.altitudeLayer);

        public CompProperties_ActivatableEffect() => this.compClass = typeof(CompActivatableEffect);
    }
}
