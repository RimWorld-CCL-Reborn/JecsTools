using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace CompVehicle
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

    public class CompVehicle : ThingComp
    {
        public List<VehicleHandlerGroup> handlers = new List<VehicleHandlerGroup>();

        /*
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
        */

        public bool MovementHandlerAvailable
        {
            get
            {
                bool result = false;
                if (handlers != null && handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in handlers)
                    {
                        if (group.role != null && group.handlers != null && group.handlers.Count > 0)
                        {
                            if (group.role.handlesMovement)
                            {
                                result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                            }
                        }
                    }
                }
                return result;
            }
        }
        public bool WeaponHandlerAvailable
        {
            get
            {
                bool result = false;
                if (handlers != null && handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in handlers)
                    {
                        if (group.role != null && group.handlers != null && group.handlers.Count > 0)
                        {
                            if (group.role.handlesWeapons)
                            {
                                result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                            }
                        }
                    }
                }
                return result;
            }
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

        public List<Pawn> AllOccupants
        {
            get
            {
                List<Pawn> result = new List<Pawn>();
                if (handlers != null && handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in handlers)
                    {
                        if (group.handlers != null && group.handlers.Count > 0) result.AddRange(group.handlers);
                    }
                }
                return result;
            }
        }

        //public List<Pawn> OnPostDamageInjuredPawns()
        //{
        //    List<BodyPartRecord> result = null; ;
        //    if ( != null && pilotParts.Count > 0)
        //    {
        //        if (pilotParts.Contains(injury.Part))
        //        {
        //            if (compPilotable.pilots != null && compPilotable.pilots.Count > 0)
        //            {
        //                affectedPawns.AddRange(compPilotable.pilots);
        //            }
        //        }
        //    }
        //}

        // RimWorld.IncidentWorker_CaravanMeeting

        private Pawn GeneratePawn(List<PawnGenOption> optionalDefs = null)
        {

            PawnKindDef newPawnKind = Pawn.Faction.RandomPawnKind();
            if (optionalDefs != null && optionalDefs.Count > 0)
            {
                newPawnKind = optionalDefs.RandomElementByWeight((PawnGenOption x) => x.selectionWeight).kind;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(newPawnKind, Pawn.Faction, PawnGenerationContext.NonPlayer, Pawn.Map.Tile, false, false, false, false, true, true, 1f, false, true, true, false, false, null, null, null, null, null, null);
            Pawn item = PawnGenerator.GeneratePawn(request);
            return item;
        }

        public void InitializeVehicleHandlers()
        {
            if (this.handlers != null && this.handlers.Count > 0) return;
            
            if (this.Props.roles != null && this.Props.roles.Count > 0)
            {
                foreach (VehicleRole role in this.Props.roles)
                {
                    this.handlers.Add(new VehicleHandlerGroup(Pawn, role, new List<Pawn>()));
                }
            }
        }

        public void ResolveFactionPilots()
        {
            if (!ResolvedPawns)
            {
                ResolvedPawns = true;

                if (handlers != null && handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in handlers)
                    {
                        VehicleRole role = group.role;
                        if (role.slotsToOperate > 0)
                        {
                            int minimum = Math.Min(role.slotsToOperate, role.slots);
                            int maximum = Math.Max(role.slotsToOperate, role.slots);
                            int range = Rand.Range(minimum, maximum);
                            for (int i = 0; i < range; i++)
                            {
                                Pawn newPawn = GeneratePawn(role.preferredHandlers);
                                if (newPawn != null)
                                {
                                    group.handlers.Add(newPawn);
                                }
                            }
                        }
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
                        if (handlers != null && handlers.Count > 0)
                        {
                            foreach (VehicleHandlerGroup group in handlers)
                            {
                                EjectAll(group.handlers);
                            }
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
                                if (handlers != null && handlers.Count > 0)
                                {
                                    foreach (VehicleHandlerGroup group in handlers)
                                    {
                                        EjectAll(group.handlers);
                                    }
                                    weaponStatus = WeaponState.frozen;
                                    movingStatus = MovingState.frozen;
                                    if (Pawn.Downed) Pawn.SetFaction(Faction.OfPlayerSilentFail);
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
            if (MovementHandlerAvailable && movingStatus == MovingState.frozen)
            {
                movingStatus = MovingState.able;
            }
            if (WeaponHandlerAvailable && weaponStatus == WeaponState.frozen)
            {
                weaponStatus = WeaponState.able;
            }

            if (!MovementHandlerAvailable && movingStatus == MovingState.able)
            {
                if (!Props.canMoveWithoutHandler) movingStatus = MovingState.frozen;
            }
            if (!WeaponHandlerAvailable && weaponStatus == WeaponState.able)
            {
                if (!Props.canFireWithoutHandler) weaponStatus = WeaponState.frozen;
            }

        }
       
        public override void CompTick()
        {
            base.CompTick();
            InitializeVehicleHandlers();
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
            Scribe_Collections.Look<VehicleHandlerGroup>(ref this.handlers, "handlers", LookMode.Deep, new object[0]);

            //Scribe_Collections.Look<Pawn>(ref this.pilots, "pilots", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.gunners, "gunners", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.crew, "crew", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.passengers, "passengers", LookMode.Deep, new object[0]);
        }
    }
}
