using RimWorld;
using Verse;

namespace JecsTools
{
    [StaticConstructorOnStartup]
    internal static class JobDefMaker
    {
        public static JobDef JecsTools_UseConsole;

        static JobDefMaker()
        {
            JecsTools_UseConsole = DefDatabase<JobDef>.GetNamedSilentFail("JecsTools_UseConsole");
            if (JecsTools_UseConsole == null)
            {
                JecsTools_UseConsole = new JobDef
                {
                    defName = "JecsTools_UseConsole",
                    driverClass = typeof(JobDriver_UseConsole),
                    reportString = JobDefOf.UseCommsConsole.reportString,
                };
                DefDatabase<JobDef>.Add(JecsTools_UseConsole);
            }
        }
    }
}
