//#define DEBUGLOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    public class PawnAbility : IExposable, IEquatable<PawnAbility>
    {
        // Note: With abilityUser being a public field and AbilityUser being a virtual property,
        // the authoritative source for pawn must be AbilityUser, so there's no point having a pawn field.
        [Obsolete("Use AbilityUser property instead")]
        public CompAbilityUser abilityUser; // public for resolving saving / loading issues between versions
        private AbilityDef powerDef;
        private int ticksUntilCasting = -1;
        private Verb_UseAbility verb;

        [Conditional("DEBUGLOG")]
        private static void DebugMessage(string s) => Log.Message(s);

        public PawnAbility()
        {
            DebugMessage($"new PawnAbility()");
        }

        public PawnAbility(CompAbilityUser comp)
        {
            if (comp.Pawn == null)
                throw new ArgumentNullException("comp.Pawn cannot be null");
#pragma warning disable CS0618 // Type or member is obsolete
            abilityUser = comp;
            DebugMessage($"new PawnAbility({comp}) => abilityUser={abilityUser}");
#pragma warning restore CS0618 // Type or member is obsolete
        }

        internal void Initialize(CompAbilityUser abilityUser, AbilityDef powerDef, int ticksUntilCasting)
        {
            if (abilityUser.Pawn == null)
                throw new ArgumentNullException("comp.Pawn cannot be null");
            DebugMessage($"PawnAbility.Initialize({this}, {abilityUser}, {powerDef}, {ticksUntilCasting})");
#pragma warning disable CS0618 // Type or member is obsolete
            this.abilityUser = abilityUser;
#pragma warning restore CS0618 // Type or member is obsolete
            this.powerDef = powerDef;
            this.ticksUntilCasting = ticksUntilCasting;
        }

        public PawnAbility(AbilityData data)
        {
            var pawn = data.Pawn;
            if (pawn == null)
                throw new ArgumentNullException("data.Pawn cannot be null");
#pragma warning disable CS0618 // Type or member is obsolete
            abilityUser = pawn.GetExactCompAbilityUser(data.AbilityClass);
            DebugMessage($"new PawnAbility{data} => abilityUser={abilityUser}");
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Obsolete("This only works reliably when there's a single CompAbilityUser")]
        public PawnAbility(Pawn user, AbilityDef pdef)
        {
            if (user == null)
                throw new ArgumentNullException("user cannot be null");
            if (pdef == null)
                throw new ArgumentNullException("pdef cannot be null");
            abilityUser = user.GetCompAbilityUser();
            powerDef = pdef;
            DebugMessage($"new PawnAbility({user}, {pdef}) => abilityUser={abilityUser}");
        }

        // Note: Since this is virtual, the AbilityUser implementation isn't guaranteed to use abilityUser field,
        // but we still set abilityUser in other places in case this implementation is used.
        public virtual CompAbilityUser AbilityUser
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get => abilityUser;
            set
            {
                if (abilityUser != value)
                {
                    abilityUser = value ?? throw new ArgumentNullException("AbilityUser cannot be set to null");
                    verb = null;
                }
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public Pawn Pawn
        {
            get => AbilityUser?.Pawn; // can be null during initialization, but is assumed non-null afterwards
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Pawn cannot be null");
                var abilityUser = AbilityUser;
                if (abilityUser == null)
                {
                    Log.ErrorOnce("PawnAbility.AbilityUser is unexpectedly null while setting Pawn - " +
                        "defaulting it to Pawn's first CompAbilityUser", 601945744);
#pragma warning disable CS0618 // Type or member is obsolete
                    AbilityUser = value.GetCompAbilityUser();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                else if (abilityUser.Pawn != value)
                {
                    DebugMessage($"PawnAbility.set_Pawn({this}, {value}): abilityUser={AbilityUser} => " +
                        value.GetExactCompAbilityUser(abilityUser.GetType()));
                    AbilityUser = value.GetExactCompAbilityUser(abilityUser.GetType());
                }
            }
        }

        public AbilityDef Def
        {
            get => powerDef; // can be null during initialization, but is assumed to be non-null afterwards
            set
            {
                if (powerDef != value)
                {
                    powerDef = value ?? throw new ArgumentNullException("Def cannot be set to null");
                    verb = null;
                }
            }
        }

        // XXX: This doesn't seem to be used. Lazy-init to avoid unnecessary List construction cost.
        private List<ThingComp> comps;
        public List<ThingComp> Comps => comps ??= new List<ThingComp>();

        public Texture2D PowerButton => powerDef.uiIcon;

        public int CooldownTicksLeft
        {
            get => ticksUntilCasting;
            set => ticksUntilCasting = value;
        }

        public virtual void Notify_AbilityFailed(bool refund)
        {
            Pawn.jobs.StopAll();
            if (refund)
                CooldownTicksLeft = -1;
        }

        public int MaxCastingTicks => (int)(powerDef.MainVerb.SecondsToRecharge * GenTicks.TicksPerRealSecond);

        public Verb_UseAbility Verb
        {
            get
            {
                if (verb == null)
                    InitVerb(Pawn);
                else
                {
                    var pawn = Pawn;
                    if (verb.caster != pawn)
                        InitVerb(pawn);
                }
                return verb;
            }
        }

        private void InitVerb(Pawn pawn)
        {
            var newVerb = (Verb_UseAbility)Activator.CreateInstance(powerDef.MainVerb.verbClass);
            newVerb.caster = pawn;
            newVerb.Ability = this;
            newVerb.verbProps = powerDef.MainVerb;
            DebugMessage($"PawnAbility.InitVerb({this}, {pawn}) => {newVerb}");
            verb = newVerb;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref ticksUntilCasting, "pawnAbilityTicksUntilcasting", -1);
            // pawnAbilityPawn is no longer necessary, but for backwards compatibility, still save it.
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                var pawn = Pawn;
                Scribe_References.Look(ref pawn, "pawnAbilityPawn");
            }
            Scribe_Defs.Look(ref powerDef, "pawnAbilityPowerDef");
            DebugMessage($"PawnAbility.ExposeData({this}) for mode={Scribe.mode}");
        }

        public void Tick()
        {
            powerDef.PassiveProps?.Worker?.Tick(AbilityUser);
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
            if (target.Thing != null && !CanOverpowerTarget(context, target, out var _))
                return null;
            var job = GetJob(context, target);
            if (context == AbilityContext.Player)
                Pawn.jobs.TryTakeOrderedJob(job);
            return job;
        }

        public virtual Job GetJob(AbilityContext context, LocalTargetInfo target)
        {
            var verb = Verb;
            var job = powerDef.GetJob(verb.UseAbilityProps.AbilityTargetCategory, target);
            job.playerForced = true;
            job.verbToUse = verb;
            job.count = context == AbilityContext.Player ? 1 : 0; //Count 1 for Player : 0 for AI
            if (target.Thing is Pawn targetPawn)
                job.killIncappedTarget = targetPawn.Downed;
            return job;
        }

        public bool TryCastAbility(AbilityContext context, LocalTargetInfo target)
        {
            //Can our body cast?
            if (!CanCastPowerCheck(context, out var _))
                return false;

            //If we're a player, let's target.
            if (context == AbilityContext.Player)
            {
                var verb = Verb;
                var casterPawn = verb.CasterPawn;
                var targeter = Find.Targeter;
                if (targeter.targetingSource?.GetVerb?.verbProps == verb.verbProps)
                {
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

        // This method and CanCastPowerCheck are based off VerbTracker.CreateVerbTargetCommand.
        public Command_PawnAbility GetGizmo()
        {
            var verb = Verb;
            var verbProps = verb.UseAbilityProps;
            var command_CastPower = new Command_PawnAbility(AbilityUser, this, CooldownTicksLeft)
            {
                verb = verb,
                defaultLabel = powerDef.LabelCap,
                defaultDesc = powerDef.GetDescription() + "\n" + PostAbilityVerbCompDesc(verbProps) + "\n",
                targetingParams = powerDef.MainVerb.targetParams,
                icon = powerDef.uiIcon,
                action = delegate (Thing target)
                {
                    var tInfo = GenUI.TargetsAt_NewTemp(UI.MouseMapPosition(), verbProps.targetParams).FirstOrFallback(target);
                    TryCastAbility(AbilityContext.Player, tInfo);
                },
            };

            if (!CanCastPowerCheck(AbilityContext.Player, out var reason))
                command_CastPower.Disable(reason);
            return command_CastPower;
        }

        public virtual bool CanCastPowerCheck(AbilityContext context, out string reason)
        {
            var casterPawn = verb.CasterPawn;
            if (context == AbilityContext.Player && casterPawn.Faction != Faction.OfPlayer)
            {
                reason = "CannotOrderNonControlled".Translate();
                return false;
            }
            if (powerDef.MainVerb.isViolent && casterPawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                reason = "IsIncapableOfViolence".Translate(casterPawn.LabelShort, casterPawn);
                return false;
            }
            if (CooldownTicksLeft > 0)
            {
                reason = "AU_PawnAbilityRecharging".Translate(casterPawn.LabelShort);
                return false;
            }
            //else if (!casterPawn.drafter.Drafted)
            //{
            //    reason = "IsNotDrafted".Translate(casterPawn.LabelShort, casterPawn);
            //}
            else
            {
                reason = "";
                return true;
            }
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

        [Obsolete("Formerly used in Equals implementation - no longer serves any purpose")]
        public static string GenerateID(Pawn pawn, AbilityDef def)
        {
            return def.defName + "_" + pawn.GetUniqueLoadID();
        }

        [Obsolete("Formerly used in Equals implementation - no longer serves any purpose")]
        public string GetUniqueLoadID()
        {
            return GenerateID(Pawn, Def);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PawnAbility);
        }

        public bool Equals(PawnAbility other)
        {
            static bool DefEquals(Def a, Def b) => a == b || a != null && b != null && a.defName == b.defName;
            return other != null && DefEquals(Def, other.Def) && Equals(AbilityUser, other.AbilityUser);
        }

        public override int GetHashCode()
        {
            return Gen.HashCombineInt(Gen.HashCombineInt(66, Def?.shortHash ?? 0), AbilityUser?.GetHashCode() ?? 0);
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Pawn={Pawn}, Def={Def}, CooldownTicksLeft={CooldownTicksLeft})";
        }
    }
}
