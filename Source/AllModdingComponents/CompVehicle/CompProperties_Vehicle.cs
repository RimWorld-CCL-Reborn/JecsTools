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
        public bool canBeDowned = false;                    // Does this become downed?
        public bool canWiggleWhenDowned = false;                // Does this wiggle when downed?
        public bool canMoveWithoutHandler = false;            // Is the movement automatic?
        public bool canFireWithoutHandler = false;           // Are the weapons automatic?
        public string labelUndamaged = "Undamaged";
        public string labelDamaged = "Damaged";
        public string labelInoperable = "Inoperable";
        public string labelBroken = "Broken";
        public List<VehicleRole> roles = new List<VehicleRole>(); // Defines roles of passengers.

        //---------- Additions made by Swenzi ------------
        public float fuelConsumptionRate = 80f; //Stores what the fuel usage rate is, i.e. how much fuel is lost
        public float ejectIfBelowNeedPercent = 0.2f; //Unloads passenger when one of their major needs drops below this percent
        public float worldSpeedFactor = 2.0f; // How much faster does this move on the world map than a standard pawn? Caravan speed is calculated from an average of the combined speeds (Vanilla formula)
		public float worldSpeedFactorNoFuel = 0.5f; //Modifies the change in world speed if this vehicle is present but does not have fuel
        public float restNeedRate = 2.0E-05f; //The rate at which the rest need changes while in the vehicle
        public float foodNeedRate = 2.0E-05f; //The rate at which the food need changes while in the vehicle
        public float joyNeedRate = 1.5E-05f; //The rate at which the joy need changes while in the vehicle
        public bool draftStatusChanged = false; //Boolean connected to comp to prevent excessive changing of the draftstatus when forming a caravan
        public int tickCount = 0; //Counter for how long the vehicle has traveled without a driver
        public int momentumTimeSeconds = 2; //Counter for how long the vehicle can travel without a driver in seconds
        public bool warnedOnNoFuel = false; //Boolean connected to comp to prevent spamming of the Caravan No Fuel warning message
        public List<VehicleHandlerGroup> pawnsInVehicle; //Stores the handlergroups of the vehicle and its pawns while the vehicle is in a caravan
       
        //Vehicle type (Can only be one, effects not implemented yet)
        public bool isAir = false; //Is the vehicle an aircraft? i.e. airplane, helicopter
        public bool isLand = false; //Is the vehicle a land based vehicle? i.e. car, tank
        public bool isWater = false; //Is the vehicle a water based vehicle? i.e. boat

        //---------- Additions made by Swenzi end -------------

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
