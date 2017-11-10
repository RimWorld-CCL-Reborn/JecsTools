using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
namespace AbilityUser
{
    public class SpawnThings : IExposable
    {
        public ThingDef def = null;
        public PawnKindDef kindDef = null;
        public FactionDef factionDef = null;
        public int spawnCount = 1;
        public bool temporary = false;

        public SpawnThings() { }

        public void ExposeData()
        {
            Scribe_Defs.Look<ThingDef>(ref this.def, "def");
            Scribe_Defs.Look<PawnKindDef>(ref this.kindDef, "kindDef");
            Scribe_Defs.Look<FactionDef>(ref this.factionDef, "factionDef");
            Scribe_Values.Look<int>(ref this.spawnCount, "spawnCount", 1);
            Scribe_Values.Look<bool>(ref this.temporary, "temporary", false);
        }
    }
}
