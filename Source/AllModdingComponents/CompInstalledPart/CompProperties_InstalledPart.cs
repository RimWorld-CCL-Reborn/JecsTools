using System.Collections.Generic;
using Verse;
using RimWorld;

namespace CompInstalledPart
{
    public class CompProperties_InstalledPart : CompProperties
    {
        public List<ThingDef> allowedToInstallOn;
        public int workToInstall = 500;
        public int workToUninstall = 500;
        public EffecterDef workEffect = EffecterDefOf.ConstructMetal;

        public CompProperties_InstalledPart() => this.compClass = typeof(CompInstalledPart);
    }
}
