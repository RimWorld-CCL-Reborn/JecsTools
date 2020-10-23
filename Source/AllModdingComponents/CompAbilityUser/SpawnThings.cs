using RimWorld;
using Verse;

namespace AbilityUser
{
    public class SpawnThings : IExposable
    {
        public ThingDef def;
        public FactionDef factionDef;
        public PawnKindDef kindDef;
        public int spawnCount = 1;
        public bool temporary;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, nameof(def));
            Scribe_Defs.Look(ref kindDef, nameof(kindDef));
            Scribe_Defs.Look(ref factionDef, nameof(factionDef));
            Scribe_Values.Look(ref spawnCount, nameof(spawnCount), 1);
            Scribe_Values.Look(ref temporary, nameof(temporary));
        }

        public override string ToString()
        {
            return $"(def={def}, factionDef={factionDef}, kindDef={kindDef}, spawnCount={spawnCount}, temporary={temporary})";
        }
    }
}
