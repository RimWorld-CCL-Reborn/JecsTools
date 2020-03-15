using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    //public class PawnAbility : ThingWithComps
    public class PawnAbility : IExposable, IEquatable<PawnAbility>
    {
        public CompAbilityUser abilityUser; //public for resolving saving / loading issues between versions
        private Pawn pawn;
        private Texture2D powerButton;
        private AbilityDef powerdef;
        private int TicksUntilCasting = -1;
        private Verb_UseAbility verb;

        public PawnAbility()
        {
            //Log.Message("PawnAbility Created: "  + this.Def.defName);
        }

        public PawnAbility(CompAbilityUser comp)
        {
            pawn = comp.AbilityUser;
            abilityUser = comp;
            //Log.Message("PawnAbility Created: " + this.Def.defName);
        }

        public PawnAbility(AbilityData data)
        {
            pawn = data.Pawn;
            abilityUser = data.Pawn.AllComps.FirstOrDefault(x => x.GetType() == data.AbilityClass) as CompAbilityUser;
            //Log.Message("PawnAbility Created: " + this.Def.defName);
        }

        public PawnAbility(Pawn user, AbilityDef pdef)
        {
            pawn = user;
            powerdef = pdef;
            powerButton = pdef.uiIcon;
            //Log.Message("PawnAbility Created: " + this.Def.defName);
        }

        public virtual CompAbilityUser AbilityUser
        {
            get
            {
                if (abilityUser == null)
                    abilityUser = (CompAbilityUser) pawn.AllComps.FirstOrDefault(x => x is CompAbilityUser);
                return abilityUser;
            }
        }

        public Pawn Pawn
        {
            get => pawn;
            set => pawn = value;
        }

        public AbilityDef Def
        {
            get => powerdef;
            set => powerdef = value;
        }

        public List<ThingComp> Comps { get; } = new List<ThingComp>();

        public Texture2D PowerButton
        {
            get
            {
                if (powerButton == null)
                    powerButton = powerdef.uiIcon;
                return powerButton;
            }
        }

        public int CooldownTicksLeft
        {
            get => TicksUntilCasting;
            set => TicksUntilCasting = value;
        } //Log.Message(value.ToString()); } }

        public virtual void Notify_AbilityFailed(bool refund)
        {
            AbilityUser.AbilityUser.jobs.StopAll();
            if (refund)
                CooldownTicksLeft = -1;
        }

        public int MaxCastingTicks => (int) (powerdef.MainVerb.SecondsToRecharge * GenTicks.TicksPerRealSecond);

        public Verb_UseAbility Verb
        {
            get
            {
                if (verb == null)
                {
                    var newVerb = (Verb_UseAbility) Activator.CreateInstance(powerdef.MainVerb.verbClass);
                    newVerb.caster = AbilityUser.AbilityUser;
                    newVerb.Ability = this;
                    newVerb.verbProps = powerdef.MainVerb;
                    verb = newVerb;
                }
                return verb;
            }
        }


        //Added on 12/3/17 to prevent hash errors.

        public bool Equals(PawnAbility other)
        {
            if (other.GetUniqueLoadID() == GetUniqueLoadID()) return true;
            return false;
        }

        public void ExposeData()
        {
            //base.ExposeData();
            Scribe_Values.Look(ref TicksUntilCasting, "pawnAbilityTicksUntilcasting", -1);
            Scribe_References.Look(ref pawn, "pawnAbilityPawn");
            Scribe_Defs.Look(ref powerdef, "pawnAbilityPowerDef");
        }

        public void Tick()
        {
            if (powerdef?.PassiveProps?.Worker is PassiveEffectWorker w)
                w.Tick(abilityUser);
            if (Verb != null)
                Verb.VerbTick();
            if (CooldownTicksLeft > -1 && !Find.TickManager.Paused)
                CooldownTicksLeft--;
        }

        public virtual bool ShouldShowGizmo()
        {
            return true;
        }

        public virtual bool CanOverpowerTarget(AbilityContext context, LocalTargetInfo target, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual Job UseAbility(AbilityContext context, LocalTargetInfo target)
        {
            var reason = "";
            if (target.Thing != null && !CanOverpowerTarget(context, target, out reason))
                return null;
            var job = GetJob(context, target);
            if (context == AbilityContext.Player)
                pawn.jobs.TryTakeOrderedJob(job);
            return job;
        }

        public virtual Job GetJob(AbilityContext context, LocalTargetInfo target)
        {
            Job job;
            job = powerdef.GetJob(verb.UseAbilityProps.AbilityTargetCategory, target);
            job.playerForced = true;
            job.verbToUse = verb;
            job.count = context == AbilityContext.Player ? 1 : 0; //Count 1 for Player : 0 for AI
            if (target != null)
                if (target.Thing is Pawn pawn2)
                    job.killIncappedTarget = pawn2.Downed;
            return job;
        }

        public bool TryCastAbility(AbilityContext context, LocalTargetInfo target)
        {
            //Can our body cast?
            var reason = "";
            if (!CanCastPowerCheck(context, out reason))
                return false;
           
            //If we're a player, let's target.
            if (context == AbilityContext.Player)
            {
                var targeter = Find.Targeter;
                if (verb.CasterIsPawn && targeter.targetingSource != null )
                    // Tad : Commented out for now. 
                    // && targeter.targetingSource.targetParams .verbProps == verb.verbProps)
                {
                    var casterPawn = verb.CasterPawn;
                    if (!targeter.IsPawnTargeting(casterPawn))
                        targeter.targetingSourceAdditionalPawns.Add(casterPawn);
                }
                UseAbility(AbilityContext.Player, target);
            }
            else
            {
                UseAbility(AbilityContext.AI, target);
            }
            return true;
        }

        public Command_PawnAbility GetGizmo()
        {
            var command_CastPower = new Command_PawnAbility(abilityUser, this, CooldownTicksLeft)
            {
                verb = Verb,
                defaultLabel = powerdef.LabelCap
            };

            command_CastPower.curTicks = CooldownTicksLeft;

            //GetDesc
            var s = new StringBuilder();
            s.AppendLine(powerdef.GetDescription());
            s.AppendLine(PostAbilityVerbCompDesc(Verb.UseAbilityProps));
            command_CastPower.defaultDesc = s.ToString();
            s = null;
            command_CastPower.targetingParams = powerdef.MainVerb.targetParams;
            command_CastPower.icon = powerdef.uiIcon;
            command_CastPower.action = delegate(Thing target)
            {
                var tInfo = GenUI.TargetsAt(UI.MouseMapPosition(), Verb.verbProps.targetParams, false)?.First() ??
                            target;
                TryCastAbility(AbilityContext.Player, tInfo);
            };

            var reason = "";
            if (!CanCastPowerCheck(AbilityContext.Player, out reason))
                command_CastPower.Disable(reason);
            return command_CastPower;
        }

        public virtual bool CanCastPowerCheck(AbilityContext context, out string reason)
        {
            reason = "";

            if (context == AbilityContext.Player && Verb.caster.Faction != Faction.OfPlayer)
            {
                reason = "CannotOrderNonControlled".Translate();
                return false;
            }
            if (Verb.CasterPawn.WorkTagIsDisabled(WorkTags.Violent) &&
                powerdef.MainVerb.isViolent)
            {
                reason = "IsIncapableOfViolence".Translate(Verb.CasterPawn.Name.ToStringShort);
                return false;
            }
            if (CooldownTicksLeft > 0)
            {
                reason = "AU_PawnAbilityRecharging".Translate(Verb.CasterPawn.Name.ToStringShort);
                return false;
            }
            //else if (!Verb.CasterPawn.drafter.Drafted)
            //{
            //    reason = "IsNotDrafted".Translate(new object[]
            //    {
            //        Verb.CasterPawn.Name.ToStringShort
            //    });
            //}

            return true;
        }


        public virtual void PostAbilityAttempt()
        {
            CooldownTicksLeft = MaxCastingTicks;
        }

        public virtual string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef)
        {
            return "";
        }

        public virtual string PostAbilityVerbDesc()
        {
            return "";
        }

        public static string GenerateID(Pawn pawn, AbilityDef def)
        {
            return def.defName + "_" + pawn.GetUniqueLoadID();
        }

        public string GetUniqueLoadID()
        {
            return GenerateID(Pawn, Def);
        }

        public override int GetHashCode()
        {
            var num = 66;
            num = Gen.HashCombineInt(num, Def.shortHash);
            if (Pawn != null)
                num = Gen.HashCombineInt(num, Pawn.thingIDNumber);
            if (Verb.AbilityProjectileDef != null)
                num = Gen.HashCombineInt(num, Verb.AbilityProjectileDef.shortHash);
            return num;
        }
    }
}