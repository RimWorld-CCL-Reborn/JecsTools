using System;
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
                                        group.handlers.Add(newPawn);
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

			CompRefuelable refuelable = this.Pawn.GetComp<CompRefuelable>();
            if (refuelable != null)
            {
                if (this.handlers != null && this.handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in this.handlers)
                    {
                        for (int i = 0; i < group.handlers.Count; i++)
                        {
                            Pawn pawn = group.handlers[i];
                            List<Need> pawnNeeds = pawn.needs.AllNeeds;
                            for (int j = 0; j < pawnNeeds.Count; j++)
                            {
                                if (pawnNeeds[j].CurLevelPercentage < this.Props.ejectIfBelowNeedPercent)
                                {
									//Prevents annoying issues where the pawn leaves the vehicle due to needs when forming a caravan
									//since they can wait till world map, world needs are handled seperately by the game
									if (!this.Pawn.IsCaravanMember() && !((this.Pawn.GetLord()?.LordJob?.ToString()) == "RimWorld.LordJob_FormAndSendCaravan"))
                                    {
                                        //Prevents annoying issues where the pawn leaves the vehicle despite the player wanting
                                        //the pawn to enter the vehicle. I.e. Life and death situation? Ignore Needs. Live!!!!
                                        if (!this.Pawn.IsFighting() && !pawn.IsFighting())
                                        {
                                            //Notify the player that "Johnny" has left the vehicle so the pawn can be punished as appropriate
                                            Messages.Message("MessagePawnLeftVehicle".Translate(new object[] { pawn.Label, this.Pawn.Label, pawnNeeds[j].def.defName }), this.Pawn, MessageSound.SeriousAlert);
                                            Eject(pawn, group.handlers);
                                            group.handlers.Remove(pawn);
                                            break;
                                        }
                                    }
                                }
                            }


                        }
                    }
                }
            }

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
                                EjectAll(group.handlers);
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
                                        EjectAll(group.handlers);
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

            if (this.movingStatus == MovingState.able)
            {
                if (this.Pawn.CurJob != null && this.Pawn.IsCaravanMember() && this.Pawn.CurJob.def.defName == "GotoWander")
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
                        this.Pawn.jobs.StopAll();
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
		// ------ Additions made by Swenzi ------
		
        public void Eject(Pawn pawn, List<Pawn> list)
        {
            GenSpawn.Spawn(pawn, this.Pawn.PositionHeld.RandomAdjacentCell8Way(), this.Pawn.MapHeld);
            list.Remove(pawn);
        }
        public void EjectAll(List<Pawn> pawns)
        {
            List<Pawn> pawnsToEject = new List<Pawn>(pawns);
            if (pawnsToEject != null && pawnsToEject.Count > 0)
            {
                foreach (Pawn p in pawnsToEject)
                {
                    Eject(p, pawns);
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
                    pawnToLoad.DeSpawn();
                    bill.group.handlers.Add(pawnToLoad);
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

            //------ Additions made by Swenzi ------
            ResolveNeeds();
            //------ Additions made by Swenzi ------

        }
        public void GetVehicleButtonFloatMenu(VehicleHandlerGroup group, bool canLoad)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Map map = this.Pawn.Map;
            List<Pawn> tempList = new List<Pawn>(group.handlers);
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
                        Eject(handler, group.handlers);
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
                        if (group.role != null && group.handlers != null)
                        {
                            //Log.Message("4");
                            bool loadable = group.AreSlotsAvailable;
                            bool unloadable = group.handlers.Count > 0;
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
