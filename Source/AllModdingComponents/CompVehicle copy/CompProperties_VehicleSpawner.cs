using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace CompVehicle
{
    public class CompProperties_VehicleSpawner : CompProperties
    {
        public string useVerb = "Assemble {0}";
        public PawnKindDef vehicleToSpawn = null;
        public float assemblyTime = 20f; //In seconds
        public EffecterDef workEffect = EffecterDefOf.ConstructMetal;

        public CompProperties_VehicleSpawner() => this.compClass = typeof(CompVehicleSpawner);
    }
}
