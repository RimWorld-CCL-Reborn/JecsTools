﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI;
using RimWorld.Planet;
using Verse.AI.Group;
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
    public enum ManipulationState
    {
        frozen = 0,
        able
    }


    public class CompVehicle : ThingComp
    {
		public float fuelConsumptionRate = 80f; //Stores what the fuel usage rate is, i.e. how much fuel is lost
		public bool draftStatusChanged = false; //Boolean connected to comp to prevent excessive changing of the draftstatus when forming a caravan
		public int tickCount = 0; //Counter for how long the vehicle has traveled without a driver
		public bool warnedNoFuel = false; //Boolean connected to comp to prevent spamming of the Caravan No Fuel warning message
		public List<VehicleHandlerGroup> vehicleContents; //Stores the handlergroups of the vehicle and its pawns while the vehicle is in a caravan
		public List<VehicleHandlerGroup> handlers = new List<VehicleHandlerGroup>();
        public List<Bill_LoadVehicle> bills = new List<Bill_LoadVehicle>();
        public List<ThingCountClass> repairCostList = new List<ThingCountClass>();
        private Sustainer movingSustainer;

        #region SwenzisCode
        //------ Additions By Swenzi -------
        //Purpose: Control the boolean warnedOnNoFuel
        //Logic: Needed to prevent spamming of the warning message

        public bool WarnedOnNoFuel{
            get => this.warnedNoFuel;

            set => this.warnedNoFuel = value;
        }

		//Purpose: Store the pawns in the vehicle while it is in a caravan
		//Logic: Allows the vehicle to remember what pawns were inside it so they can be put back in later on map entry
		
        public List<VehicleHandlerGroup> PawnsInVehicle
		{
            get => this.vehicleContents;

            set => this.vehicleContents = value;
        }
        //------ Additions By Swenzi -------
#endregion SwenzisCode

        public bool CanManipulate => this.Props.manipulationHandling > HandlingType.HandlerRequired || ManipulationHandlerAvailable;
        public bool ManipulationHandlerAvailable
        {
            
            get
            {
                bool result = false;
                if (this.handlers != null && this.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        if (group.handlers != null && group.handlers.Count > 0)
                        {
                            if (group.role != null)
                            {
                                if ((group.role.handlingTypes & HandlingTypeFlags.Manipulation) != HandlingTypeFlags.None)
                                {
                                    result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }
        public bool CanMove => this.Props.movementHandling > HandlingType.HandlerRequired || MovementHandlerAvailable;
        public bool MovementHandlerAvailable
        {
            get
            {
                bool result = false;
                if (this.handlers != null && this.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        if (group.handlers != null && group.handlers.Count > 0)
                        {
                            if (group.role != null)
                            {
                                if ((group.role.handlingTypes & HandlingTypeFlags.Movement) != HandlingTypeFlags.None)
                                {
                                    result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }
        public bool CanFireWeapons => this.Props.weaponHandling > HandlingType.HandlerRequired || WeaponHandlerAvailable;
        public bool WeaponHandlerAvailable
        {
            get
            {
                bool result = false;
                if (this.handlers != null && this.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        if (group.role != null && group.handlers != null && group.handlers.Count > 0)
                        {
                            if ((group.role.handlingTypes & HandlingTypeFlags.Weapons) != HandlingTypeFlags.None)
                            {
                                result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                            }
                        }
                    }
                }
                return result;
            }
        }

        public Pawn Pawn => this.parent as Pawn;

        public MovingState movingStatus             = MovingState.able;
        public WeaponState weaponStatus             = WeaponState.able;
        public ManipulationState manipulationStatus = ManipulationState.able;

        public bool ResolvedITTab = false;
        public bool ResolvedPawns = false;
        public void ResolveITab()
        {
            if (!this.ResolvedITTab)
            {
                this.ResolvedITTab = true;
                //PostExposeData();
                //Make the ITab
                IEnumerable<InspectTabBase> tabs = this.Pawn.GetInspectTabs();
                if (tabs != null && tabs.Count<InspectTabBase>() > 0)
                {
                    if (tabs.FirstOrDefault((InspectTabBase x) => x is ITab_Passengers) == null)
                    {
                        try
                        {
                            this.Pawn.def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Passengers)));
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
                if (this.handlers != null && this.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        if (group.handlers != null && group.handlers.Count > 0) result.AddRange(group.handlers);
                    }
                }
                return result;
            }
        }
        
        private Pawn GeneratePawn(List<PawnGenOption> optionalDefs = null)
        {

            PawnKindDef newPawnKind = this.Pawn.Faction.RandomPawnKind();
            if (optionalDefs != null && optionalDefs.Count > 0)
            {
                newPawnKind = optionalDefs.RandomElementByWeight((PawnGenOption x) => x.selectionWeight).kind;
            }

            PawnGenerationRequest request = new PawnGenerationRequest(newPawnKind, this.Pawn.Faction, PawnGenerationContext.NonPlayer, this.Pawn.Map.Tile, false, false, false, false, true, true, 1f, false, true, true, false, false, null, null, null, null, null, null);
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
                    this.handlers.Add(new VehicleHandlerGroup(this.Pawn, role, new List<Pawn>()));
                }
            }
        }
        public void ResolveFactionPilots()
        {
            //this.Pawn.pather.
            //-------- Additions Made By Swenzi --------
            //Purpose: Remove Spawning of Premade Pawns in Vehicles
            //Logic: Players should not get free pawns for making vehicles
            //Improvements: None I can think of

            //Premade Pawns should only appear for non-player-faction vehicles
            if (!this.Pawn.Faction.IsPlayer)
            {
                if (!this.ResolvedPawns)
                {
                    this.ResolvedPawns = true;

                    if (this.handlers != null && this.handlers.Count > 0)
                    {
                        foreach (VehicleHandlerGroup group in this.handlers)
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
                                        group.handlers.TryAdd(newPawn);
                                        //group.handlers.Add(newPawn);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void ResolveEjection()
        {
            //----Additions Made By Swenzi-----
            //Purpose: Ejects pawns if the need of that pawn is below the threshold set by ejectIfBelowNeedPercent
            //Logic: Prevents pawns from starving to death or other need related issues i.e. moral from poor management of pawns in vehicles
            //Improvements: Have pawns remember the vehicle they left, so they return after needs are satisfied?

            //CompRefuelable refuelable = this.Pawn.GetComp<CompRefuelable>();
            //if (refuelable != null)
            //{
            if (this.parent is Pawn vehicle && vehicle.Spawned && this.handlers != null && this.handlers.Count > 0 && 
               !((this.Pawn.GetLord()?.LordJob?.ToString()) == "RimWorld.LordJob_FormAndSendCaravan") &&
                (!((this.Pawn.CurJob?.def) == JobDefOf.UnloadYourInventory) && 
                 !this.Pawn.Position.InNoBuildEdgeArea(this.Pawn.Map)) && 
                !this.Pawn.IsFighting()
               )
            {
                //Find and remove all spawned characters from the vehicle list.
                foreach (VehicleHandlerGroup group in this.handlers)
                {
                    Pawn toRemove = group?.handlers?.InnerListForReading?.FirstOrDefault(x => x.Spawned);
                    if (toRemove != null)
                    {
						//Log.Message("x");
						//Same deal as TryDrop, Remove wasn't working
						//group.handlers.InnerListForReading.Remove(toRemove);
						ThingOwner<Pawn> handler = new ThingOwner<Pawn>();
						for (int z = 0; z < group.handlers.Count; z++)
						{
							if (group.handlers[z].def != toRemove.def)
							{
								handler.TryAdd(toRemove, 1, true);
							}
						}
						group.handlers = handler;
                        //return;
                    }
                }

                //Find and eject all characters who need to have their needs met.
                foreach (VehicleHandlerGroup group in this.handlers)
                {
                    Pawn toEject = group?.handlers?.InnerListForReading?.FirstOrDefault(x => !x.Spawned && x?.needs?.AllNeeds?.FirstOrDefault(y => y.CurLevelPercentage < this.Props.ejectIfBelowNeedPercent) != null);
                    if (toEject != null)
                    {
                        Messages.Message("MessagePawnLeftVehicle".Translate(new object[] { toEject.Label, this.Pawn.Label, "low" }), this.Pawn, MessageSound.SeriousAlert);
                        //Eject(p, ref group.handlers);
                        Pawn b;
						//Log.Message("1");
						//group.handlers.TryDrop(toEject, this.Pawn.PositionHeld, this.Pawn.MapHeld, ThingPlaceMode.Near, out b);
						//TryDrop wasn't working 
                        Eject(toEject, ref group.handlers);
                        ThingOwner<Pawn> handler = new ThingOwner<Pawn>();
						for (int z = 0; z < group.handlers.Count; z++)
						{
							if (group.handlers[z].def != toEject.def)
							{
								handler.TryAdd(toEject, 1, true);
							}
						}
						group.handlers = handler;

                        //Log.Message("2");
                        //return;
                    }
                }
                //}

                //----Additions Made By Swenzi-----

                //Every 250 ticks
                if (this.Props.ejectIfBelowHealthPercent > 0.0f)
                {
                    if (Find.TickManager.TicksGame % 250 == 0)
                    {
                        if (this.Pawn.Dead || this.Pawn.Downed)
                        {
                            if (this.handlers != null && this.handlers.Count > 0)
                            {
                                foreach (VehicleHandlerGroup group in this.handlers)
                                {
                                    EjectAll(ref group.handlers);
                                }
                                this.weaponStatus = WeaponState.frozen;
                                this.movingStatus = MovingState.frozen;
                                if (this.Pawn.Downed && this.Pawn.Faction != Faction.OfPlayerSilentFail) this.Pawn.SetFaction(Faction.OfPlayerSilentFail);
                                return;
                            }
                        }

                        if (this.Pawn.health != null)
                        {
                            if (this.Pawn.health.summaryHealth != null)
                            {
                                float currentHealthPercentage = this.Pawn.health.summaryHealth.SummaryHealthPercent;
                                if (currentHealthPercentage < this.Props.ejectIfBelowHealthPercent)
                                {
                                    if (this.handlers != null && this.handlers.Count > 0)
                                    {
                                        foreach (VehicleHandlerGroup group in this.handlers)
                                        {
                                            EjectAll(ref group.handlers);
                                        }
                                        this.weaponStatus = WeaponState.frozen;
                                        this.movingStatus = MovingState.frozen;
                                        if (this.Pawn.Downed) this.Pawn.SetFaction(Faction.OfPlayerSilentFail);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public void ResolveStatus()
        {
            //-------- Additions Made By Swenzi --------
            //Purpose: Fixes bugs and adds better fuel consumption if the vehicle is refuelable
            //Logic: Better fuel consumption logic saves chemfuel, less bugs is good
            //Improvements: None I can think of.
            //Other Info: Changes marked with --- ADB Swenzi --- due to the dispersion of modifications in the method

            //Safety check in case the modder didn't assign a vehicle locomotion type. Defaults to Land Vehicle
            //if (!this.Props.isWater && !this.Props.isLand && !this.Props.isAir)
            //	this.Props.isLand = true;

            //If refuelable, then check for fuel.
            //if(this.Pawn.GetLord()?.CurLordToil != null){
            //    Log.Warning(this.Pawn.GetLord().CurLordToil.ToString());
            //}
            //if(this.Pawn.CurJob != null){
            //    Log.Warning(this.Pawn.CurJob.ToString());
            //}
            //if(this.Pawn.mindState?.duty != null){
            //    Log.Warning(this.Pawn.mindState.duty.def.defName);
            //}
            CompRefuelable compRefuelable = this.Pawn.GetComp<CompRefuelable>();
            if (compRefuelable != null)
            {
                //------ ADB Swenzi ------
                //If it isn't moving than it shouldn't use fuel
                if ((this.Pawn.pather != null && !this.Pawn.pather.Moving) || (this.Pawn.GetCaravan() != null && (this.Pawn.GetCaravan().CantMove)))
                    compRefuelable.Props.fuelConsumptionRate = 0f;
                else
                    //If it's moving than it should use fuel
                    compRefuelable.Props.fuelConsumptionRate = this.fuelConsumptionRate;
                //------ ADB Swenzi ------

                if (!compRefuelable.HasFuel)
                {
                    this.weaponStatus = WeaponState.frozen;
                    this.movingStatus = MovingState.frozen;
                    return;
                }
            }

            if (this.MovementHandlerAvailable && this.movingStatus == MovingState.frozen)
            {
                this.movingStatus = MovingState.able;
            }
            if (this.WeaponHandlerAvailable && this.weaponStatus == WeaponState.frozen)
            {
                this.weaponStatus = WeaponState.able;
            }

            if (!this.MovementHandlerAvailable && this.movingStatus == MovingState.able)
            {
                if (this.Props.movementHandling != HandlingType.NoHandlerRequired ) this.movingStatus = MovingState.frozen;
            }
            if (!this.WeaponHandlerAvailable && this.weaponStatus == WeaponState.able)
            {
                if (this.Props.weaponHandling != HandlingType.NoHandlerRequired) this.weaponStatus = WeaponState.frozen;
            }
            if (!this.ManipulationHandlerAvailable && this.manipulationStatus == ManipulationState.able)
            {
                if (this.Props.manipulationHandling != HandlingType.NoHandlerRequired) this.manipulationStatus = ManipulationState.frozen;
            }

            // ------ ADB Swenzi -------
            //If it can move and it's in a caravan wandering than it might be stuck 
            //aka the movement thing hasn't kicked in. Change draft status just to be safe.

            //Fixes bug where weapon tries to fire even after gunner is removed
            if (this.weaponStatus != WeaponState.able){
                if (this.Pawn.CurJob.def == JobDefOf.WaitCombat || this.Pawn.CurJob.def == JobDefOf.AttackStatic || this.Pawn.CurJob.def == JobDefOf.AttackMelee){
                    if (!this.Pawn.pather.Moving)
                    {
                        this.Pawn.jobs.EndCurrentJob(JobCondition.None, false);
                    }else{
                        this.Pawn.jobs.EndCurrentJob(JobCondition.None, true);
                    }

                }
                //Log.Error(this.Pawn.CurJob.ToString());
            }
            if (this.movingStatus == MovingState.able)
            {
                //Removed caravan member check as apparently pawns currently forming a caravan aren't part of one yet
                if (this.Pawn.CurJob != null && this.Pawn.CurJob.def == JobDefOf.GotoWander)
                {
                    if (!this.draftStatusChanged){
						this.Pawn.drafter.Drafted = !this.Pawn.Drafted;
						this.draftStatusChanged = true;
                    }
                }
                else
                {
                    //Safety to allow this for future caravans
                    this.draftStatusChanged = false;
                }
            }
            else{
                //Vehicles that can't move shouldn't have Lords, it causes problems cause they never complete their jobs and toils
				if (this.Pawn.GetLord() != null)
					this.Pawn.GetLord().lordManager.RemoveLord(this.Pawn.GetLord());
                
				if (this.Pawn.pather != null && this.Pawn.pather.Moving)
				{

                    if (this.tickCount > this.Props.momentumTimeSeconds * 60)
                    {
                        //No more fake momentum, vehicle should stop
                        //this.Pawn.jobs.
                        if(this.Pawn.pather.Moving){
                            this.Pawn.jobs.EndCurrentJob(JobCondition.None, false);
                        }
                        this.Pawn.pather.StopDead();
                        this.tickCount = 0;
                    }
                    else
				    {
                        //Be cool, keep the momentum going
                        this.tickCount++;
				    }
				}
            }
            if(this.movingStatus != MovingState.able && this.weaponStatus != WeaponState.able){
                this.Pawn.mindState.lastJobTag = JobTag.Idle;
            }
            // ------ ADB Swenzi -------
        }

		// ------ Additions made by Swenzi ------
		//Purpose: Reduce the needs of pawns while in the vehicle
		//Logic: Prevent cryosleep gimmicks by storing pawns in vehicles forever
        //To prevent unneccessary patching of game methods, needs are modified by this function
        //Improvements: Mood modifiers if they stay too long? Traits effecting mood? Driving Experience?
		public void ResolveNeeds(){
            //Player only, NPC factions is weird unless better logic is make them go back to vehicles after being ejected
            if (this.Pawn.Faction.IsPlayer)
            {
                if (this.handlers != null && this.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        if ((group?.handlers?.Count ?? 0) > 0)
                        {
                            for (int i = 0; i < group.handlers.Count; i++)
                            {
                                Pawn pawn = group.handlers[i];
                                List<Need> pawn_needs = pawn.needs.AllNeeds;
                                //These needs are major and should change
                                for (int j = 0; j < pawn_needs.Count; j++)
                                {
                                    if (pawn_needs[j].def == NeedDefOf.Rest)
                                        pawn_needs[j].CurLevel -= this.Props.restNeedRate;
                                    if (pawn_needs[j].def == NeedDefOf.Food)
                                        pawn_needs[j].CurLevel -= this.Props.foodNeedRate;
                                    if (pawn_needs[j].def == NeedDefOf.Joy)
                                        pawn_needs[j].CurLevel -= this.Props.joyNeedRate;
                                }
                            }
                        }

                    }
                }
            }


        }
		// ------ Additions made by Swenzi ------
		
        public void Eject(Pawn pawn, ref ThingOwner<Pawn> list)
        {
            if (!pawn.Spawned)
            {
                GenSpawn.Spawn(pawn, this.Pawn.PositionHeld.RandomAdjacentCell8Way(), this.Pawn.MapHeld);
            }
            list.Remove(pawn);
        }
        public void EjectAll(ref ThingOwner<Pawn> pawns)
        {
            List<Pawn> pawnsToEject = new List<Pawn>(pawns);
            if (pawnsToEject != null && pawnsToEject.Count > 0)
            {
                foreach (Pawn p in pawnsToEject)
                {
                    Eject(p, ref pawns);
                }
            }
        }
        public void GiveLoadJob(Thing thingToLoad, VehicleHandlerGroup group)
        {
            if (thingToLoad is Pawn pawn)
            {
                Job newJob = new Job(DefDatabase<JobDef>.GetNamed("CompVehicle_LoadPassenger"), this.Pawn);
                pawn.jobs.TryTakeOrderedJob(newJob);

                if (this.bills != null && this.bills.Count > 0)
                {
                    Bill_LoadVehicle bill = this.bills.FirstOrDefault((Bill_LoadVehicle x) => x.pawnToLoad == pawn);
                    if (bill != null)
                    {
                        bill.group = group;
                        return;
                    }
                }
                this.bills.Add(new Bill_LoadVehicle(pawn, this.Pawn, group));
            }
        }
        public void Notify_Loaded(Pawn pawnToLoad)
        {
            if (this.bills != null & this.bills.Count > 0)
            {
                Bill_LoadVehicle bill = this.bills.FirstOrDefault((x) => x.pawnToLoad == pawnToLoad);
                if (bill != null)
                {
                    var curFaction = pawnToLoad.Faction;
                    pawnToLoad.DeSpawn();
                    if (pawnToLoad.holdingOwner != null) pawnToLoad.holdingOwner = null;
                    bill.group.handlers.TryAdd(pawnToLoad);
                    Find.WorldPawns.PassToWorld(pawnToLoad, PawnDiscardDecideMode.KeepForever);
                    pawnToLoad.SetFaction(curFaction);
                    this.bills.Remove(bill);
                }
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
            ResolveMovementSound();

            //------ Additions made by Swenzi ------
            ResolveNeeds();
            //------ Additions made by Swenzi ------
        }

        public void ResolveMovementSound()
        {
            if (this.parent is Pawn p && Props.soundMoving != null)
            {
                var isMovingNow = p?.pather?.Moving ?? false;
                if (isMovingNow && movingSustainer == null)
                {
                    SoundInfo info = SoundInfo.InMap(this.parent, MaintenanceType.None);
                    this.movingSustainer = Props.soundMoving.TrySpawnSustainer(info);
                    return;
                }
                else if (!isMovingNow && this.movingSustainer != null)
                {
                    this.movingSustainer.End();
                    this.movingSustainer = null;
                }
            }
        }

        public void GetVehicleButtonFloatMenu(VehicleHandlerGroup group, bool canLoad)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Map map = this.Pawn.Map;
            List<Pawn> tempList = (group?.handlers?.InnerListForReading != null) ? new List<Pawn>(group.handlers.InnerListForReading) : new List<Pawn>();
            if (canLoad)
            {
                string text = "CompVehicle_Load".Translate(group.role.label);
                //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, handler);
                list.Add(new FloatMenuOption(text, delegate
                {
                    SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                    Find.Targeter.BeginTargeting(new TargetingParameters() { validator = ti => ti.Thing is Pawn p && (p.RaceProps.Humanlike && p.IsColonistPlayerControlled ) }, delegate (LocalTargetInfo target)
                    {
                        GiveLoadJob(target.Thing, group);
                    }, null, null, null);
                }, MenuOptionPriority.Default, null, null, 29f, null, null));
            }
            if (tempList.Count != 0)
            {
                foreach (Pawn handler in tempList)
                {
                    string text = "CompVehicle_Unload".Translate(handler.Name.ToStringFull);
                    List<FloatMenuOption> arg_121_0 = list;
                    Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, handler);
                    list.Add(new FloatMenuOption(text, delegate
                    {
                        Eject(handler, ref group.handlers);
                    }, MenuOptionPriority.Default, null, null, 29f, extraPartOnGUI, null));
                }
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public CompProperties_Vehicle Props => (CompProperties_Vehicle)this.props;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            IEnumerator<Gizmo> enumerator = base.CompGetGizmosExtra().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            //Log.Message("0");
            if (this.Pawn.Faction == Faction.OfPlayerSilentFail)
            {
                //Log.Message("1");
                if (this.Props.roles != null && this.Props.roles.Count > 0)
                {
                    //Log.Message("2");
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        //Log.Message("3");
                        if (group.role != null)
                        {
                            //Log.Message("4");
                            bool loadable = group.AreSlotsAvailable;
                            bool unloadable = (group?.handlers?.Count ?? 0) > 0;
                            if (loadable || unloadable)
                            {
                                //Log.Message("5");
                                Command_VehicleHandler button = new Command_VehicleHandler()
                                {
                                    action = delegate
                                    {
                                        SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                                        GetVehicleButtonFloatMenu(group, loadable);
                                    },
                                    hotKey = KeyBindingDefOf.Misc1
                                };
                                string label = "CompVehicle_Load";
                                string desc = "CompVehicle_LoadDesc";
                                if (!loadable && unloadable)
                                {
                                    label = "CompVehicle_Unload";
                                    desc = "CompVehicle_UnloadDesc";
                                }
                                if (loadable && unloadable)
                                {
                                    label = "CompVehicle_LoadUnload";
                                    desc = "CompVehicle_LoadUnloadDesc";
                                }

                                button.defaultLabel = label.Translate(group.role.label.CapitalizeFirst());
                                button.defaultDesc = desc.Translate(group.role.label.CapitalizeFirst());
                                button.icon = TexCommand.Install;  //ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", true);

                                float pctFilled = this.Pawn.health.summaryHealth.SummaryHealthPercent;
                                button.disabled = this.Pawn.Downed || this.Pawn.Dead || (pctFilled < this.Props.ejectIfBelowHealthPercent);
                                button.disabledReason = "CompVehicle_DisabledDesc".Translate();
                                
                                //Log.Message(button.ToString());
                                yield return button;
                            }
                        }
                    }
                }
                if (this.Pawn.drafter == null) this.Pawn.drafter = new Pawn_DraftController(this.Pawn);
                

            }
        }
        public override void PostExposeData()
        {
            
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.ResolvedPawns, "ResolvedPawns", false);
            Scribe_Values.Look<WeaponState>(ref this.weaponStatus, "weaponStatus", WeaponState.able);
            Scribe_Values.Look<MovingState>(ref this.movingStatus, "movingStatus", MovingState.able);

            Scribe_Collections.Look<VehicleHandlerGroup>(ref this.handlers, "handlers", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<Bill_LoadVehicle>(ref this.bills, "bills", LookMode.Deep, new object[0]);

            //Scribe_Collections.Look<Pawn>(ref this.pilots, "pilots", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.gunners, "gunners", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.crew, "crew", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.passengers, "passengers", LookMode.Deep, new object[0]);
        }
    }
}
