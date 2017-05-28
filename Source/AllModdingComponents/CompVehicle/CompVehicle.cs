using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Verse.AI;

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
        public List<Bill_LoadVehicle> bills = new List<Bill_LoadVehicle>();

        public bool MovementHandlerAvailable
        {
            get
            {
                bool result = false;
                if (handlers != null && handlers.Count > 0)
                {
                    foreach (VehicleHandlerGroup group in handlers)
                    {
                        if (group.handlers != null && group.handlers.Count > 0)
                        {
                            if (group.role != null)
                            {
                                if (group.role.handlesMovement)
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
                            if (Pawn.Downed && Pawn.Faction != Faction.OfPlayerSilentFail) Pawn.SetFaction(Faction.OfPlayerSilentFail);
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
        public void ResolveStatus()
        {
            //If refuelable, then check for fuel.
            CompRefuelable compRefuelable = Pawn.GetComp<CompRefuelable>();
            if (compRefuelable != null)
            {
                if (!compRefuelable.HasFuel)
                {
                    weaponStatus = WeaponState.frozen;
                    movingStatus = MovingState.frozen;
                    return;
                }
            }

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

        public void Eject(Pawn pawn, List<Pawn> list)
        {
            GenSpawn.Spawn(pawn, Pawn.PositionHeld.RandomAdjacentCell8Way(), Pawn.MapHeld);
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
            Pawn pawn = thingToLoad as Pawn;
            if (pawn != null)
            {
                Job newJob = new Job(DefDatabase<JobDef>.GetNamed("CompVehicle_LoadPassenger"), Pawn);
                pawn.jobs.TryTakeOrderedJob(newJob);

                if (bills != null && bills.Count > 0)
                {
                    var bill = bills.FirstOrDefault((Bill_LoadVehicle x) => x.pawnToLoad == pawn);
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
            if (bills != null & bills.Count > 0)
            {
                var bill = bills.FirstOrDefault((x) => x.pawnToLoad == pawnToLoad);
                if (bill != null)
                {
                    pawnToLoad.DeSpawn();
                    bill.group.handlers.Add(pawnToLoad);
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
        }
        public void GetVehicleButtonFloatMenu(VehicleHandlerGroup group, bool canLoad)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            Map map = Pawn.Map;
            List<Pawn> tempList = new List<Pawn>(group.handlers);
            if (canLoad)
            {
                string text = "CompVehicle_Load".Translate(group.role.label);
                //Func<Rect, bool> extraPartOnGUI = (Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, handler);
                list.Add(new FloatMenuOption(text, delegate
                {
                    SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                    Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target)
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
            else
            {
                list.Add(new FloatMenuOption("NoPrisoners".Translate(), delegate
                {
                }, MenuOptionPriority.Default));
            }
            Find.WindowStack.Add(new FloatMenu(list));
        }

        public CompProperties_Vehicle Props
        {
            get
            {
                return (CompProperties_Vehicle)props;
            }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            IEnumerator<Gizmo> enumerator = base.CompGetGizmosExtra().GetEnumerator();
            while (enumerator.MoveNext())
            {
                Gizmo current = enumerator.Current;
                yield return current;
            }
            //Log.Message("0");
            if (Pawn.Faction == Faction.OfPlayerSilentFail)
            {
                //Log.Message("1");
                if (Props.roles != null && Props.roles.Count > 0)
                {
                    //Log.Message("2");
                    foreach (VehicleHandlerGroup group in handlers)
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
                                var button = new Command_VehicleHandler();
                                button.action = delegate
                                {
                                    SoundDefOf.TickTiny.PlayOneShotOnCamera(null);
                                    GetVehicleButtonFloatMenu(group, loadable);
                                };
                                button.hotKey = KeyBindingDefOf.Misc1;
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
                                button.icon = TexCommand.Install;  //ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect", true);

                                button.disabled = Pawn.Downed || Pawn.Dead;
                                button.disabledReason = "CompVehicle_DisabledDesc".Translate();
                                
                                //Log.Message(button.ToString());
                                yield return button;
                            }
                        }
                    }
                }
                if (Pawn.drafter == null) Pawn.drafter = new Pawn_DraftController(Pawn);
                

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
