using System.Collections.Generic;
using Verse;

namespace CompVehicle
{
    public class CompProperties_Vehicle : CompProperties
    {
        public SoundDef soundEntry = null;
        public SoundDef soundEject = null;
        public float ejectIfBelowHealthPercent = 0.0f;      // Unloads all passengers when health percentage drops below this point
        public float seatHitDamageFactor = 2.0f;            // Multiply & apply this damage to passengers in seats
        public float seatHitCriticalHitChance = 0.05f;      // Chance of doubling the damage.
        public float worldSpeedFactor = 1.0f;               // How much faster does this move on the world map than a standard pawn? The slowest vehicle is the speed the caravan will take.
        public bool canBeDowned = false;                    // Does this become downed?
        public bool canWiggleWhenDowned = false;                // Does this wiggle when downed?
        public bool canMoveWithoutHandler = false;            // Is the movement automatic?
        public bool canFireWithoutHandler = false;           // Are the weapons automatic?
        public string labelUndamaged = "Undamaged";
        public string labelDamaged = "Damaged";
        public string labelInoperable = "Inoperable";
        public string labelBroken = "Broken";
        public List<VehicleRole> roles = new List<VehicleRole>(); // Defines roles of passengers.

        public int TotalCapacity
        {
            get
            {
                int result = 0;
                if (this.roles != null && this.roles.Count > 0)
                {
                    foreach (VehicleRole role in this.roles)
                    {
                        result += role.slots;
                    }
                }
                return result;
            }
        }

        public CompProperties_Vehicle() => this.compClass = typeof(CompVehicle);
    }
}
