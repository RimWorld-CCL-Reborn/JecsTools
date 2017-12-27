using System.Collections.Generic;
using Verse;

namespace CompInstalledPart
{
    public class CompProperties_InstalledPart : CompProperties
    {
        public List<ThingDef> allowedToInstallOn;
        public GraphicData installedWeaponGraphic;

        public EffecterDef workEffect;

        //public GraphicData installedOverlayGraphic; // Considering adding this at some stage
        public int workToInstall = 500;

        public int workToUninstall = 500;

        public CompProperties_InstalledPart()
        {
            compClass = typeof(CompInstalledPart);
        }
    }
}