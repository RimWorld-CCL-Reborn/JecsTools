using RimWorld;
using Verse;

namespace JecsTools
{
    public class FactionSettings : DefModExtension
    {
        public SoundDef entrySoundDef = SoundDefOf.ExecuteTrade;
        public string greetingHostileKey = "FactionGreetingHostile";
        public string greetingWaryKey = "FactionGreetingWary";
        public string greetingWarmKey = "FactionGreetingWarm";
        public int waryMinimumRelations = -70;
        public int warmMinimumRelations = 40;
    }
}