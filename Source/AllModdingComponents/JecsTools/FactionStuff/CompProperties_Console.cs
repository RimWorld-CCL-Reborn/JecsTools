using Verse;

namespace JecsTools
{
    
    public class CompProperties_Console : CompProperties
    {
        public bool usesPower = true;
        public int minRelations = 10;
        public int minFreeReinforcements = 50;

        public CompProperties_Console()
        {
            this.compClass = typeof(CompConsole);
        }

    }
}