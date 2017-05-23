using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace CompPilotable
{
    public enum PilotableSlotType
    {
        pilot,
        gunner,
        crew,
        dutiless
    }

    public enum MovingState
    {
        frozen = 0,
        able
    }

    public enum WeaponState
    {
        frozen = 0,
        able
    }

    internal class CompVehicle : ThingComp
    {
        public List<Pawn> pilots = new List<Pawn>();
        public List<Pawn> gunners = new List<Pawn>();
        public List<Pawn> crew = new List<Pawn>();
        public List<Pawn> passengers = new List<Pawn>();
      
        public List<Pawn> AllPassengers
        {
            get
            {
                List<Pawn> pawns = new List<Pawn>();
                if (pilots != null && pilots.Count > 0) pawns.AddRange(pilots);
                if (gunners != null && gunners.Count > 0) pawns.AddRange(gunners);
                if (crew != null && crew.Count > 0) pawns.AddRange(crew);
                if (passengers != null && passengers.Count > 0) pawns.AddRange(passengers);
                return pawns;
            }
        }

        public List<Pawn> GetPassengersOfType(PilotableSlotType slotType)
        {
            switch (slotType)
            {
                case PilotableSlotType.crew:     return crew;
                case PilotableSlotType.gunner:   return gunners;
                case PilotableSlotType.dutiless: return passengers;
                case PilotableSlotType.pilot:    return pilots;
            }
            return null;
        }

        public int GetTotalSeats(PilotableSlotType slotType)
        {
            switch (slotType)
            {
                case PilotableSlotType.crew:     return Props.crewSeats;
                case PilotableSlotType.dutiless: return Props.passengerSeats;
                case PilotableSlotType.gunner:   return Props.gunnerSeats;
                case PilotableSlotType.pilot:    return Props.pilotSeats;
            }
            return 0;
        }

        public bool IsInList(Pawn pawn, List<Pawn> listPawn)
        {
            bool result = false;
            if (listPawn != null && listPawn.Count > 0)
            {
                if (listPawn.Contains(pawn)) result = true;
            }
            return result;
        }

        public bool IsPilot(Pawn pawn) { return IsInList(pawn, pilots); }
        public bool IsGunner(Pawn pawn) { return IsInList(pawn, gunners); }
        public bool IsCrew(Pawn pawn) { return IsInList(pawn, crew); }
        public bool IsPassenger(Pawn pawn) { return IsInList(pawn, passengers); }

        public bool PilotAvailable
        {
            get
            {
                bool result = false;
                if (pilots != null && pilots.Count > 0)
                {
                    Pawn availablePilot = pilots.FirstOrDefault((Pawn x) => !x.Dead && !x.Downed);
                    if (availablePilot != null) return true;
                }
                return result;
            }
        }
        public bool GunnerAvailable
        {
            get
            {
                bool result = false;
                if (gunners != null && gunners.Count > 0)
                {
                    Pawn availableGunner = gunners.FirstOrDefault((Pawn x) => !x.Dead && !x.Downed);
                    if (availableGunner != null) return true;
                }
                return result;
            }
        }
        public bool CrewAvailable
        {
            get
            {
                bool result = false;
                if (crew != null && crew.Count > 0)
                {
                    Pawn availableCrew = crew.FirstOrDefault((Pawn x) => !x.Dead && !x.Downed);
                    if (availableCrew != null) return true;
                }
                return result;
            }
        }
        public bool PassengerAvailable
        {
            get
            {
                bool result = false;
                if (passengers != null && passengers.Count > 0)
                {
                    Pawn availablePassenger = passengers.FirstOrDefault((Pawn x) => !x.Dead && !x.Downed);
                    if (availablePassenger != null) return true;
                }
                return result;
            }
        }

        public string GetVehicleRole(Pawn pawn)
        {
            if (IsPilot(pawn)) return StringOf.pilot;
            if (IsGunner(pawn)) return StringOf.gunner;
            if (IsCrew(pawn)) return StringOf.crew;
            return StringOf.passenger;
        }

        public Pawn Pawn
        {
            get
            {
                return this.parent as Pawn;
            }
        }

        public MovingState movingStatus = MovingState.able;
        public WeaponState weaponStatus = WeaponState.able;

        public bool ResolvedITTab = false;

        public bool ResolvedPawns = false;

        public void ResolveITab()
        {
            if (!ResolvedITTab)
            {
                ResolvedITTab = true;
                //PostExposeData();
                //Make the ITab
                IEnumerable<InspectTabBase> tabs = Pawn.GetInspectTabs();
                if (tabs != null && tabs.Count<InspectTabBase>() > 0)
                {
                    if (tabs.FirstOrDefault((InspectTabBase x) => x is ITab_Passengers) == null)
                    {
                        try
                        {
                            Pawn.def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Passengers)));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat(new object[]
                            {
                    "Could not instantiate inspector tab of type ",
                    typeof(ITab_Passengers),
                    ": ",
                    ex
                            }));
                        }
                    }
                }
            }
        }

        // RimWorld.IncidentWorker_CaravanMeeting
        private Pawn GeneratePawn(PawnKindDef optionalDef = null)
        {
            if (optionalDef == null) optionalDef = Pawn.Faction.RandomPawnKind();
            PawnGenerationRequest request = new PawnGenerationRequest(optionalDef, Pawn.Faction, PawnGenerationContext.NonPlayer, Pawn.Map.Tile, false, false, false, false, true, true, 1f, false, true, true, false, false, null, null, null, null, null, null);
            Pawn item = PawnGenerator.GeneratePawn(request);
            return item;
        }

        public void ResolveFactionPilots()
        {
            if (!ResolvedPawns)
            {
                ResolvedPawns = true;

                if (Props.requiredPilots > 0)
                {
                    int minimum = Math.Min(Props.requiredPilots, Props.pilotSeats);
                    int possible = Math.Max(Props.requiredPilots, Props.pilotSeats);
                    int range = Rand.Range(minimum, possible);
                    for (int i = 0; i < range; i++)
                    {
                        Pawn newPawn = GeneratePawn(Props.preferredPilotKind);
                        if (newPawn != null) pilots.Add(newPawn);
                    }

                }
                if (Props.requiredGunners > 0)
                {
                    int minimum = Math.Min(Props.requiredGunners, Props.gunnerSeats);
                    int possible = Math.Max(Props.requiredGunners, Props.gunnerSeats);
                    int range = Rand.Range(minimum, possible);
                    for (int i = 0; i < range; i++)
                    {
                        Pawn newPawn = GeneratePawn(Props.preferredGunnerKind);
                        if (newPawn != null) gunners.Add(newPawn);
                    }
                }
                if (Props.requiredCrew > 0)
                {
                    int minimum = Math.Min(Props.requiredCrew, Props.crewSeats);
                    int possible = Math.Max(Props.requiredCrew, Props.crewSeats);
                    int range = Rand.Range(minimum, possible);
                    for (int i = 0; i < range; i++)
                    {
                        Pawn newPawn = GeneratePawn(Props.preferredCrewKind);
                        if (newPawn != null) crew.Add(newPawn);
                    }
                }

            }
        }

        public void ResolveEjection()
        {
            //Every 250 ticks
            if (Props.ejectIfBelowHealthPercent > 0.0f)
            {
                if (Find.TickManager.TicksGame % 250 == 0)
                {
                    if (Pawn.Dead || Pawn.Downed)
                    {
                        if (AllPassengers != null && AllPassengers.Count > 0)
                        {
                            EjectAll(pilots);
                            EjectAll(gunners);
                            EjectAll(crew);
                            EjectAll(passengers);
                            weaponStatus = WeaponState.frozen;
                            movingStatus = MovingState.frozen;
                            if (Pawn.Downed) Pawn.SetFaction(Faction.OfPlayerSilentFail);
                            return;
                        }
                    }

                    if (Pawn.health != null)
                    {
                        if (Pawn.health.summaryHealth != null)
                        {
                            float currentHealthPercentage = Pawn.health.summaryHealth.SummaryHealthPercent;
                            if (currentHealthPercentage < Props.ejectIfBelowHealthPercent)
                            {
                                if (AllPassengers != null && AllPassengers.Count > 0)
                                {
                                    EjectAll(pilots);
                                    EjectAll(gunners);
                                    EjectAll(crew);
                                    EjectAll(passengers);
                                    weaponStatus = WeaponState.frozen;
                                    movingStatus = MovingState.frozen;
                                    Pawn.SetFaction(Faction.OfPlayerSilentFail);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void EjectAll(List<Pawn> pawns)
        {
            List<Pawn> pawnsToEject = new List<Pawn>(pawns);
            if (pawnsToEject != null && pawnsToEject.Count > 0)
            {
                foreach (Pawn p in pawnsToEject)
                {
                    GenSpawn.Spawn(p, Pawn.PositionHeld.RandomAdjacentCell8Way(), Pawn.MapHeld);
                    pawns.Remove(p);
                }
            }
        }

        public void ResolveStatus()
        {
            if (PilotAvailable && movingStatus == MovingState.frozen)
            {
                movingStatus = MovingState.able;
            }
            if (GunnerAvailable && weaponStatus == WeaponState.frozen)
            {
                weaponStatus = WeaponState.able;
            }

            if (!PilotAvailable && movingStatus == MovingState.able)
            {
                if (!Props.canMoveWithoutPilot) movingStatus = MovingState.frozen;
            }
            if (!GunnerAvailable && weaponStatus == WeaponState.able)
            {
                if (!Props.canFireWithoutGunner) weaponStatus = WeaponState.frozen;
            }

        }

        #region PartRecords
        public List<BodyPartRecord> pilotParts = null;
        public List<BodyPartRecord> PilotParts
        {
            get
            {
                if (pilotParts == null)
                {
                    pilotParts = new List<BodyPartRecord>(Pawn.health.hediffSet.GetNotMissingParts(
                        BodyPartHeight.Undefined,
                        BodyPartDepth.Undefined
                        ).ToList<BodyPartRecord>().FindAll(((BodyPartRecord x) => x.def.tags.Contains("PilotSeat"))));
                }
                return pilotParts;
            }
        }
        public List<BodyPartRecord> gunnerParts = null;
        public List<BodyPartRecord> GunnerParts
        {
            get
            {
                if (gunnerParts == null)
                {
                    gunnerParts = new List<BodyPartRecord>(Pawn.health.hediffSet.GetNotMissingParts(
                        BodyPartHeight.Undefined,
                        BodyPartDepth.Undefined
                        ).ToList<BodyPartRecord>().FindAll(((BodyPartRecord x) => x.def.tags.Contains("GunnerSeat"))));
                }
                return gunnerParts;
            }
        }
        public List<BodyPartRecord> crewParts = null;
        public List<BodyPartRecord> CrewParts
        {
            get
            {
                if (crewParts == null)
                {
                    crewParts = new List<BodyPartRecord>(Pawn.health.hediffSet.GetNotMissingParts(
                        BodyPartHeight.Undefined,
                        BodyPartDepth.Undefined
                        ).ToList<BodyPartRecord>().FindAll(((BodyPartRecord x) => x.def.tags.Contains("CrewSeat"))));
                }
                return crewParts;
            }
        }
        public List<BodyPartRecord> passengerParts = null;
        public List<BodyPartRecord> PassengerParts
        {
            get
            {
                if (passengerParts == null)
                {
                    passengerParts = new List<BodyPartRecord>(Pawn.health.hediffSet.GetNotMissingParts(
                        BodyPartHeight.Undefined,
                        BodyPartDepth.Undefined
                        ).ToList<BodyPartRecord>().FindAll(((BodyPartRecord x) => x.def.tags.Contains("PassengerSeat"))));
                }
                return passengerParts;
            }
        }
        #endregion PartRecords
        
        public override void CompTick()
        {
            base.CompTick();
            ResolveITab();
            ResolveFactionPilots();
            ResolveEjection();
            ResolveStatus();
        }


        public CompProperties_Vehicle Props
        {
            get
            {
                return (CompProperties_Vehicle)props;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.ResolvedPawns, "ResolvedPawns", false);
            Scribe_Values.Look<WeaponState>(ref this.weaponStatus, "weaponStatus", WeaponState.able);
            Scribe_Values.Look<MovingState>(ref this.movingStatus, "movingStatus", MovingState.able);
            Scribe_Collections.Look<Pawn>(ref this.pilots, "pilots", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Pawn>(ref this.gunners, "gunners", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Pawn>(ref this.crew, "crew", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Pawn>(ref this.passengers, "passengers", LookMode.Deep, new object[0]);
        }
    }
}
