using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

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
        public List<Bill_LoadVehicle> bills = new List<Bill_LoadVehicle>();

        public bool draftStatusChanged = false
            ; //Boolean connected to comp to prevent excessive changing of the draftstatus when forming a caravan

        public float fuelConsumptionRate = 80f; //Stores what the fuel usage rate is, i.e. how much fuel is lost
        public List<VehicleHandlerGroup> handlers = new List<VehicleHandlerGroup>();
        public Rot4 lastDirection = Rot4.South; //J Stores the last direction a vehicle was facing. 
        public ManipulationState manipulationStatus = ManipulationState.able;

        public MovingState movingStatus = MovingState.able;
        private Sustainer movingSustainer;
        public List<ThingCountClass> repairCostList = new List<ThingCountClass>();

        public bool ResolvedITTab;
        public bool ResolvedPawns;
        public int tickCount; //Counter for how long the vehicle has traveled without a driver

        public List<VehicleHandlerTemp> vehicleContents = new List<VehicleHandlerTemp>()
            ; //Stores the handlergroups of the vehicle and its pawns while the vehicle is in a caravan

        public bool warnedNoFuel; //Boolean connected to comp to prevent spamming of the Caravan No Fuel warning message
        public WeaponState weaponStatus = WeaponState.able;

        public bool CanManipulate =>
            Props.manipulationHandling > HandlingType.HandlerRequired || ManipulationHandlerAvailable;

        public bool ManipulationHandlerAvailable
        {
            get
            {
                var result = false;
                if (handlers != null && handlers.Count > 0)
                    foreach (var group in handlers)
                        if (group.handlers != null && group.handlers.Count > 0)
                            if (group.role != null)
                                if ((group.role.handlingTypes & HandlingTypeFlags.Manipulation) !=
                                    HandlingTypeFlags.None)
                                    result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                return result;
            }
        }

        public bool CanMove => Props.movementHandling > HandlingType.HandlerRequired || MovementHandlerAvailable;

        public bool MovementHandlerAvailable
        {
            get
            {
                var result = false;
                if (handlers != null && handlers.Count > 0)
                    foreach (var group in handlers)
                        if (group.handlers != null && group.handlers.Count > 0)
                            if (group.role != null)
                                if ((group.role.handlingTypes & HandlingTypeFlags.Movement) != HandlingTypeFlags.None)
                                    result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                return result;
            }
        }

        public bool CanFireWeapons => Props.weaponHandling > HandlingType.HandlerRequired || WeaponHandlerAvailable;

        public bool WeaponHandlerAvailable
        {
            get
            {
                var result = false;
                if (handlers != null && handlers.Count > 0)
                    foreach (var group in handlers)
                        if (group.role != null && group.handlers != null && group.handlers.Count > 0)
                            if ((group.role.handlingTypes & HandlingTypeFlags.Weapons) != HandlingTypeFlags.None)
                                result = group.handlers.Any((Pawn x) => !x.Dead && !x.Downed);
                return result;
            }
        }

        public Pawn Pawn => parent as Pawn;


        public List<Pawn> AllOccupants
        {
            get
            {
                var result = new List<Pawn>();
                if (handlers != null && handlers.Count > 0)
                    foreach (var group in handlers)
                        if (group.handlers != null && group.handlers.Count > 0) result.AddRange(group.handlers);
                return result;
            }
        }

        public CompProperties_Vehicle Props => (CompProperties_Vehicle) props;

        public void ResolveITab()
        {
            if (!ResolvedITTab)
            {
                ResolvedITTab = true;
                //PostExposeData();
                //Make the ITab
                var tabs = Pawn.GetInspectTabs();
                if (tabs != null && tabs.Count() > 0)
                    if (tabs.FirstOrDefault(x => x is ITab_Passengers) == null)
                        try
                        {
                            Pawn.def.inspectorTabsResolved.Add(
                                InspectTabManager.GetSharedInstance(typeof(ITab_Passengers)));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(string.Concat("Could not instantiate inspector tab of type ",
                                typeof(ITab_Passengers), ": ", ex));
                        }
            }
        }

        private Pawn GeneratePawn(List<PawnGenOption> optionalDefs = null)
        {
            var newPawnKind = Pawn.Faction.RandomPawnKind();
            if (optionalDefs != null && optionalDefs.Count > 0)
                newPawnKind = optionalDefs.RandomElementByWeight(x => x.selectionWeight).kind;

            var request = new PawnGenerationRequest(newPawnKind, Pawn.Faction, PawnGenerationContext.NonPlayer,
                Pawn.Map.Tile, false, false, false, false, true, true, 1f, false, true, true, false, false, false,
                false, false, 0, null, (0-1), null, null, null, null);
            var item = PawnGenerator.GeneratePawn(request);
            return item;
        }

        public void InitializeVehicleHandlers()
        {
            if (handlers != null && handlers.Count > 0) return;

            if (Props.roles != null && Props.roles.Count > 0)
                foreach (var role in Props.roles)
                    handlers.Add(new VehicleHandlerGroup(Pawn, role, new List<Pawn>()));
        }

        public void ResolveFactionPilots()
        {
            //this.Pawn.pather.
            //-------- Additions Made By Swenzi --------
            //Purpose: Remove Spawning of Premade Pawns in Vehicles
            //Logic: Players should not get free pawns for making vehicles
            //Improvements: None I can think of

            //Premade Pawns should only appear for non-player-faction vehicles
            if (!Pawn.Faction.IsPlayer)
                if (!ResolvedPawns)
                {
                    ResolvedPawns = true;

                    if (handlers != null && handlers.Count > 0)
                        foreach (var group in handlers)
                        {
                            var role = group.role;
                            if (role.slotsToOperate > 0)
                            {
                                var minimum = Math.Min(role.slotsToOperate, role.slots);
                                var maximum = Math.Max(role.slotsToOperate, role.slots);
                                var range = Rand.Range(minimum, maximum);
                                for (var i = 0; i < range; i++)
                                {
                                    var newPawn = GeneratePawn(role.preferredHandlers);
                                    if (newPawn != null)
                                        group.handlers.TryAdd(newPawn);
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
            //Adjustments by Jecrell

            if (parent is Pawn vehicle && vehicle.Spawned && handlers != null && handlers.Count > 0 &&
                !(Pawn.GetLord()?.LordJob?.ToString() == "RimWorld.LordJob_FormAndSendCaravan") &&
                !(Pawn.CurJob?.def == JobDefOf.UnloadYourInventory) && !Pawn.Position.InNoBuildEdgeArea(Pawn.Map) &&
                !Pawn.IsFighting()
            )
            {
                //Do not eject anyone from the vehicle for needs during combat.
                //It's too dangerous to leave during combat.
                if (this?.Pawn?.Map?.attackTargetsCache?.GetPotentialTargetsFor(Pawn)
                        ?.FirstOrDefault(x => !x.ThreatDisabled(vehicle)) == null) //TODO May require further investigation for proper update
                    foreach (var group in handlers)
                    {
                        var toEject = group?.handlers?.InnerListForReading?.FirstOrDefault(x =>
                            !x.Spawned &&
                            x?.needs?.AllNeeds?.FirstOrDefault(
                                y => y.CurLevelPercentage < Props.ejectIfBelowNeedPercent) != null);
                        if (toEject != null)
                        {
                            Messages.Message("MessagePawnLeftVehicle".Translate(toEject.Label, Pawn.Label, "low"), Pawn,
                                MessageTypeDefOf.NegativeHealthEvent);
                            Pawn b;
                            Eject(toEject);
                        }
                    }

                //}

                //----Additions Made By Swenzi-----

                //Every 250 ticks
                if (Props.ejectIfBelowHealthPercent > 0.0f)
                    if (Find.TickManager.TicksGame % 250 == 0)
                    {
                        if (Pawn.Dead || Pawn.Downed)
                            if (handlers != null && handlers.Count > 0)
                            {
                                foreach (var group in handlers)
                                    EjectAll();
                                weaponStatus = WeaponState.frozen;
                                movingStatus = MovingState.frozen;
                                if (Pawn.Downed && Pawn.Faction != Faction.OfPlayerSilentFail)
                                    Pawn.SetFaction(Faction.OfPlayerSilentFail);
                                return;
                            }

                        if (Pawn.health != null)
                            if (Pawn.health.summaryHealth != null)
                            {
                                var currentHealthPercentage = Pawn.health.summaryHealth.SummaryHealthPercent;
                                if (currentHealthPercentage < Props.ejectIfBelowHealthPercent)
                                    if (handlers != null && handlers.Count > 0)
                                    {
                                        foreach (var group in handlers)
                                            EjectAll();
                                        weaponStatus = WeaponState.frozen;
                                        movingStatus = MovingState.frozen;
                                        if (Pawn.Downed) Pawn.SetFaction(Faction.OfPlayerSilentFail);
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
            if (Pawn?.GetComp<CompRefuelable>() is CompRefuelable compRefuelable)
            {
                //------ ADB Swenzi ------
                //If it isn't moving than it shouldn't use fuel
                if (Pawn.pather != null && !Pawn.pather.Moving ||
                    Pawn.GetCaravan() != null && Pawn.GetCaravan().CantMove)
                    compRefuelable.Props.fuelConsumptionRate = 0f;
                else
                    //If it's moving than it should use fuel
                    compRefuelable.Props.fuelConsumptionRate = fuelConsumptionRate;
                //------ ADB Swenzi ------

                if (!compRefuelable.HasFuel)
                {
                    weaponStatus = WeaponState.frozen;
                    movingStatus = MovingState.frozen;
                    return;
                }
            }

            if (MovementHandlerAvailable && movingStatus == MovingState.frozen)
                movingStatus = MovingState.able;

            if (WeaponHandlerAvailable && weaponStatus == WeaponState.frozen)
                weaponStatus = WeaponState.able;

            if (!MovementHandlerAvailable && movingStatus == MovingState.able &&
                Props.movementHandling != HandlingType.NoHandlerRequired)
                movingStatus = MovingState.frozen;

            if (!WeaponHandlerAvailable && weaponStatus == WeaponState.able &&
                Props.weaponHandling != HandlingType.NoHandlerRequired)
                weaponStatus = WeaponState.frozen;

            if (!ManipulationHandlerAvailable && manipulationStatus == ManipulationState.able &&
                Props.manipulationHandling != HandlingType.NoHandlerRequired)
                manipulationStatus = ManipulationState.frozen;

            // ------ ADB Swenzi -------
            //If it can move and it's in a caravan wandering than it might be stuck 
            //aka the movement thing hasn't kicked in. Change draft status just to be safe.

            //Fixes bug where weapon tries to fire even after gunner is removed
            if (weaponStatus != WeaponState.able)
                if (this?.Pawn?.CurJob?.def == JobDefOf.Wait_Combat ||
                    this?.Pawn?.CurJob?.def == JobDefOf.AttackStatic ||
                    this?.Pawn?.CurJob?.def == JobDefOf.AttackMelee)
                    if (!this?.Pawn?.pather?.Moving ?? false)
                        Pawn.jobs.EndCurrentJob(JobCondition.None, false);
                    else
                        Pawn.jobs.EndCurrentJob(JobCondition.None, true);
            if (movingStatus == MovingState.able)
            {
                //Removed caravan member check as apparently pawns currently forming a caravan aren't part of one yet
                //          if (this.Pawn.CurJob != null && this.Pawn.CurJob.def == JobDefOf.GotoWander)
                //          {
                //              if (!this.draftStatusChanged){
                //this.Pawn.drafter.Drafted = !this.Pawn.Drafted;
                //this.draftStatusChanged = true;
                //              }
                //          }
                //          else
                //          {
                //              //Safety to allow this for future caravans
                //              this.draftStatusChanged = false;
                //          }
            }
            else
            {
                //Vehicles that can't move shouldn't have Lords, it causes problems cause they never complete their jobs and toils
                if (this?.Pawn?.GetLord() != null)
                    Pawn.GetLord().lordManager.RemoveLord(Pawn.GetLord());

                if (Pawn.pather != null && Pawn.pather.Moving)
                    if (tickCount > Props.momentumTimeSeconds * 60)
                    {
                        //No more fake momentum, vehicle should stop
                        //this.Pawn.jobs.
                        if (Pawn.pather.Moving) Pawn.jobs.EndCurrentJob(JobCondition.None, false);
                        Pawn.pather.StopDead();
                        tickCount = 0;
                    }
                    else
                    {
                        //Be cool, keep the momentum going
                        tickCount++;
                    }
            }
            if (movingStatus != MovingState.able && weaponStatus != WeaponState.able &&
                this?.Pawn?.mindState != null)
                Pawn.mindState.lastJobTag = JobTag.Idle;
            // ------ ADB Swenzi -------
            if (Pawn.Drafted && movingStatus == MovingState.frozen)
                Pawn.drafter.Drafted = false;
        }

        // ------ Additions made by Swenzi ------
        //Purpose: Reduce the needs of pawns while in the vehicle
        //Logic: Prevent cryosleep gimmicks by storing pawns in vehicles forever
        //To prevent unneccessary patching of game methods, needs are modified by this function
        //Improvements: Mood modifiers if they stay too long? Traits effecting mood? Driving Experience?
        //
        //Addendum by Jecrell:
        //-Added the ability for some pawns to resolve needs inside the vehicle.
        //-Prevent needs from falling while resting.
        public void ResolveNeeds()
        {
            //Player only, NPC factions is weird unless better logic is make them go back to vehicles after being ejected
            if (Pawn.Faction.IsPlayer)
                if (handlers != null && handlers.Count > 0)
                    foreach (var group in handlers)
                        if ((group?.handlers?.Count ?? 0) > 0)
                            for (var i = 0; i < group.handlers.Count; i++)
                            {
                                var pawn = group.handlers[i];
                                //Don't change needs while a caravan member.
                                if (pawn.IsCaravanMember()) continue;
                                var pawn_needs = pawn.needs.AllNeeds;
                                //These needs are major and should change
                                for (var j = 0; j < pawn_needs.Count; j++)
                                {
                                    if (pawn_needs[j] is Need_Rest need_Rest)
                                    {
                                        pawn_needs[j].CurLevel -= Props.restNeedRate;
                                        CompVehicleUtility.TrySatisfyRestNeed(pawn, need_Rest, Pawn);
                                    }
                                    if (pawn_needs[j] is Need_Food need_Food)
                                    {
                                        pawn_needs[j].CurLevel -= Props.foodNeedRate;
                                        CompVehicleUtility.TrySatisfyFoodNeed(pawn, need_Food, Pawn);
                                    }
                                    if (pawn_needs[j] is Need_Chemical need_Chemical)
                                    {
                                        pawn_needs[j].CurLevel -= Props.foodNeedRate;
                                        CompVehicleUtility.TrySatisfyChemicalNeed(pawn, need_Chemical, Pawn);
                                    }
                                    if (pawn_needs[j].def == NeedDefOf.Joy)
                                        pawn_needs[j].CurLevel -= Props.joyNeedRate;
                                }
                            }
        }

        // ------ Additions made by Swenzi ------

        public void RemovePawn(Pawn pawn)
        {
            //Log.Message("Remove1");
            if (handlers is List<VehicleHandlerGroup> groups && !groups.NullOrEmpty())
            {
                //Log.Message("Remove2");

                var tempGroups = groups.FindAll(x => x.handlers.InnerListForReading.Contains(pawn));
                if (!tempGroups.NullOrEmpty())
                    foreach (var group in tempGroups)
                        //Log.Message("Remove4");

                        if (group.handlers.InnerListForReading.Remove(pawn))
                            return;
            }
        }

        public void Eject(Pawn pawn)
        {
            if (!pawn.Spawned)
                GenSpawn.Spawn(pawn, Pawn.PositionHeld.RandomAdjacentCell8Way(), Pawn.MapHeld);
            RemovePawn(pawn);
        }

        public void EjectAll()
        {
            var pawnsToEject = new List<Pawn>(AllOccupants);
            if (pawnsToEject != null && pawnsToEject.Count > 0)
                foreach (var p in pawnsToEject)
                    Eject(p);
        }

        public void GiveLoadJob(Thing thingToLoad, VehicleHandlerGroup group)
        {
            if (thingToLoad is Pawn pawn)
            {
                var newJob = new Job(DefDatabase<JobDef>.GetNamed("CompVehicle_LoadPassenger"), Pawn);
                pawn.jobs.TryTakeOrderedJob(newJob);

                if (bills != null && bills.Count > 0)
                {
                    var bill = bills.FirstOrDefault(x => x.pawnToLoad == pawn);
                    if (bill != null)
                    {
                        bill.group = group;
                        return;
                    }
                }
                bills.Add(new Bill_LoadVehicle(pawn, Pawn, group));
            }
        }

        public void Notify_Loaded(Pawn pawnToLoad)
        {
            if ((bills != null) & (bills.Count > 0))
            {
                var bill = bills.FirstOrDefault(x => x.pawnToLoad == pawnToLoad);
                if (bill != null)
                {
                    if (pawnToLoad.IsWorldPawn())
                        Log.Warning("Called LoadPawn on world pawn");
                    //var curFaction = pawnToLoad.Faction;
                    pawnToLoad.DeSpawn();
                    if (pawnToLoad.holdingOwner != null)
                        pawnToLoad.holdingOwner.TryTransferToContainer(pawnToLoad, bill.group.handlers);
                    else bill.group.handlers.TryAdd(pawnToLoad);
                    if (!pawnToLoad.IsWorldPawn())
                        Find.WorldPawns.PassToWorld(pawnToLoad, PawnDiscardDecideMode.Decide);
                    //pawnToLoad.SetFaction(curFaction);
                    bills.Remove(bill);
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

        //Additions by Swenzi 1/1/2018
        public override void PostDraw()
        {
            //Additions by Swenzi 1/1/2018
            ResolveGraphics();
            base.PostDraw();
        }
        //Draw the drivers in the vehicle if drawDrivers is true
        public void ResolveGraphics(){
            if(Props.drawDrivers){
                List<Pawn> haulers = new List<Pawn>();
                if (handlers != null && handlers.Count > 0)
                {
                    //Amass a list of drivers that are alive and in a slot modifying movement
                    foreach (var group in handlers)
                    {
                        if (group.handlers != null && group.handlers.Count > 0){
                            if (group.role != null && (group.role.handlingTypes & HandlingTypeFlags.Movement) != HandlingTypeFlags.None)
                            {
                                if (group.handlers.Any((Pawn x) =>  !x.Downed && !x.Dead))
                                {
                                    foreach (Pawn p in group.handlers)
                                    {
                                        if (!p.Downed && !p.Dead)
                                        {
                                            haulers.Add(p);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if(haulers.Count > 0){
                        //Display the drivers that are in the vehicle
                        Pawn p = parent as Pawn;
                        float width = p.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize.x;
                        float height = p.ageTracker.CurKindLifeStage.bodyGraphicData.drawSize.y;
                        float xMod = -width / 4;
                        float yMod = 0;
                        foreach(Pawn d in haulers){
                            d.Rotation = parent.Rotation;
                            //If it's the last driver to be displayed and on a new row, put it in the center
                            if (haulers.Count % 2 == 1 && d == haulers.Last())
                            {
                                d.Drawer.renderer.RenderPawnAt(ResolveOffset(ResolveOffset(p.Drawer.DrawPos, new Vector3(0, 0, yMod), parent.Rotation), Props.drawOffset, parent.Rotation));
                            }
                            else{
                                //Draw drivers in 2 x 1 rows
                                        d.Drawer.renderer.RenderPawnAt(ResolveOffset(ResolveOffset(p.Drawer.DrawPos,new Vector3(xMod, 0, yMod),parent.Rotation), Props.drawOffset, parent.Rotation));
                                        xMod += 2 * width/4;
                                        if(xMod > 2 * width / 4){
                                            xMod = -width/3;
                                            yMod += height/3;
                                        }
                            }
                        }
                    }
                }
            }
        }

        public Vector3 ResolveOffset(Vector3 oldPos, Vector3 offset, Rot4 rot){
            Vector3 newPos = oldPos;
            if(rot == Rot4.North){
                newPos = new Vector3(oldPos.x + offset.x, oldPos.y, oldPos.z + offset.z);
            }
            if(rot == Rot4.East){
                newPos = new Vector3(oldPos.x + offset.z, oldPos.y, oldPos.z - offset.x);
            }
            if(rot == Rot4.South){
                newPos = new Vector3(oldPos.x - offset.x, oldPos.y, oldPos.z - offset.z);
            }
            if(rot == Rot4.West){
                newPos = new Vector3(oldPos.x - offset.z, oldPos.y, oldPos.z + offset.x);
            }
            return newPos;
        }


        public void ResolveMovementSound()
        {
            if (parent is Pawn p && Props.soundMoving != null)
            {
                var isMovingNow = p?.pather?.Moving ?? false;
                if (isMovingNow && movingSustainer == null)
                {
                    var info = SoundInfo.InMap(parent, MaintenanceType.None);
                    movingSustainer = Props.soundMoving.TrySpawnSustainer(info);
                }
                else if (!isMovingNow && movingSustainer != null)
                {
                    movingSustainer.End();
                    movingSustainer = null;
                }
            }
        }

        public void GetVehicleButtonFloatMenu(VehicleHandlerGroup group, bool canLoad)
        {
            var list = new List<FloatMenuOption>();
            var map = Pawn.Map;
            var tempList = group?.handlers?.InnerListForReading != null
                ? new List<Pawn>(group.handlers.InnerListForReading)
                : new List<Pawn>();
            if (canLoad && tempList.Count == 0)
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                //Additions by Swenzi 1/1/2018
                //Allow animals to ride in vehicles or pull vehicles (animals only allowed to be in slots that allow movement)
                if (Props.animalDrivers && (group.role.handlingTypes & HandlingTypeFlags.Movement) != HandlingTypeFlags.None)
                {
                    Find.Targeter.BeginTargeting(
                        new TargetingParameters
                        {
                            validator = ti => ti.Thing is Pawn p && p.Faction != null && p.Faction.IsPlayer && p.training != null && p.training.HasLearned(DefDatabase<TrainableDef>.GetNamed("Haul")) && p.BodySize > Props.minBodySize
                        }, delegate (LocalTargetInfo target) { GiveLoadJob(target.Thing, group); }, null, null, null);
                }else{
                    Find.Targeter.BeginTargeting(
                        new TargetingParameters
                        {
                            validator = ti => ti.Thing is Pawn p && p.RaceProps.Humanlike && p.IsColonistPlayerControlled
                        }, delegate (LocalTargetInfo target) { GiveLoadJob(target.Thing, group); }, null, null, null);

                }
                return;
            }
            if (canLoad)
            {
                var text = "CompVehicle_Load".Translate(group.role.label);
                //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, handler);

                //Additions by Swenzi 1/1/2018
                //Allow animals to ride in vehicles or pull vehicles (animals only allowed to be in slots that allow movement)
                if(Props.animalDrivers && (group.role.handlingTypes & HandlingTypeFlags.Movement) != HandlingTypeFlags.None){
                    list.Add(new FloatMenuOption(text, delegate
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                        Find.Targeter.BeginTargeting(
                            new TargetingParameters
                            {
                                validator = ti =>
                                ti.Thing is Pawn p && p.Faction != null && p.Faction.IsPlayer && p.training != null && p.training.HasLearned(DefDatabase<TrainableDef>.GetNamed("Haul")) && p.BodySize > Props.minBodySize
                            }, delegate (LocalTargetInfo target) { GiveLoadJob(target.Thing, group); }, null, null, null);
                    }, MenuOptionPriority.Default, null, null, 29f, null, null));
                }
                else{
                    list.Add(new FloatMenuOption(text, delegate
                    {
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                        Find.Targeter.BeginTargeting(
                            new TargetingParameters
                            {
                                validator = ti =>
                                    ti.Thing is Pawn p && p.RaceProps.Humanlike && p.IsColonistPlayerControlled
                            }, delegate (LocalTargetInfo target) { GiveLoadJob(target.Thing, group); }, null, null, null);
                    }, MenuOptionPriority.Default, null, null, 29f, null, null));

                }
            }
            if (!canLoad && tempList.Count == 1)
            {
                var temptempList = new List<Pawn>(tempList);
                foreach (var handler in temptempList)
                    Eject(handler);
                temptempList.Clear();
                return;
            }
            if (tempList.Count != 0)
                foreach (var handler in tempList)
                {
                    var text = "CompVehicle_Unload".Translate(handler.Name.ToStringFull);
                    var arg_121_0 = list;
                    Func<Rect, bool> extraPartOnGUI = rect =>
                        Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, handler);
                    list.Add(new FloatMenuOption(text, delegate { Eject(handler); }, MenuOptionPriority.Default, null,
                        null, 29f, extraPartOnGUI, null));
                }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var enumerator = base.CompGetGizmosExtra().GetEnumerator();
            while (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                yield return current;
            }
            //Log.Message("0");
            if (Pawn.Faction == Faction.OfPlayerSilentFail)
            {
                //Log.Message("1");
                if (Props.roles != null && Props.roles.Count > 0)
                    foreach (var group in handlers)
                        //Log.Message("3");
                        if (group.role != null)
                        {
                            //Log.Message("4");
                            var loadable = group.AreSlotsAvailable;
                            var unloadable = (group?.handlers?.Count ?? 0) > 0;
                            if (loadable || unloadable)
                            {
                                //Log.Message("5");
                                var button = new Command_VehicleHandler
                                {
                                    action = delegate
                                    {
                                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                                        GetVehicleButtonFloatMenu(group, loadable);
                                    },
                                    hotKey = KeyBindingDefOf.Misc1
                                };
                                var label = "CompVehicle_Load";
                                var desc = "CompVehicle_LoadDesc";
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
                                button.icon =
                                    TexCommand
                                        .Install; //ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", true);

                                var pctFilled = Pawn.health.summaryHealth.SummaryHealthPercent;
                                button.disabled = Pawn.Downed || Pawn.Dead ||
                                                  pctFilled < Props.ejectIfBelowHealthPercent;
                                button.disabledReason = "CompVehicle_DisabledDesc".Translate();

                                //Log.Message(button.ToString());
                                yield return button;
                            }
                        }
                if (Pawn.drafter == null) Pawn.drafter = new Pawn_DraftController(Pawn);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ResolvedPawns, "ResolvedPawns", false);
            Scribe_Values.Look(ref weaponStatus, "weaponStatus", WeaponState.able);
            Scribe_Values.Look(ref movingStatus, "movingStatus", MovingState.able);
            Scribe_Values.Look(ref lastDirection, "lastDirection", Rot4.South);

            Scribe_Collections.Look(ref vehicleContents, "vehicleContents",
                LookMode.Deep); //Stores the handlergroups of the vehicle and its pawns while the vehicle is in a caravan
            Scribe_Collections.Look(ref handlers, "handlers", LookMode.Deep);
            Scribe_Collections.Look(ref bills, "bills", LookMode.Deep);

            //Scribe_Collections.Look<Pawn>(ref this.pilots, "pilots", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.gunners, "gunners", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.crew, "crew", LookMode.Deep, new object[0]);
            //Scribe_Collections.Look<Pawn>(ref this.passengers, "passengers", LookMode.Deep, new object[0]);
        }

        #region SwenzisCode

        //------ Additions By Swenzi -------
        //Purpose: Control the boolean warnedOnNoFuel
        //Logic: Needed to prevent spamming of the warning message

        public bool WarnedOnNoFuel
        {
            get => warnedNoFuel;

            set => warnedNoFuel = value;
        }

        //Purpose: Store the pawns in the vehicle while it is in a caravan
        //Logic: Allows the vehicle to remember what pawns were inside it so they can be put back in later on map entry

        public List<VehicleHandlerTemp> PawnsInVehicle
        {
            get => vehicleContents;

            set => vehicleContents = value;
        }

        //------ Additions By Swenzi -------

        #endregion SwenzisCode
    }
}