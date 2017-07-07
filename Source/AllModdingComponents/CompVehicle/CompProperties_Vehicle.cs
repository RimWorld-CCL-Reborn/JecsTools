using System.Collections.Generic;
using Verse;

namespace CompVehicle
{
    public enum VehicleType
    {
        Aircraft,
        AircraftAmphibious,
        AircraftSpace,
        Amphibious,
        LandWheeled,
        LandHover,
        LandWalker,
        Sea,
        SeaSubmarine,
        SpaceOuter
    }
    
    public enum HandlingType : int
    {
        Incapable = 0,
        HandlerRequired = 1,
        NoHandlerRequired = 2
    }

    public class CompProperties_Vehicle : CompProperties
    {
        public VehicleType vehicleType = VehicleType.LandWheeled;             // Defaults to wheeled land vehicles.
        public HandlingType movementHandling = HandlingType.HandlerRequired;  // Is the movement automatic?
        public HandlingType weaponHandling = HandlingType.HandlerRequired;    // Are the weapons automatic?
        public HandlingType manipulationHandling = HandlingType.Incapable;    // Does this vehicle rely on others to load it? Or can it "pick" up objects?
        public float ejectIfBelowHealthPercent = 0.0f;                        // Unloads all passengers when health percentage drops below this point
        public float seatHitDamageFactor = 2.0f;                              // Multiply & apply this damage to passengers in seats
        public float seatHitCriticalHitChance = 0.05f;                        // Chance of doubling the damage.
        public bool canBeDowned = false;                                      // Does this become downed?
        public bool canWiggleWhenDowned = false;                              // Does this wiggle when downed?
        public SoundDef soundEntry = null;                                    // Sound for when a character enters the vehicle.
        public SoundDef soundEject = null;                                    // Sound when a character exits the vehicle.
        public SoundDef soundMoving = null;                                   // For a sound to play while the vehicle is moving.
        public string labelUndamaged = "Undamaged";                           // Label replacer for when the vehicle is undamaged.
        public string labelDamaged = "Damaged";                               // Label replacer for when the vehicle is damaged.
        public string labelInoperable = "Inoperable";                         // Label replacer for when the vehicle is inoperable / "downed".
        public string labelBroken = "Broken";                                 // Label replacer for when the vehicle is broken / "dead".
        public List<VehicleRole> roles = new List<VehicleRole>();             // Defines roles of passengers.

        //---------- Additions made by Swenzi ------------

        public float ejectIfBelowNeedPercent = 0.2f; //Unloads passenger when one of their major needs drops below this percent
        public float worldSpeedFactor = 2.0f; // How much faster does this move on the world map than a standard pawn? Caravan speed is calculated from an average of the combined speeds (Vanilla formula)
		public float worldSpeedFactorNoFuel = 0.5f; //Modifies the change in world speed if this vehicle is present but does not have fuel
        public float restNeedRate = 2.0E-05f; //The rate at which the rest need changes while in the vehicle
        public float foodNeedRate = 2.0E-05f; //The rate at which the food need changes while in the vehicle
        public float joyNeedRate = 1.5E-05f; //The rate at which the joy need changes while in the vehicle
        public int momentumTimeSeconds = 2; //Counter for how long the vehicle can travel without a driver in seconds

        //Vehicle type (Can only be one, effects not implemented yet)
        //public bool isAir = false; //Is the vehicle an aircraft? i.e. airplane, helicopter
        //public bool isLand = false; //Is the vehicle a land based vehicle? i.e. car, tank
        //public bool isWater = false; //Is the vehicle a water based vehicle? i.e. boat

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
