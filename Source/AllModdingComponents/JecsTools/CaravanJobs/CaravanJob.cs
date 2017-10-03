using RimWorld;
using System;
using System.Collections.Generic;
using Verse.AI.Group;
using Verse;
using RimWorld.Planet;
using Verse.AI;

namespace JecsTools
{
    public class CaravanJob : IExposable
    {
        public CaravanJobDef def;

        public GlobalTargetInfo targetA = GlobalTargetInfo.Invalid;

        public GlobalTargetInfo targetB = GlobalTargetInfo.Invalid;

        public GlobalTargetInfo targetC = GlobalTargetInfo.Invalid;

        public List<GlobalTargetInfo> targetQueueA;

        public List<GlobalTargetInfo> targetQueueB;

        public int count = -1;

        public List<int> countQueue;

        public int startTick = -1;

        public int expiryInterval = -1;

        public bool checkOverrideOnExpire;

        public bool playerForced;

        public List<ThingStackPartClass> placedThings;

        public int maxNumMeleeAttacks = 2147483647;

        public int maxNumStaticAttacks = 2147483647;

        public LocomotionUrgency locomotionUrgency = LocomotionUrgency.Jog;

        public HaulMode haulMode;

        public Bill bill;

        public ICommunicable commTarget;

        public ThingDef plantDefToSow;

        public Verb verbToUse;

        public bool haulOpportunisticDuplicates;

        public bool exitMapOnArrival;

        public bool failIfCantJoinOrCreateCaravan;

        public bool killIncappedTarget;

        public bool ignoreForbidden;

        public bool ignoreDesignations;

        public bool canBash;

        public bool haulDroppedApparel;

        public bool restUntilHealed;

        public bool ignoreJoyTimeAssignment;

        public bool overeat;

        public bool attackDoorIfTargetLost;

        public int takeExtraIngestibles;

        public bool expireRequiresEnemiesNearby;

        public Lord lord;

        public bool collideWithCaravans;

        public RecipeDef RecipeDef
        {
            get
            {
                return this.bill.recipe;
            }
        }

        public CaravanJob()
        {
        }

