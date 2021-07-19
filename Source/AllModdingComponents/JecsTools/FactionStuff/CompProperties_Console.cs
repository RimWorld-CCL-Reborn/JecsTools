using System;
using Verse;

namespace JecsTools
{
    // See comments on JecsToolsFactionDialogMaker.
    [Obsolete("Hasn't worked properly since RW B19")]
    public class CompProperties_Console : CompProperties
    {
        public bool usesPower = true;
        public int minRelations = 10;
        public int minFreeReinforcements = 50;

        public CompProperties_Console()
        {
            compClass = typeof(CompConsole);
        }
    }
}
