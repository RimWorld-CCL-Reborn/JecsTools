using Verse;

namespace CompActivatableEffect
{
    public class CompProperties_ActivatableEffect : CompProperties
    {
        public string ActivateLabel;

        public SoundDef activateSound;

        public AltitudeLayer altitudeLayer;

        public bool autoActivateOnDraft = true;
        public string DeactivateLabel;
        public SoundDef deactivateSound;

        public bool draftToUseGizmos = true;

        public bool gizmosOnEquip = false;
        public GraphicData graphicData;
        public SoundDef sustainerSound;

        public string uiIconPathActivate;
        public string uiIconPathDeactivate;

        public CompProperties_ActivatableEffect()
        {
            compClass = typeof(CompActivatableEffect);
        }

        public float Altitude => Altitudes.AltitudeFor(altitudeLayer);
    }
}