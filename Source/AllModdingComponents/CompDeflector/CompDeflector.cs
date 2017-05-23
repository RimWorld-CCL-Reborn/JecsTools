using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompDeflector
{
    public class CompDeflector : ThingComp
    {
        private int animationDeflectionTicks = 0;
        public int AnimationDeflectionTicks
        {
            set
            {
                animationDeflectionTicks = value;
            }
            get
            {
                return animationDeflectionTicks;
            }
        }
        public bool IsAnimatingNow
        {
            get
            {
                if (animationDeflectionTicks >= 0) return true;
                return false;
            }
        }

        public CompEquippable GetEquippable
        {
            get
            {
                return this.parent.GetComp<CompEquippable>();
            }
        }

        public Pawn GetPawn
        {
            get
            {
                return GetEquippable.verbTracker.PrimaryVerb.CasterPawn;
            }
        }

        public ThingComp GetActivatableEffect
        {
            get
            {
                return this.parent.AllComps.FirstOrDefault<ThingComp>((ThingComp y) => y.GetType().ToString().Contains("ActivatableEffect"));
            }
        }

        public bool HasCompActivatableEffect
        {
            get
            {
                ThingWithComps x = this.parent as ThingWithComps;
                if (x != null)
                {
                    if (GetActivatableEffect != null)
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
            Pawn thisPawn = GetPawn;
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

        public virtual bool TrySpecialMeleeBlock()
        {
            return false;
        }

        public float DeflectionChance
        {
            get
            {
                float calc = Props.baseDeflectChance;
                
                if (GetEquippable != null)
                {
                    if (GetPawn != null)
                    {
                        Pawn pawn = GetPawn;

                        //This handles if a deflection skill is defined.
                        //Example, melee skill of 20.
                        if (Props.useSkillInCalc)
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
                                            calc += ((float)skillRecord.Level) * param; //Makes the skill into float percent
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
                        if (Props.useManipulationInCalc)
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

        public virtual float DeflectionChance_InFix(float calc)
        {
            return calc;
        }

        public string ChanceToString
        {
            get
            {
                return DeflectionChance.ToStringPercent();
            }
        }

        public IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            //yield return new StatDrawEntry(StatCategoryDefOf.Basics, "DeflectChance".Translate(), ChanceToString, 0);
            yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Deflect chance", ChanceToString, 0);
            
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
            if (Props.canReflect)
            {
                lastAccuracyRoll = ReflectionAccuracy();
                Verb deflectVerb = newVerb;

                //Initialize VerbProperties
                VerbProperties newVerbProps = new VerbProperties();

                //Copy values over to a new verb props
                newVerbProps.hasStandardCommand = newVerb.verbProps.hasStandardCommand;
                newVerbProps.projectileDef = newVerb.verbProps.projectileDef;
                newVerbProps.range = newVerb.verbProps.range;
                newVerbProps.muzzleFlashScale = newVerb.verbProps.muzzleFlashScale;
                newVerbProps.warmupTime = 0;
                newVerbProps.defaultCooldownTime = 0;
                newVerbProps.soundCast = this.Props.deflectSound;

                switch (lastAccuracyRoll)
                {
                    case AccuracyRoll.CriticalSuccess:
                        if (GetPawn != null)
                        {
                            MoteMaker.ThrowText(GetPawn.DrawPos, GetPawn.Map, "SWSaber_TextMote_CriticalSuccess".Translate(), 6f);
                        }
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
                        {
                            MoteMaker.ThrowText(GetPawn.DrawPos, GetPawn.Map, "SWSaber_TextMote_CriticalFailure".Translate(), 6f);
                        }
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
                deflectVerb.verbProps = newVerbProps;
                return deflectVerb;
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
                deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                deflectVerb.caster = GetPawn;

                //Initialize VerbProperties
                VerbProperties newVerbProps = new VerbProperties();

                //Copy values over to a new verb props
                newVerbProps.hasStandardCommand = newVerb.verbProps.hasStandardCommand;
                newVerbProps.projectileDef = newVerb.verbProps.projectileDef;
                newVerbProps.range = newVerb.verbProps.range;
                newVerbProps.muzzleFlashScale = newVerb.verbProps.muzzleFlashScale;
                newVerbProps.warmupTime = 0;
                newVerbProps.defaultCooldownTime = 0;
                newVerbProps.soundCast = this.Props.deflectSound;

                //Apply values
                deflectVerb.verbProps = newVerbProps;
            }
            else
            {
                if (deflectVerb == null)
                {
                    deflectVerb = (Verb_Deflected)Activator.CreateInstance(typeof(Verb_Deflected));
                    deflectVerb.caster = GetPawn;
                    deflectVerb.verbProps = this.Props.DeflectVerb;
                }
            }
            return deflectVerb;
        }

        public void ResolveDeflectVerb()
        {
            CopyAndReturnNewVerb(null);
        }

        public virtual Pawn ResolveDeflectionTarget(Pawn defaultTarget = null)
        {
            if (lastAccuracyRoll == AccuracyRoll.CritialFailure)
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
            if (lastAccuracyRoll == AccuracyRoll.CritialFailure)
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
                    GetPawn.TakeDamage(new DamageInfo(dinfo.Def, dinfo.Amount));
                }
            }
        }

        public virtual void GiveDeflectJob(DamageInfo dinfo)
        {
            try
            {

                Pawn pawn2 = dinfo.Instigator as Pawn;

                if (pawn2 != null)
                {
                    Job job = new Job(CompDeflectorDefOf.CastDeflectVerb);
                    job.playerForced = true;
                    job.locomotionUrgency = LocomotionUrgency.Sprint;
                    if (pawn2.equipment != null)
                    {
                        CompEquippable compEquip = pawn2.equipment.PrimaryEq;
                        if (compEquip != null)
                        {
                            if (compEquip.PrimaryVerb != null)
                            {
                                Verb_Deflected verbToUse = (Verb_Deflected)CopyAndReturnNewVerb(compEquip.PrimaryVerb);
                                verbToUse = (Verb_Deflected)ReflectionHandler(deflectVerb);
                                verbToUse.lastShotReflected = this.lastShotReflected;
                                pawn2 = ResolveDeflectionTarget(pawn2);
                                bool shouldContinue = false;
                                CriticalFailureHandler(dinfo, pawn2, out shouldContinue);
                                if (shouldContinue)
                                {
                                    job.targetA = pawn2;
                                    job.verbToUse = verbToUse;
                                    job.killIncappedTarget = pawn2.Downed;
                                    GetPawn.jobs.TryTakeOrderedJob(job);
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
            if (dinfo.WeaponGear != null)
            {
                if (!dinfo.WeaponGear.IsMeleeWeapon && dinfo.WeaponBodyPartGroup == null)
                {

                    if (HasCompActivatableEffect)
                    {
                        bool? isActive = (bool)AccessTools.Method(GetActivatableEffect.GetType(), "IsActive").Invoke(GetActivatableEffect, null);
                        if (isActive == false)
                        {
                            ////Log.Message("Inactivate Weapon");
                            absorbed = false;
                            return;
                        }
                    }
                    float calc = DeflectionChance;
                    int deflectThreshold = (int)(calc * 100); // 0.3f => 30
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
            }
            absorbed = false;
            return;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<int>(ref this.animationDeflectionTicks, "animationDeflectionTicks", 0);
            base.PostExposeData();
        }


        public CompProperties_Deflector Props
        {
            get
            {
                return (CompProperties_Deflector)this.props;
            }
        }
    }
}
