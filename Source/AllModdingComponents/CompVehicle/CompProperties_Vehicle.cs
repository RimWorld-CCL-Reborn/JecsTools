using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CompPilotable
{
    internal class CompProperties_Vehicle : CompProperties
    {
        public SoundDef entrySound = null;
        public SoundDef ejectSound = null;
        public int pilotSeats = 1;                          // Number of possible pilots
        public int gunnerSeats = 0;                         // Number of possible gunners
        public int crewSeats = 0;                           // Number of possible crew
        public int passengerSeats = 0;                      // Number of possible passengers
        public int requiredPilots = 1;                      // How many pilots are required for movement
        public int requiredGunners = 0;                     // How many gunners are required for weapons to function
        public int requiredCrew = 0;                        // How many crew are required to be pilotable
        public float ejectIfBelowHealthPercent = 0.0f;      // Unloads all passengers when health percentage drops below this point
        public float seatHitDamageFactor = 2.0f;            // Multiply & apply this damage to passengers in seats
        public float seatHitCriticalHitChance = 0.05f;      // Chance of doubling the damage.
        public bool canBeDowned = false;                    // Does this become downed?
        public bool wigglesWhenDowned = false;                // Does this wiggle when downed?
        public bool canMoveWithoutPilot = false;            // Is the movement automatic?
        public bool canFireWithoutGunner = false;           // Are the weapons automatic?
        public PawnKindDef preferredPilotKind = null;       
        public PawnKindDef preferredGunnerKind = null;      // These types are preferred when spawning AI loaded pilotables
        public PawnKindDef preferredCrewKind = null;

        public int TotalCapacity
        {
            get
            {
                return pilotSeats + gunnerSeats + crewSeats + passengerSeats;
            }
        }

        public CompProperties_Vehicle()
        {
            this.compClass = typeof(CompVehicle);
        }
    }
}
