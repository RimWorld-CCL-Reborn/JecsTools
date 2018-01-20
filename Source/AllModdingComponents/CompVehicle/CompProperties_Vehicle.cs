using System.Collections.Generic;
using UnityEngine;
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

    public enum HandlingType
    {
        Incapable = 0,
        HandlerRequired = 1,
        NoHandlerRequired = 2
    }

    public class CompProperties_Vehicle : CompProperties
    {
        //Added by Swenzi 1/1/2018
        //Allow animals to pull vehicles and be in a role that controls Movement Handling
        public bool animalDrivers = false;

        //Modifies the draw offset of the animal when it's attached to the vehicle
        public Vector3 drawOffset = new Vector3(0, 0, 0);

        //Whether or not the drivers of a pawn should be drawn, i.e. animals pulling a wagon or a pawn on a motorcycle
        public bool drawDrivers = false;

        //The minimum body size requirement for an animal to haul a vehicle
        public float minBodySize = 0;


        public bool canBeDowned = false; // Does this become downed?

        public bool canWiggleWhenDowned = false; // Does this wiggle when downed?

        //Added 7/23/17
        public float cargoCapacity = 385.554f; // Cargo capacity for your vehicle in kilograms.

        public float ejectIfBelowHealthPercent = 0.0f
            ; // Unloads all passengers when health percentage drops below this point

        //---------- Additions made by Swenzi ------------

        public float ejectIfBelowNeedPercent = 0.2f
            ; //Unloads passenger when one of their major needs drops below this percent

        public float foodNeedRate = 2.0E-05f; //The rate at which the food need changes while in the vehicle
        public float joyNeedRate = 1.5E-05f; //The rate at which the joy need changes while in the vehicle
        public string labelBroken = "Broken"; // Label replacer for when the vehicle is broken / "dead".
        public string labelDamaged = "Damaged"; // Label replacer for when the vehicle is damaged.
        public string labelInoperable = "Inoperable"; // Label replacer for when the vehicle is inoperable / "downed".
        public string labelUndamaged = "Undamaged"; // Label replacer for when the vehicle is undamaged.

        public HandlingType manipulationHandling = HandlingType.Incapable
            ; // Does this vehicle rely on others to load it? Or can it "pick" up objects?

        public int momentumTimeSeconds = 2; //Counter for how long the vehicle can travel without a driver in seconds
        public HandlingType movementHandling = HandlingType.HandlerRequired; // Is the movement automatic?
        public float restNeedRate = 2.0E-05f; //The rate at which the rest need changes while in the vehicle
        public List<VehicleRole> roles = new List<VehicleRole>(); // Defines roles of passengers.
        public float seatHitCriticalHitChance = 0.05f; // Chance of doubling the damage.
        public float seatHitDamageFactor = 2.0f; // Multiply & apply this damage to passengers in seats
        public SoundDef soundEject = null; // Sound when a character exits the vehicle.
        public SoundDef soundEntry = null; // Sound for when a character enters the vehicle.
        public SoundDef soundMoving = null; // For a sound to play while the vehicle is moving.

        public VehicleType vehicleType = VehicleType.LandWheeled; // Defaults to wheeled land vehicles.
        public HandlingType weaponHandling = HandlingType.HandlerRequired; // Are the weapons automatic?

        public float worldSpeedFactor = 2.0f
            ; // How much faster does this move on the world map than a standard pawn? Caravan speed is calculated from an average of the combined speeds (Vanilla formula)

        public float worldSpeedFactorNoFuel = 0.5f
            ; //Modifies the change in world speed if this vehicle is present but does not have fuel

        public CompProperties_Vehicle()
        {
            compClass = typeof(CompVehicle);
        }

        //Vehicle type (Can only be one, effects not implemented yet)
        //public bool isAir = false; //Is the vehicle an aircraft? i.e. airplane, helicopter
        //public bool isLand = false; //Is the vehicle a land based vehicle? i.e. car, tank
        //public bool isWater = false; //Is the vehicle a water based vehicle? i.e. boat

        //---------- Additions made by Swenzi end -------------

        public int TotalCapacity
        {
            get
            {
                var result = 0;
                if (roles != null && roles.Count > 0)
                    foreach (var role in roles)
                        result += role.slots;
                return result;
            }
        }
    }
}