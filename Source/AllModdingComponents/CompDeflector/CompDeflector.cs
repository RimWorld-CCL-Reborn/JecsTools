using Harmony;
using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace CompDeflector
{
    public class CompDeflector : ThingComp
    {
        private int animationDeflectionTicks = 0;
        public int AnimationDeflectionTicks
        {
            set => this.animationDeflectionTicks = value;
            get => this.animationDeflectionTicks;
        }
        public bool IsAnimatingNow
        {
            get
            {
                if (this.animationDeflectionTicks >= 0) return true;
                return false;
            }
        }

        public CompEquippable GetEquippable => this.parent.GetComp<CompEquippable>();

        public Pawn GetPawn => this.GetEquippable.verbTracker.PrimaryVerb.CasterPawn;

        public ThingComp GetActivatableEffect => this.parent.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString().Contains("ActivatableEffect"));

        public bool HasCompActivatableEffect
        {
            get
            {
                if (this.parent is ThingWithComps x)
                {
                    if (this.GetActivatableEffect != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
            

        public Verb_Deflected deflectVerb;
        public bool lastShotReflected = false;

        public enum AccuracyRoll
        {
            CritialFailure,
            Failure,
            Success,
            CriticalSuccess
        }
        public AccuracyRoll lastAccuracyRoll = AccuracyRoll.Failure;

        public void DeflectionSkillGain(SkillRecord skill)
        {
            if (this.GetPawn.skills != null)
            {
                this.GetPawn.skills.Learn(this.Props.deflectSkill, this.Props.deflectSkillLearnRate, false);
            }
        }


        public void ReflectionSkillGain(SkillRecord skill)
        {
            if (this.GetPawn.skills != null)
            {
                this.GetPawn.skills.Learn(this.Props.reflectSkill, this.Props.reflectSkillLearnRate, false);
            }
        }

        //Accuracy Roll Calculator
        public AccuracyRoll ReflectionAccuracy()
        {
            int d100 = Rand.Range(1, 100);
            int modifier = 0;
            int difficulty = 80;
            Pawn thisPawn = this.GetPawn;
            if (thisPawn != null)
            {
                if (thisPawn.skills != null)
                {
                    if (this.Props != null)
                    {
                        if (this.Props.reflectSkill != null)
                        {
                            SkillRecord skill = thisPawn.skills.GetSkill(this.Props.reflectSkill);
                            if (skill != null)
                            {
                                if (skill.Level > 0)
                                {
                                    modifier += (int)((this.Props.deflectRatePerSkillPoint) * skill.Level);
                                    //Log.Message("Deflection mod: " + modifier.ToString());
                                    ReflectionSkillGain(skill);
                                }
                            }
                        }
                    }
                }
            }
            ReflectionAccuracy_InFix(ref modifier, ref difficulty);

            int subtotal = d100 + modifier;
            if (subtotal >= 90)
            {
                return AccuracyRoll.CriticalSuccess;
            }
            else if (subtotal > difficulty)
            {
                return AccuracyRoll.Success;
            }
            else if (subtotal <= 30)
            {
                return AccuracyRoll.CritialFailure;
            }
            return AccuracyRoll.Failure;
        }

        public virtual void ReflectionAccuracy_InFix(ref int modifier, ref int difficulty)
        {
            //Placeholder
        }

        public virtual bool TrySpecialMeleeBlock() => false;

        public float DeflectionChance
        {
            get
            {
                float calc = this.Props.baseDeflectChance;
                
                if (this.GetEquippable != null)
                {
                    if (this.GetPawn != null)
                    {
                        Pawn pawn = this.GetPawn;

                        //This handles if a deflection skill is defined.
                        //Example, melee skill of 20.
                        if (this.Props.useSkillInCalc)
                        {
                            SkillDef skillToCheck = this.Props.deflectSkill;
                            if (skillToCheck != null)
                            {
                                if (pawn.skills != null)
                                {
                                    SkillRecord skillRecord = pawn.skills.GetSkill(skillToCheck);
                                    if (skillRecord != null)
                                    {
                                        float param = this.Props.deflectRatePerSkillPoint;
                                        if (param != 0)
                                        {
                                            calc += skillRecord.Level * param; //Makes the skill into float percent
                                                                                        //Ex: Melee skill of 20. Multiplied by 0.015f. Equals 0.3f, or 30%
                                        }
                                        else
                                        {
                                            Log.Error("CompDeflector :: deflectRatePerSkillPoint is set to 0, but useSkillInCalc is set to true.");
                                        }

                                    }
                                }

                            }
                        }

                        calc = DeflectionChance_InFix(calc);

                        //This handles if manipulation needs to be checked.
                        if (this.Props.useManipulationInCalc)
                        {
                            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                            {
                                calc *= pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation);
                            }
                            else
                            {
                                calc = 0f;
                            }
                        }
                    }
                }
                return Mathf.Clamp(calc, 0, 1.0f);
            }
        }

        public virtual float DeflectionChance_InFix(float calc) => calc;

        public string ChanceToString => this.DeflectionChance.ToStringPercent();

        public IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            //yield return new StatDrawEntry(StatCategoryDefOf.Basics, "DeflectChance".Translate(), ChanceToString, 0);
            yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Deflect chance", this.ChanceToString, 0);
            
            yield break;
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
            if (this.Props.canReflect)
            {
                this.lastAccuracyRoll = ReflectionAccuracy();
                Verb deflectVerb = newVerb;

                //Initialize VerbProperties
                VerbProperties newVerbProps = new VerbProperties()
                {

                    //Copy values over to a new verb props
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0,
                    defaultCooldownTime = 0,
                    soundCast = this.Props.deflectSound
                };
                switch (this.lastAccuracyRoll)
                {
                    case AccuracyRoll.CriticalSuccess:
                        if (this.GetPawn != null)
                        {
                            MoteMaker.ThrowText(this.GetPawn.DrawPos, this.GetPawn.Map, "SWSaber_TextMote_CriticalSuccess".Translate(), 6f);
                        }
                        newVerbProps.accuracyLong = 999.0f;
                        newVerbProps.accuracyMedium = 999.0f;
                        newVerbProps.accuracyShort = 999.0f;
                        this.lastShotReflected = true;
                        break;
                    case AccuracyRoll.Failure:
                        newVerbProps.forcedMissRadius = 50.0f;
                        newVerbProps.accuracyLong = 0.0f;
                        newVerbProps.accuracyMedium = 0.0f;
                        newVerbProps.accuracyShort = 0.0f;
                        this.lastShotReflected = false;
                        break;

                    case AccuracyRoll.CritialFailure:
                        if (this.GetPawn != null)
                        {
                            MoteMaker.ThrowText(this.GetPawn.DrawPos, this.GetPawn.Map, "SWSaber_TextMote_CriticalFailure".Translate(), 6f);
                        }
                        newVerbProps.accuracyLong = 999.0f;
                        newVerbProps.accuracyMedium = 999.0f;
                        newVerbProps.accuracyShort = 999.0f;
                        this.lastShotReflected = true;
                        break;
                    case AccuracyRoll.Success:
                        newVerbProps.accuracyLong = 999.0f;
                        newVerbProps.accuracyMedium = 999.0f;
                        newVerbProps.accuracyShort = 999.0f;
                        this.lastShotReflected = true;
                        break;
                }
                //Apply values
                deflectVerb.verbProps = newVerbProps;
                return deflectVerb;
            }
            return newVerb;

        }

        public virtual Verb CopyAndReturnNewVerb_PostFix(Verb newVerb) => newVerb;

        public Verb CopyAndReturnNewVerb(Verb newVerb = null)
        {
            if (newVerb != null)
            {
                this.deflectVerb = null;
                this.deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                this.deflectVerb.caster = this.GetPawn;

                //Initialize VerbProperties
                VerbProperties newVerbProps = new VerbProperties()
                {

                    //Copy values over to a new verb props
                    hasStandardCommand = newVerb.verbProps.hasStandardCommand,
                    defaultProjectile = newVerb.verbProps.defaultProjectile,
                    range = newVerb.verbProps.range,
                    muzzleFlashScale = newVerb.verbProps.muzzleFlashScale,
                    warmupTime = 0,
                    defaultCooldownTime = 0,
                    soundCast = this.Props.deflectSound
                };

                //Apply values
                this.deflectVerb.verbProps = newVerbProps;
            }
            else
            {
                if (this.deflectVerb == null)
                {
                    this.deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                    this.deflectVerb.caster = this.GetPawn;
                    this.deflectVerb.verbProps = this.Props.DeflectVerb;
                }
            }
            return this.deflectVerb;
        }

        public void ResolveDeflectVerb() => CopyAndReturnNewVerb(null);

        public virtual Pawn ResolveDeflectionTarget(Pawn defaultTarget = null)
        {
            if (this.lastAccuracyRoll == AccuracyRoll.CritialFailure)
            {
                Pawn thisPawn = this.GetPawn;
                if (thisPawn != null && !thisPawn.Dead)
                {
                    Predicate<Thing> validator = delegate (Thing t)
                    {
                        Pawn pawn3 = t as Pawn;
                        return (pawn3 != null && pawn3 != thisPawn);
                    };
                    Pawn closestPawn = (Pawn)GenClosest.ClosestThingReachable(thisPawn.Position, thisPawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.InteractionCell, TraverseParms.For(thisPawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null, 0, -1, false, RegionType.Set_Passable, false);
                    if (closestPawn != null)
                    {
                        if (closestPawn == defaultTarget) return thisPawn;
                        return closestPawn;   
                    }
                }
            }
            return defaultTarget;
        }

        public virtual void CriticalFailureHandler(DamageInfo dinfo, Pawn newTarget, out bool shouldContinue)
        {
            shouldContinue = true;
            if (this.lastAccuracyRoll == AccuracyRoll.CritialFailure)
            {
                Pawn thisPawn = this.GetPawn;
                if (thisPawn != null && !thisPawn.Dead)
                {
                    //If the target isn't the old target, then get out of this
                    if (newTarget != dinfo.Instigator as Pawn)
                    {
                        return;
                    }
                    shouldContinue = false;
                    this.GetPawn.TakeDamage(new DamageInfo(dinfo.Def, dinfo.Amount));
                }
            }
        }

        public virtual void GiveDeflectJob(DamageInfo dinfo)
        {
            try
            {


                if (dinfo.Instigator is Pawn pawn2)
                {
                    Job job = new Job(CompDeflectorDefOf.CastDeflectVerb)
                    {
                        playerForced = true,
                        locomotionUrgency = LocomotionUrgency.Sprint
                    };
                    if (pawn2.equipment != null)
                    {
                        CompEquippable compEquip = pawn2.equipment.PrimaryEq;
                        if (compEquip != null)
                        {
                            if (compEquip.PrimaryVerb != null)
                            {
                                Verb_Deflected verbToUse = (Verb_Deflected)CopyAndReturnNewVerb(compEquip.PrimaryVerb);
                                verbToUse = (Verb_Deflected)ReflectionHandler(this.deflectVerb);
                                verbToUse.lastShotReflected = this.lastShotReflected;
                                pawn2 = ResolveDeflectionTarget(pawn2);
                                CriticalFailureHandler(dinfo, pawn2, out bool shouldContinue);
                                if (shouldContinue)
                                {
                                    job.targetA = pawn2;
                                    job.verbToUse = verbToUse;
                                    job.killIncappedTarget = pawn2.Downed;
                                    this.GetPawn.jobs.TryTakeOrderedJob(job);
                                }
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException) { }
            ////Log.Message("TryToTakeOrderedJob Called");
        }
        
        /// <summary>
        /// This does the math for determining if shots are deflected.
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="absorbed"></param>
        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            if (dinfo.Weapon != null)
            {
                if (!dinfo.Weapon.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                {

                    if (this.HasCompActivatableEffect)
                    {
                        bool? isActive = (bool)AccessTools.Method(this.GetActivatableEffect.GetType(), "IsActive").Invoke(this.GetActivatableEffect, null);
                        if (isActive == false)
                        {
                            ////Log.Message("Inactivate Weapon");
                            absorbed = false;
                            return;
                        }
                    }
                    float calc = this.DeflectionChance;
                    int deflectThreshold = (int)(calc * 100); // 0.3f => 30
                    if (Rand.Range(1, 100) > deflectThreshold)
                    {
                        absorbed = false;
                        this.lastShotReflected = false;
                        return;
                    }

                    ResolveDeflectVerb();
                    GiveDeflectJob(dinfo);
                    absorbed = true;
                    return;
                }
            }
            absorbed = false;
            return;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<int>(ref this.animationDeflectionTicks, "animationDeflectionTicks", 0);
            base.PostExposeData();
        }


        public CompProperties_Deflector Props => (CompProperties_Deflector)this.props;
    }
}
