using RimWorld;
using Verse;

namespace CompVehicle
{
    public class CompProperties_VehicleSpawner : CompProperties
    {
        public float assemblyTime = 20f; //In seconds
        public string useVerb = "Assemble {0}";
        public PawnKindDef vehicleToSpawn = null;
        public EffecterDef workEffect = EffecterDefOf.ConstructMetal;

        public CompProperties_VehicleSpawner()
        {
            compClass = typeof(CompVehicleSpawner);
        }
    }
}