        public CaravanJob(CaravanJobDef def) : this(def, default(GlobalTargetInfo))
        {
        }

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA) : this(def, targetA, default(GlobalTargetInfo))
        {
        }

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA, GlobalTargetInfo targetB)
        {
            this.def = def;
            this.targetA = targetA;
            this.targetB = targetB;
        }

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA, GlobalTargetInfo targetB, GlobalTargetInfo targetC)
        {
            this.def = def;
            this.targetA = targetA;
            this.targetB = targetB;
            this.targetC = targetC;
        }

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA, int expiryInterval, bool checkOverrideOnExpiry = false)
        {
            this.def = def;
            this.targetA = targetA;
            this.expiryInterval = expiryInterval;
            this.checkOverrideOnExpire = checkOverrideOnExpiry;
        }

        public CaravanJob(CaravanJobDef def, int expiryInterval, bool checkOverrideOnExpiry = false)
        {
            this.def = def;
            this.expiryInterval = expiryInterval;
            this.checkOverrideOnExpire = checkOverrideOnExpiry;
        }

        public GlobalTargetInfo GetTarget(TargetIndex ind)
        {
            switch (ind)
            {
                case TargetIndex.A:
                    return this.targetA;
                case TargetIndex.B:
                    return this.targetB;
                case TargetIndex.C:
                    return this.targetC;
                default:
                    throw new ArgumentException();
            }
        }

        public List<GlobalTargetInfo> GetTargetQueue(TargetIndex ind)
        {
            if (ind == TargetIndex.A)
            {
                if (this.targetQueueA == null)
                {
                    this.targetQueueA = new List<GlobalTargetInfo>();
                }
                return this.targetQueueA;
            }
            if (ind != TargetIndex.B)
            {
                throw new ArgumentException();
            }
            if (this.targetQueueB == null)
            {
                this.targetQueueB = new List<GlobalTargetInfo>();
            }
            return this.targetQueueB;
        }

        public void SetTarget(TargetIndex ind, GlobalTargetInfo pack)
        {
            switch (ind)
            {
                case TargetIndex.A:
                    this.targetA = pack;
                    return;
                case TargetIndex.B:
                    this.targetB = pack;
                    return;
                case TargetIndex.C:
                    this.targetC = pack;
                    return;
                default:
                    throw new ArgumentException();
            }
        }

        public void AddQueuedTarget(TargetIndex ind, GlobalTargetInfo target)
        {
            this.GetTargetQueue(ind).Add(target);
        }

        public void ExposeData()
        {
            ILoadReferenceable loadReferenceable = (ILoadReferenceable)this.commTarget;
            Scribe_References.Look<ILoadReferenceable>(ref loadReferenceable, "commTarget", false);
            this.commTarget = (ICommunicable)loadReferenceable;
            Scribe_References.Look<Verb>(ref this.verbToUse, "verbToUse", false);
            Scribe_References.Look<Bill>(ref this.bill, "bill", false);
            Scribe_References.Look<Lord>(ref this.lord, "lord", false);
            Scribe_Defs.Look<CaravanJobDef>(ref this.def, "def");
            Scribe_TargetInfo.Look(ref this.targetA, "targetA");
            Scribe_TargetInfo.Look(ref this.targetB, "targetB");
            Scribe_TargetInfo.Look(ref this.targetC, "targetC");
            Scribe_Collections.Look<GlobalTargetInfo>(ref this.targetQueueA, "targetQueueA", LookMode.Undefined, new object[0]);
            Scribe_Collections.Look<GlobalTargetInfo>(ref this.targetQueueB, "targetQueueB", LookMode.Undefined, new object[0]);
            Scribe_Values.Look<int>(ref this.count, "count", -1, false);
            Scribe_Collections.Look<int>(ref this.countQueue, "countQueue", LookMode.Undefined, new object[0]);
            Scribe_Values.Look<int>(ref this.startTick, "startTick", -1, false);
            Scribe_Values.Look<int>(ref this.expiryInterval, "expiryInterval", -1, false);
            Scribe_Values.Look<bool>(ref this.checkOverrideOnExpire, "checkOverrideOnExpire", false, false);
            Scribe_Values.Look<bool>(ref this.playerForced, "playerForced", false, false);
            Scribe_Collections.Look<ThingStackPartClass>(ref this.placedThings, "placedThings", LookMode.Undefined, new object[0]);
            Scribe_Values.Look<int>(ref this.maxNumMeleeAttacks, "maxNumMeleeAttacks", 2147483647, false);
            Scribe_Values.Look<int>(ref this.maxNumStaticAttacks, "maxNumStaticAttacks", 2147483647, false);
            Scribe_Values.Look<bool>(ref this.exitMapOnArrival, "exitMapOnArrival", false, false);
            Scribe_Values.Look<bool>(ref this.failIfCantJoinOrCreateCaravan, "failIfCantJoinOrCreateCaravan", false, false);
            Scribe_Values.Look<bool>(ref this.killIncappedTarget, "killIncappedTarget", false, false);
            Scribe_Values.Look<bool>(ref this.haulOpportunisticDuplicates, "haulOpportunisticDuplicates", false, false);
            Scribe_Values.Look<HaulMode>(ref this.haulMode, "haulMode", HaulMode.Undefined, false);
            Scribe_Defs.Look<ThingDef>(ref this.plantDefToSow, "plantDefToSow");
            Scribe_Values.Look<LocomotionUrgency>(ref this.locomotionUrgency, "locomotionUrgency", LocomotionUrgency.Jog, false);
            Scribe_Values.Look<bool>(ref this.ignoreDesignations, "ignoreDesignations", false, false);
            Scribe_Values.Look<bool>(ref this.canBash, "canBash", false, false);
            Scribe_Values.Look<bool>(ref this.haulDroppedApparel, "haulDroppedApparel", false, false);
            Scribe_Values.Look<bool>(ref this.restUntilHealed, "restUntilHealed", false, false);
            Scribe_Values.Look<bool>(ref this.ignoreJoyTimeAssignment, "ignoreJoyTimeAssignment", false, false);
            Scribe_Values.Look<bool>(ref this.overeat, "overeat", false, false);
            Scribe_Values.Look<bool>(ref this.attackDoorIfTargetLost, "attackDoorIfTargetLost", false, false);
            Scribe_Values.Look<int>(ref this.takeExtraIngestibles, "takeExtraIngestibles", 0, false);
            Scribe_Values.Look<bool>(ref this.expireRequiresEnemiesNearby, "expireRequiresEnemiesNearby", false, false);
            Scribe_Values.Look<bool>(ref this.collideWithCaravans, "collideWithCaravans", false, false);
        }

        public CaravanJobDriver MakeDriver(Caravan driverCaravan)
        {
            CaravanJobDriver jobDriver = (CaravanJobDriver)Activator.CreateInstance(this.def.driverClass);
            jobDriver.caravan = driverCaravan;
            Log.Message("JecsTools :: MakeDriver Called :: " + this.def.driverClass.ToString());
            return jobDriver;
        }

        public bool CanBeginNow(Caravan Caravan)
        {
            return true; //For now
        }

        public bool JobIsSameAs(CaravanJob other)
        {
            return other != null && this.def == other.def && !(this.targetA != other.targetA) && !(this.targetB != other.targetB) && this.verbToUse == other.verbToUse && !(this.targetC != other.targetC) && this.commTarget == other.commTarget && this.bill == other.bill;
        }

        public bool AnyTargetIs(GlobalTargetInfo target)
        {
            return target.IsValid && (this.targetA == target || this.targetB == target || this.targetC == target || (this.targetQueueA != null && this.targetQueueA.Contains(target)) || (this.targetQueueB != null && this.targetQueueB.Contains(target)));
        }

        public override string ToString()
        {
            string text = this.def.ToString();
            if (this.targetA.IsValid)
            {
                text = text + " A=" + this.targetA.ToString();
            }
            if (this.targetB.IsValid)
            {
                text = text + " B=" + this.targetB.ToString();
            }
            if (this.targetC.IsValid)
            {
                text = text + " C=" + this.targetC.ToString();
            }
            return text;
        }
    }
}
