using System.Linq;
using RimWorld;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    static class JobDefMaker
    {
        static JobDefMaker()
        {
            if (DefDatabase<JobDef>.AllDefs.Any(x => x.defName == "JecsTools_UseConsole")) return;
            var newDef = new JobDef
            {
                defName = "JecsTools_UseConsole",
                driverClass = typeof(JecsTools.JobDriver_UseConsole),
                reportString = JobDefOf.UseCommsConsole.reportString
            };
            DefDatabase<JobDef>.Add(newDef);
        }

    }
}
