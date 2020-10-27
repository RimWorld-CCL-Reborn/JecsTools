using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace JecsTools
{
    // Based off Job.
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

        public int maxNumMeleeAttacks = int.MaxValue;

        public int maxNumStaticAttacks = int.MaxValue;

        public bool overeat;

        public List<ThingCountClass> placedThings;

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

        public CaravanJob(CaravanJobDef def) : this(def, default)
        {
        }

        public CaravanJob(CaravanJobDef def, GlobalTargetInfo targetA) : this(def, targetA, default)
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
            var loadReferenceable = (ILoadReferenceable)commTarget;
            Scribe_References.Look(ref loadReferenceable, nameof(commTarget));
            commTarget = (ICommunicable)loadReferenceable;
            Scribe_References.Look(ref verbToUse, nameof(verbToUse));
            Scribe_References.Look(ref bill, nameof(bill));
            Scribe_References.Look(ref lord, nameof(lord));
            Scribe_Defs.Look(ref def, nameof(def));
            Scribe_TargetInfo.Look(ref targetA, nameof(targetA));
            Scribe_TargetInfo.Look(ref targetB, nameof(targetB));
            Scribe_TargetInfo.Look(ref targetC, nameof(targetC));
            Scribe_Collections.Look(ref targetQueueA, nameof(targetQueueA), LookMode.GlobalTargetInfo);
            Scribe_Collections.Look(ref targetQueueB, nameof(targetQueueB), LookMode.GlobalTargetInfo);
            Scribe_Values.Look(ref count, nameof(count), -1);
            Scribe_Collections.Look(ref countQueue, nameof(countQueue), LookMode.Value);
            Scribe_Values.Look(ref startTick, nameof(startTick), -1);
            Scribe_Values.Look(ref expiryInterval, nameof(expiryInterval), -1);
            Scribe_Values.Look(ref checkOverrideOnExpire, nameof(checkOverrideOnExpire));
            Scribe_Values.Look(ref playerForced, nameof(playerForced));
            Scribe_Collections.Look(ref placedThings, nameof(placedThings), LookMode.Deep);
            Scribe_Values.Look(ref maxNumMeleeAttacks, nameof(maxNumMeleeAttacks), int.MaxValue);
            Scribe_Values.Look(ref maxNumStaticAttacks, nameof(maxNumStaticAttacks), int.MaxValue);
            Scribe_Values.Look(ref exitMapOnArrival, nameof(exitMapOnArrival));
            Scribe_Values.Look(ref failIfCantJoinOrCreateCaravan, nameof(failIfCantJoinOrCreateCaravan));
            Scribe_Values.Look(ref killIncappedTarget, nameof(killIncappedTarget));
            Scribe_Values.Look(ref haulOpportunisticDuplicates, nameof(haulOpportunisticDuplicates));
            Scribe_Values.Look(ref haulMode, nameof(haulMode));
            Scribe_Defs.Look(ref plantDefToSow, nameof(plantDefToSow));
            Scribe_Values.Look(ref locomotionUrgency, nameof(locomotionUrgency), LocomotionUrgency.Jog);
            Scribe_Values.Look(ref ignoreDesignations, nameof(ignoreDesignations));
            Scribe_Values.Look(ref canBash, nameof(canBash));
            Scribe_Values.Look(ref haulDroppedApparel, nameof(haulDroppedApparel));
            Scribe_Values.Look(ref restUntilHealed, nameof(restUntilHealed));
            Scribe_Values.Look(ref ignoreJoyTimeAssignment, nameof(ignoreJoyTimeAssignment));
            Scribe_Values.Look(ref overeat, nameof(overeat));
            Scribe_Values.Look(ref attackDoorIfTargetLost, nameof(attackDoorIfTargetLost));
            Scribe_Values.Look(ref takeExtraIngestibles, nameof(takeExtraIngestibles));
            Scribe_Values.Look(ref expireRequiresEnemiesNearby, nameof(expireRequiresEnemiesNearby));
            Scribe_Values.Look(ref collideWithCaravans, nameof(collideWithCaravans));
        }

        public GlobalTargetInfo GetTarget(TargetIndex ind)
        {
            return ind switch
            {
                TargetIndex.A => targetA,
                TargetIndex.B => targetB,
                TargetIndex.C => targetC,
                _ => throw new ArgumentException(),
            };
        }

        public List<GlobalTargetInfo> GetTargetQueue(TargetIndex ind)
        {
            if (ind == TargetIndex.A)
            {
                targetQueueA ??= new List<GlobalTargetInfo>();
                return targetQueueA;
            }
            if (ind != TargetIndex.B)
                throw new ArgumentException();
            targetQueueB ??= new List<GlobalTargetInfo>();
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
            var jobDriver = (CaravanJobDriver)Activator.CreateInstance(def.driverClass);
            jobDriver.caravan = driverCaravan;
            //Log.Message("JecsTools :: MakeDriver Called :: " + def.driverClass);
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
                text += " A=" + targetA;
            if (targetB.IsValid)
                text += " B=" + targetB;
            if (targetC.IsValid)
                text += " C=" + targetC;
            return text;
        }
    }
}
