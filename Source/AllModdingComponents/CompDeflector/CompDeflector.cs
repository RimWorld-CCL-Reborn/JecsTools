using System;
using System.Collections.Generic;
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

        private int animationDeflectionTicks;


        public Verb_Deflected deflectVerb;
        public AccuracyRoll lastAccuracyRoll = AccuracyRoll.Failure;
        public bool lastShotReflected;

        public int AnimationDeflectionTicks
        {
            set => animationDeflectionTicks = value;
            get => animationDeflectionTicks;
        }

        public bool IsAnimatingNow
        {
            get
            {
                if (animationDeflectionTicks >= 0) return true;
                return false;
            }
        }

        public CompEquippable GetEquippable => parent.GetComp<CompEquippable>();

        public Pawn GetPawn => GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        public ThingComp GetActivatableEffect =>
            parent.AllComps.FirstOrDefault(y => y.GetType().ToString().Contains("ActivatableEffect"));

        public bool HasCompActivatableEffect
        {
            get
            {
                if (parent is ThingWithComps x)
                    if (GetActivatableEffect != null)
                        return true;
                return false;
            }
        }

        public float DeflectionChance
        {
            get
            {
                var calc = Props.baseDeflectChance;

                if (GetEquippable != null)
                    if (GetPawn != null)
                    {
                        var pawn = GetPawn;

                        //This handles if a deflection skill is defined.
                        //Example, melee skill of 20.
                        if (Props.useSkillInCalc)
                        {
                            var skillToCheck = Props.deflectSkill;
                            if (skillToCheck != null)
                            {
                                var skillRecord = pawn.skills?.GetSkill(skillToCheck);
                                if (skillRecord != null)
                                {
                                    var param = Props.deflectRatePerSkillPoint;
                                    if (param != 0)
                                        calc += skillRecord.Level * param; //Makes the skill into float percent
                                    else
                                        Log.Error(
                                            "CompDeflector :: deflectRatePerSkillPoint is set to 0, but useSkillInCalc is set to true.");
                                }
                            }
                        }

                        calc = DeflectionChance_InFix(calc);

                        //This handles if manipulation needs to be checked.
                        if (!Props.useManipulationInCalc) return Mathf.Clamp(calc, 0, 1.0f);
                        if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            calc *= pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
                        else
                            calc = 0f;
                    }
                return Mathf.Clamp(calc, 0, 1.0f);
            }
        }

        public string ChanceToString => DeflectionChance.ToStringPercent();


        public CompProperties_Deflector Props => (CompProperties_Deflector) props;

        public void DeflectionSkillGain(SkillRecord skill)
        {
            GetPawn.skills?.Learn(Props.deflectSkill, Props.deflectSkillLearnRate, false);
        }


        public void ReflectionSkillGain(SkillRecord skill)
        {
            GetPawn.skills?.Learn(Props.reflectSkill, Props.reflectSkillLearnRate, false);
        }

        //Accuracy Roll Calculator
        public AccuracyRoll ReflectionAccuracy()
        {
            var d100 = Rand.Range(1, 100);
            var modifier = 0;
            var difficulty = 80;
            var thisPawn = GetPawn;
            if (thisPawn?.skills != null)
            {
                if (Props?.reflectSkill != null)
                {
                    var skill = thisPawn.skills.GetSkill(Props.reflectSkill);
                    if (skill?.Level > 0)
                    {
                        modifier += (int) (Props.deflectRatePerSkillPoint * skill.Level);
                        //Log.Message("Deflection mod: " + modifier.ToString());
                        ReflectionSkillGain(skill);
                    }
                }
            }
            ReflectionAccuracy_InFix(ref modifier, ref difficulty);

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

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            //yield return new StatDrawEntry(StatCategoryDefOf.Basics, "DeflectChance".Translate(), ChanceToString, 0);
            yield return new StatDrawEntry(StatCategoryDefOf.Basics, StatDef.Named("Deflect chance"), float.Parse(ChanceToString), StatRequest.ForEmpty());
        }

        //        	if (this.ingestible != null)
        //	{
        //		IEnumerator<StatDrawEntry> enumerator2 = this.ingestible.SpecialDisplayStats(this).GetEnumerator();
        //		while (enumerator2.MoveNext())
        //		{
        //			StatDrawEntry current2 = enumerator2.Current;
        //        yield return current2;
        //		}
        //}

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
                        if (GetPawn != null)
                            MoteMaker.ThrowText(GetPawn.DrawPos, GetPawn.Map,
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
                        if (GetPawn != null)
                            MoteMaker.ThrowText(GetPawn.DrawPos, GetPawn.Map,
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

        public virtual Verb CopyAndReturnNewVerb_PostFix(Verb newVerb)
        {
            return newVerb;
        }

        public Verb CopyAndReturnNewVerb(Verb newVerb = null)
        {
            if (newVerb != null)
            {
                deflectVerb = null;
                deflectVerb = (Verb_Deflected) Activator.CreateInstance(typeof(Verb_Deflected));
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
                deflectVerb = (Verb_Deflected) Activator.CreateInstance(typeof(Verb_Deflected));
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

            var closestPawn = (Pawn) GenClosest.ClosestThingReachable(thisPawn.Position, thisPawn.Map,
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
            GetPawn.TakeDamage(new DamageInfo(dinfo.Def, dinfo.Amount));
        }

        public virtual void GiveDeflectJob(DamageInfo dinfo)
        {
            try
            {
                if (!(dinfo.Instigator is Pawn pawn2)) return;
                var job = new Job(CompDeflectorDefOf.CastDeflectVerb)
                {
                    playerForced = true,
                    locomotionUrgency = LocomotionUrgency.Sprint
                };
                var compEquip = pawn2.equipment?.PrimaryEq;
                if (compEquip?.PrimaryVerb == null) return;
                var verbToUse = (Verb_Deflected) CopyAndReturnNewVerb(compEquip.PrimaryVerb);
                verbToUse = (Verb_Deflected) ReflectionHandler(deflectVerb);
                verbToUse.lastShotReflected = lastShotReflected;
                verbToUse.verbTracker = GetPawn.VerbTracker;
                pawn2 = ResolveDeflectionTarget(pawn2);
                CriticalFailureHandler(dinfo, pawn2, out var shouldContinue);
                if (!shouldContinue) return;
                job.targetA = pawn2;
                job.verbToUse = verbToUse;
                job.killIncappedTarget = pawn2.Downed;
                GetPawn.jobs.TryTakeOrderedJob(job);
            }
            catch (NullReferenceException)
            {
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
                        bool? isActive = (bool) AccessTools.Method(GetActivatableEffect.GetType(), "IsActive")
                            .Invoke(GetActivatableEffect, null);
                        if (isActive == false)
                        {
                            ////Log.Message("Inactivate Weapon");
                            absorbed = false;
                            return;
                        }
                    }
                    var calc = DeflectionChance;
                    var deflectThreshold = (int) (calc * 100); // 0.3f => 30
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