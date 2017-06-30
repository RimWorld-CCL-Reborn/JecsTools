using System.Collections.Generic;

namespace CompInstalledPart
{
    public class CompProperties_InstalledPart : CompProperties
    {
        public List<ThingDef> allowedToInstallOn;
        public GraphicData installedWeaponGraphic;
        //public GraphicData installedOverlayGraphic; // Considering adding this at some stage
        public int workToInstall = 500;
        public int workToUninstall = 500;
        public EffecterDef workEffect;
        public CompProperties_InstalledPart() => this.compClass = typeof(CompInstalledPart);
    }
}
