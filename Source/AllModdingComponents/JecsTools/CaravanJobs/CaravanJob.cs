using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace JecsTools
{
    public class CaravanJob : IExposable
    {
        public bool attackDoorIfTargetLost;

        public Bill bill;

        public bool canBash;

        public bool checkOverrideOnExpire;

        public bool collideWithCaravans;

        public ICommunicable commTarget;

        public int count = -1;

        public List<int> countQueue;
        public CaravanJobDef def;

        public bool exitMapOnArrival;

        public bool expireRequiresEnemiesNearby;

        public int expiryInterval = -1;

        public bool failIfCantJoinOrCreateCaravan;

        public bool haulDroppedApparel;

        public HaulMode haulMode;

        public bool haulOpportunisticDuplicates;

        public bool ignoreDesignations;

        public bool ignoreForbidden;

        public bool ignoreJoyTimeAssignment;

        public bool killIncappedTarget;

        public LocomotionUrgency locomotionUrgency = LocomotionUrgency.Jog;

        public Lord lord;

        public int maxNumMeleeAttacks = 2147483647;

        public int maxNumStaticAttacks = 2147483647;

        public bool overeat;

        public List<ListerMergeables> placedThings;

        public ThingDef plantDefToSow;

        public bool playerForced;

        public bool restUntilHealed;

        public int startTick = -1;

        public int takeExtraIngestibles;

        public GlobalTargetInfo targetA = GlobalTargetInfo.Invalid;

        public GlobalTargetInfo targetB = GlobalTargetInfo.Invalid;

        public GlobalTargetInfo targetC = GlobalTargetInfo.Invalid;

        public List<GlobalTargetInfo> targetQueueA;

        public List<GlobalTargetInfo> targetQueueB;

        public Verb verbToUse;

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

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA, GlobalTargetInfo targetB,
            GlobalTargetInfo targetC)
        {
            this.def = def;
            this.targetA = targetA;
            this.targetB = targetB;
            this.targetC = targetC;
        }

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA, int expiryInterval,
            bool checkOverrideOnExpiry = false)
        {
            this.def = def;
            this.targetA = targetA;
            this.expiryInterval = expiryInterval;
            checkOverrideOnExpire = checkOverrideOnExpiry;
        }

        public CaravanJob(CaravanJobDef def, int expiryInterval, bool checkOverrideOnExpiry = false)
        {
            this.def = def;
            this.expiryInterval = expiryInterval;
            checkOverrideOnExpire = checkOverrideOnExpiry;
        }

        public RecipeDef RecipeDef => bill.recipe;

        public void ExposeData()
        {
            var loadReferenceable = (ILoadReferenceable) commTarget;
            Scribe_References.Look(ref loadReferenceable, "commTarget", false);
            commTarget = (ICommunicable) loadReferenceable;
            Scribe_References.Look(ref verbToUse, "verbToUse", false);
            Scribe_References.Look(ref bill, "bill", false);
            Scribe_References.Look(ref lord, "lord", false);
            Scribe_Defs.Look(ref def, "def");
            Scribe_TargetInfo.Look(ref targetA, "targetA");
            Scribe_TargetInfo.Look(ref targetB, "targetB");
            Scribe_TargetInfo.Look(ref targetC, "targetC");
            Scribe_Collections.Look(ref targetQueueA, "targetQueueA", LookMode.Undefined);
            Scribe_Collections.Look(ref targetQueueB, "targetQueueB", LookMode.Undefined);
            Scribe_Values.Look(ref count, "count", -1, false);
            Scribe_Collections.Look(ref countQueue, "countQueue", LookMode.Undefined);
            Scribe_Values.Look(ref startTick, "startTick", -1, false);
            Scribe_Values.Look(ref expiryInterval, "expiryInterval", -1, false);
            Scribe_Values.Look(ref checkOverrideOnExpire, "checkOverrideOnExpire", false, false);
            Scribe_Values.Look(ref playerForced, "playerForced", false, false);
            Scribe_Collections.Look(ref placedThings, "placedThings", LookMode.Undefined);
            Scribe_Values.Look(ref maxNumMeleeAttacks, "maxNumMeleeAttacks", 2147483647, false);
            Scribe_Values.Look(ref maxNumStaticAttacks, "maxNumStaticAttacks", 2147483647, false);
            Scribe_Values.Look(ref exitMapOnArrival, "exitMapOnArrival", false, false);
            Scribe_Values.Look(ref failIfCantJoinOrCreateCaravan, "failIfCantJoinOrCreateCaravan", false, false);
            Scribe_Values.Look(ref killIncappedTarget, "killIncappedTarget", false, false);
            Scribe_Values.Look(ref haulOpportunisticDuplicates, "haulOpportunisticDuplicates", false, false);
            Scribe_Values.Look(ref haulMode, "haulMode", HaulMode.Undefined, false);
            Scribe_Defs.Look(ref plantDefToSow, "plantDefToSow");
            Scribe_Values.Look(ref locomotionUrgency, "locomotionUrgency", LocomotionUrgency.Jog, false);
            Scribe_Values.Look(ref ignoreDesignations, "ignoreDesignations", false, false);
            Scribe_Values.Look(ref canBash, "canBash", false, false);
            Scribe_Values.Look(ref haulDroppedApparel, "haulDroppedApparel", false, false);
            Scribe_Values.Look(ref restUntilHealed, "restUntilHealed", false, false);
            Scribe_Values.Look(ref ignoreJoyTimeAssignment, "ignoreJoyTimeAssignment", false, false);
            Scribe_Values.Look(ref overeat, "overeat", false, false);
            Scribe_Values.Look(ref attackDoorIfTargetLost, "attackDoorIfTargetLost", false, false);
            Scribe_Values.Look(ref takeExtraIngestibles, "takeExtraIngestibles", 0, false);
            Scribe_Values.Look(ref expireRequiresEnemiesNearby, "expireRequiresEnemiesNearby", false, false);
            Scribe_Values.Look(ref collideWithCaravans, "collideWithCaravans", false, false);
        }

        public GlobalTargetInfo GetTarget(TargetIndex ind)
        {
            switch (ind)
            {
                case TargetIndex.A:
                    return targetA;
                case TargetIndex.B:
                    return targetB;
                case TargetIndex.C:
                    return targetC;
                default:
                    throw new ArgumentException();
            }
        }

        public List<GlobalTargetInfo> GetTargetQueue(TargetIndex ind)
        {
            if (ind == TargetIndex.A)
            {
                if (targetQueueA == null)
                    targetQueueA = new List<GlobalTargetInfo>();
                return targetQueueA;
            }
            if (ind != TargetIndex.B)
                throw new ArgumentException();
            if (targetQueueB == null)
                targetQueueB = new List<GlobalTargetInfo>();
            return targetQueueB;
        }

        public void SetTarget(TargetIndex ind, GlobalTargetInfo pack)
        {
            switch (ind)
            {
                case TargetIndex.A:
                    targetA = pack;
                    return;
                case TargetIndex.B:
                    targetB = pack;
                    return;
                case TargetIndex.C:
                    targetC = pack;
                    return;
                default:
                    throw new ArgumentException();
            }
        }

        public void AddQueuedTarget(TargetIndex ind, GlobalTargetInfo target)
        {
            GetTargetQueue(ind).Add(target);
        }

        public CaravanJobDriver MakeDriver(Caravan driverCaravan)
        {
            var jobDriver = (CaravanJobDriver) Activator.CreateInstance(def.driverClass);
            jobDriver.caravan = driverCaravan;
            Log.Message("JecsTools :: MakeDriver Called :: " + def.driverClass);
            return jobDriver;
        }

        public bool CanBeginNow(Caravan Caravan)
        {
            return true; //For now
        }

        public bool JobIsSameAs(CaravanJob other)
        {
            return other != null && def == other.def && !(targetA != other.targetA) && !(targetB != other.targetB) &&
                   verbToUse == other.verbToUse && !(targetC != other.targetC) && commTarget == other.commTarget &&
                   bill == other.bill;
        }

        public bool AnyTargetIs(GlobalTargetInfo target)
        {
            return target.IsValid && (targetA == target || targetB == target || targetC == target ||
                                      targetQueueA != null && targetQueueA.Contains(target) ||
                                      targetQueueB != null && targetQueueB.Contains(target));
        }

        public override string ToString()
        {
            var text = def.ToString();
            if (targetA.IsValid)
                text = text + " A=" + targetA;
            if (targetB.IsValid)
                text = text + " B=" + targetB;
            if (targetC.IsValid)
                text = text + " C=" + targetC;
            return text;
        }
    }
}