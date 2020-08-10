using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompDeflector
{
    public class CompDeflector : ThingComp
    {
        public enum AccuracyRoll
        {
            CritialFailure,
            Failure,
            Success,
            CriticalSuccess
        }

        public CompProperties_Deflector Props => (CompProperties_Deflector)props;

        private int animationDeflectionTicks;

        public Verb_Deflected deflectVerb;
        public AccuracyRoll lastAccuracyRoll = AccuracyRoll.Failure;
        public bool lastShotReflected;

        public int AnimationDeflectionTicks
        {
            set => animationDeflectionTicks = value;
            get => animationDeflectionTicks;
        }

        public bool IsAnimatingNow => animationDeflectionTicks >= 0;

        private bool initComps;
        private CompEquippable compEquippable;
        private ThingComp compActivatableEffect;
        private Func<bool> compActivatableEffectIsActive;

        private void InitCompsAsNeeded()
        {
            if (!initComps)
            {
                if (parent == null) return;
                compEquippable = parent.GetComp<CompEquippable>();
                compActivatableEffect = parent.AllComps.FirstOrDefault(y => y.GetType().ToString().Contains("ActivatableEffect"));
                if (compActivatableEffect != null)
                {
                    compActivatableEffectIsActive =
                        (Func<bool>)AccessTools.Method(compActivatableEffect.GetType(), "IsActive").CreateDelegate(
                            typeof(Func<bool>), compActivatableEffect);
                }
                initComps = true;
            }
        }

        public CompEquippable GetEquippable
        {
            get
            {
                InitCompsAsNeeded();
                return compEquippable;
            }
        }

        public Pawn GetPawn => GetEquippable?.verbTracker.PrimaryVerb.CasterPawn;

        public ThingComp GetActivatableEffect
        {
            get
            {
                InitCompsAsNeeded();
                return compActivatableEffect;
            }
        }

        public bool HasCompActivatableEffect => GetActivatableEffect != null;

        public bool CompActivatableEffectiveIsActive
        {
            get
            {
                InitCompsAsNeeded();
                return compActivatableEffectIsActive?.Invoke() ?? false;
            }
        }

        internal class DeflectionChanceCalculator
        {
            private readonly CompDeflector compDeflector;
            private readonly Pawn pawn;
            private readonly CompProperties_Deflector props;
            private readonly bool fixedRandSeed;

            public DeflectionChanceCalculator(CompDeflector compDeflector, bool fixedRandSeed)
            {
                this.compDeflector = compDeflector;
                this.fixedRandSeed = fixedRandSeed;
                pawn = compDeflector.GetPawn;
                props = compDeflector.Props;
            }

            public float BeforeInfixValue { get; private set; }
            public float InfixValue { get; private set; }

            public float Calculate()
            {
                var calc = props.baseDeflectChance;
                if (pawn != null)
                {
                    if (UseSkill(out var deflectSkill))
                        calc += deflectSkill.Level * props.deflectRatePerSkillPoint;
                    BeforeInfixValue = calc;
                    // Due to possibility of DeflectionChance_InFix implementation using Rand, option to use a fixed Random seed.
                    if (fixedRandSeed)
                    {
                        Rand.PushState(0);
                        try
                        {
                            calc = compDeflector.DeflectionChance_InFix(calc);
                        }
                        finally
                        {
                            Rand.PopState();
                        }
                    }
                    else
                        calc = compDeflector.DeflectionChance_InFix(calc);
                    InfixValue = calc;
                    if (UseManipulation(out var capable) && capable)
                        calc *= pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
                }
                return Mathf.Clamp(calc, 0, 1.0f);
            }

            public bool UseSkill(out SkillRecord skill)
            {
                skill = null;
                if (props.useSkillInCalc && props.deflectSkill != null)
                    skill = pawn.skills?.GetSkill(props.deflectSkill);
                return skill != null;
            }

            public bool UseManipulation(out bool capable)
            {
                capable = props.useManipulationInCalc && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
                return props.useManipulationInCalc;
            }
        }

        internal DeflectionChanceCalculator GetDeflectionChanceCalculator(bool fixedRandSeed) => new DeflectionChanceCalculator(this, fixedRandSeed);

        public float DeflectionChance => GetDeflectionChanceCalculator(fixedRandSeed: false).Calculate();

        public string ChanceToString => DeflectionChance.ToStringPercent();

        // TODO: This is never called (and Props.deflectSkillLearnRate is never used) - should it be called upon deflection success?
        // or whenever deflection is rolled ala reflection skill (though this would mean every received hit would result in skill gain)?
        public void DeflectionSkillGain(SkillRecord skill)
        {
            GetPawn.skills?.Learn(Props.deflectSkill, Props.deflectSkillLearnRate, false);
        }

        public void ReflectionSkillGain(SkillRecord skill)
        {
            GetPawn.skills?.Learn(Props.reflectSkill, Props.reflectSkillLearnRate, false);
        }

        // TODO: Use this in a StatWorker_ReflectionAccuracy.
        internal class ReflectionAccuracyCalculator
        {
            private readonly CompDeflector compDeflector;
            private readonly Pawn pawn;
            private readonly CompProperties_Deflector props;
            private readonly bool fixedRandSeed;

            public ReflectionAccuracyCalculator(CompDeflector compDeflector, bool fixedRandSeed)
            {
                this.compDeflector = compDeflector;
                this.fixedRandSeed = fixedRandSeed;
                pawn = compDeflector.GetPawn;
                props = compDeflector.Props;
            }

            public void Calculate(out int modifier, out int difficulty, out SkillRecord skill)
            {
                modifier = 0;
                difficulty = 80;
                if (UseSkill(out skill))
                {
                    modifier += (int)(props.reflectRatePerSkillPoint * skill.Level);
                    //Log.Message("Deflection mod: " + modifier.ToString());
                }
                // Due to possibility of ReflectionAccuracy_InFix implementation using Rand, option to use a fixed Random seed.
                if (fixedRandSeed)
                {
                    Rand.PushState(0);
                    try
                    {
                        compDeflector.ReflectionAccuracy_InFix(ref modifier, ref difficulty);
                    }
                    finally
                    {
                        Rand.PopState();
                    }
                }
                else
                    compDeflector.ReflectionAccuracy_InFix(ref modifier, ref difficulty);
            }

            public bool UseSkill(out SkillRecord skill)
            {
                skill = null;
                if (props.reflectSkill != null)
                    skill = pawn.skills?.GetSkill(props.reflectSkill);
                return skill != null;
            }
        }

        internal ReflectionAccuracyCalculator GetReflectionAccuracyCalculator(bool fixedRandSeed) => new ReflectionAccuracyCalculator(this, fixedRandSeed);

        public AccuracyRoll ReflectionAccuracy()
        {
            var d100 = Rand.Range(1, 100);
            GetReflectionAccuracyCalculator(fixedRandSeed: false).Calculate(out var modifier, out var difficulty, out var skill);
            if (skill != null)
            {
                // TODO: This means the skill is leveled regardless of reflection success - is this correct?
                ReflectionSkillGain(skill);
            }

            var subtotal = d100 + modifier;
            if (subtotal >= 90)
                return AccuracyRoll.CriticalSuccess;
            if (subtotal > difficulty)
                return AccuracyRoll.Success;
            if (subtotal <= 30)
                return AccuracyRoll.CritialFailure;
            return AccuracyRoll.Failure;
        }

        public virtual void ReflectionAccuracy_InFix(ref int modifier, ref int difficulty)
        {
            //Placeholder
        }

        public virtual bool TrySpecialMeleeBlock()
        {
            return false;
        }

        public virtual float DeflectionChance_InFix(float calc)
        {
            return calc;
        }

        public virtual Verb ReflectionHandler(Verb newVerb)
        {
            if (Props.canReflect)
            {
                lastAccuracyRoll = ReflectionAccuracy();
                var deflectVerbX = newVerb;

                //Initialize VerbProperties
                var newVerbProps = new VerbProperties
                {
                    //Copy values over to a new verb props
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0,
                    defaultCooldownTime = 0,
                    soundCast = Props.deflectSound
                };
                switch (lastAccuracyRoll)
                {
                    case AccuracyRoll.CriticalSuccess:
                        if (GetPawn is Pawn pawn)
                            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map,
                                "SWSaber_TextMote_CriticalSuccess".Translate(), 6f);
                        newVerbProps.accuracyLong = 999.0f;
                        newVerbProps.accuracyMedium = 999.0f;
                        newVerbProps.accuracyShort = 999.0f;
                        lastShotReflected = true;
                        break;
                    case AccuracyRoll.Failure:
                        newVerbProps.forcedMissRadius = 50.0f;
                        newVerbProps.accuracyLong = 0.0f;
                        newVerbProps.accuracyMedium = 0.0f;
                        newVerbProps.accuracyShort = 0.0f;
                        lastShotReflected = false;
                        break;

                    case AccuracyRoll.CritialFailure:
                        if (GetPawn is Pawn pawn2)
                            MoteMaker.ThrowText(pawn2.DrawPos, pawn2.Map,
                                "SWSaber_TextMote_CriticalFailure".Translate(), 6f);
                        newVerbProps.accuracyLong = 999.0f;
                        newVerbProps.accuracyMedium = 999.0f;
                        newVerbProps.accuracyShort = 999.0f;
                        lastShotReflected = true;
                        break;
                    case AccuracyRoll.Success:
                        newVerbProps.accuracyLong = 999.0f;
                        newVerbProps.accuracyMedium = 999.0f;
                        newVerbProps.accuracyShort = 999.0f;
                        lastShotReflected = true;
                        break;
                }
                //Apply values
                deflectVerbX.verbProps = newVerbProps;
                return deflectVerbX;
            }
            return newVerb;
        }

        // TODO: This is never called - still needed?
        public virtual Verb CopyAndReturnNewVerb_PostFix(Verb newVerb)
        {
            return newVerb;
        }

        public Verb CopyAndReturnNewVerb(Verb newVerb = null)
        {
            if (newVerb != null)
            {
                deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                deflectVerb.caster = GetPawn;

                //Initialize VerbProperties
                var newVerbProps = new VerbProperties
                {
                    //Copy values over to a new verb props
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0,
                    defaultCooldownTime = 0,
                    soundCast = Props.deflectSound
                };

                //Apply values
                deflectVerb.verbProps = newVerbProps;
            }
            else
            {
                if (deflectVerb != null) return deflectVerb;
                deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                deflectVerb.caster = GetPawn;
                deflectVerb.verbProps = Props.DeflectVerb;
            }
            return deflectVerb;
        }

        public void ResolveDeflectVerb()
        {
            CopyAndReturnNewVerb(null);
        }

        public virtual Pawn ResolveDeflectionTarget(Pawn defaultTarget = null)
        {
            if (lastAccuracyRoll != AccuracyRoll.CritialFailure) return defaultTarget;
            var thisPawn = GetPawn;
            if (thisPawn == null || thisPawn.Dead) return defaultTarget;

            bool Validator(Thing t)
            {
                return t is Pawn pawn3 && pawn3 != thisPawn;
            }

            var closestPawn = (Pawn)GenClosest.ClosestThingReachable(thisPawn.Position, thisPawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell,
                TraverseParms.For(thisPawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, Validator, null,
                0, -1, false, RegionType.Set_Passable, false);
            if (closestPawn == null) return defaultTarget;
            return closestPawn == defaultTarget ? thisPawn : closestPawn;
        }

        public virtual void CriticalFailureHandler(DamageInfo dinfo, Pawn newTarget, out bool shouldContinue)
        {
            shouldContinue = true;
            if (lastAccuracyRoll != AccuracyRoll.CritialFailure) return;
            var thisPawn = GetPawn;
            if (thisPawn == null || thisPawn.Dead) return;
            //If the target isn't the old target, then get out of this
            if (newTarget != dinfo.Instigator as Pawn)
                return;
            shouldContinue = false;
            thisPawn.TakeDamage(new DamageInfo(dinfo.Def, dinfo.Amount));
        }

        public virtual void GiveDeflectJob(DamageInfo dinfo)
        {
            try
            {
                if (!(dinfo.Instigator is Pawn pawn)) return;
                var job = JobMaker.MakeJob(CompDeflectorDefOf.CastDeflectVerb);
                job.playerForced = true;
                job.locomotionUrgency = LocomotionUrgency.Sprint;
                var compEquipVerb = pawn.equipment?.PrimaryEq?.PrimaryVerb;
                if (compEquipVerb == null) return;
                var thisPawn = GetPawn;
                var verbToUse = (Verb_Deflected)CopyAndReturnNewVerb(compEquipVerb);
                verbToUse = (Verb_Deflected)ReflectionHandler(deflectVerb);
                verbToUse.lastShotReflected = lastShotReflected;
                verbToUse.verbTracker = thisPawn.VerbTracker;
                pawn = ResolveDeflectionTarget(pawn);
                CriticalFailureHandler(dinfo, pawn, out var shouldContinue);
                if (!shouldContinue) return;
                job.targetA = pawn;
                job.verbToUse = verbToUse;
                job.killIncappedTarget = pawn.Downed;
                thisPawn.jobs.TryTakeOrderedJob(job);
            }
            catch (NullReferenceException e) // TODO: Is this still needed?
            {
                Log.Message(e.ToString());
            }
            ////Log.Message("TryToTakeOrderedJob Called");
        }

        /// <summary>
        ///     This does the math for determining if shots are deflected.
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="absorbed"></param>
        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            if (dinfo.Weapon != null)
                if (!dinfo.Weapon.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                {
                    if (HasCompActivatableEffect)
                    {
                        if (CompActivatableEffectiveIsActive == false)
                        {
                            //Log.Message("Inactivate Weapon");
                            absorbed = false;
                            return;
                        }
                    }
                    var calc = DeflectionChance;
                    var deflectThreshold = (int)(calc * 100); // 0.3f => 30
                    if (Rand.Range(1, 100) > deflectThreshold)
                    {
                        absorbed = false;
                        lastShotReflected = false;
                        return;
                    }

                    ResolveDeflectVerb();
                    GiveDeflectJob(dinfo);
                    absorbed = true;
                    return;
                }
            absorbed = false;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref animationDeflectionTicks, "animationDeflectionTicks", 0);
            base.PostExposeData();
        }
    }
}
