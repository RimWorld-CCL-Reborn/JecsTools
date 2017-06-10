using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace AbilityUser
{
    /*
    "This class is primarily formed from code made by Cpt. Ohu for his Warhammer 40k mod.
     Credit goes where credit is due.
     Bless you, Ohu."
                                    -Jecrell
    */


    public class CompAbilityUser : CompUseEffect
    {
        protected static bool classRegisteredWithUtility = false;

        public LocalTargetInfo CurTarget;
        public AbilityDef curPower;
        public Verb_UseAbility curVerb;
        public Rot4 curRotation;
        public bool IsInitialized = false;

        protected List<PawnAbility> Powers = new List<PawnAbility>();
        protected List<PawnAbility> temporaryWeaponPowers = new List<PawnAbility>();
        protected List<PawnAbility> temporaryApparelPowers = new List<PawnAbility>();
        protected List<PawnAbility> allPowers;
        public List<PawnAbility> AllPowers
        {
            get
            {
                if (this.allPowers != null) return allPowers;
                allPowers = new List<PawnAbility>();
                if (this.Powers != null && this.Powers.Count > 0) allPowers.AddRange(this.Powers);
                if (this.temporaryApparelPowers != null && this.temporaryApparelPowers.Count > 0) allPowers.AddRange(this.temporaryApparelPowers);
                if (this.temporaryWeaponPowers != null && this.temporaryWeaponPowers.Count > 0) allPowers.AddRange(this.temporaryWeaponPowers);
                return allPowers;
            }
            set
            {
                allPowers = value;
            }
        }
        public List<Verb_UseAbility> AbilityVerbs = new List<Verb_UseAbility>();

        public void AddPawnAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, ref this.Powers, activenow, savedTicks); public void AddWeaponAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, ref this.temporaryWeaponPowers, activenow, savedTicks); public void AddApparelAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, ref this.temporaryApparelPowers, activenow, savedTicks); private void AddAbilityInternal(AbilityDef abilityDef, ref List<PawnAbility> thelist, bool activenow, float savedTicks)
        {
            PawnAbility pa = new PawnAbility(this.AbilityUser, abilityDef);
            if (activenow == false) pa.TicksUntilCasting = (int)(pa.powerdef.MainVerb.SecondsToRecharge * GenTicks.TicksPerRealSecond);
            if (savedTicks != -1) pa.TicksUntilCasting = (int)savedTicks;
            thelist.Add(pa);
            this.UpdateAbilities();

        }

        public void RemovePawnAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, ref this.Powers); public void RemoveWeaponAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, ref this.temporaryWeaponPowers); public void RemoveApparelAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, ref this.temporaryApparelPowers); private void RemoveAbilityInternal(AbilityDef abilityDef, ref List<PawnAbility> thelist)
        {
            var abilityToRemove = thelist.FirstOrDefault(x => x.powerdef == abilityDef);
            if (abilityToRemove != null)
            {
                thelist.Remove(abilityToRemove);
            }
            abilityToRemove = Powers.FirstOrDefault(x => x.powerdef == abilityDef);
            if (abilityToRemove != null)
            {
                Powers.Remove(abilityToRemove);
            }
            this.UpdateAbilities();
        }

        public Pawn abilityUserSave = null;
        public Pawn AbilityUser
        {
            get
            {
                if (this.abilityUserSave == null)
                {
                    this.abilityUserSave = this.parent as Pawn;
                }
                return this.abilityUserSave;
            }
        }
        public CompProperties_AbilityUser Props => (CompProperties_AbilityUser)this.props;

        public override void PostSpawnSetup(bool respawningAfterLoad) => base.PostSpawnSetup(respawningAfterLoad);

        public override void CompTick()
        {
            base.CompTick();
            if (!this.IsInitialized && TryTransformPawn())
            {
                Initialize();
            }
            if (this.IsInitialized)
            {
                ///Ticks for each ability
                if (this.AllPowers != null && this.AllPowers.Count > 0)
                {
                    foreach (PawnAbility power in this.AllPowers)
                    {
                        power.PawnAbilityTick();
                    }
                }

                ///Tick for each verb
                foreach (Verb v in this.AbilityVerbs)
                {
                    if (v.state != VerbState.Idle) Log.Message("  - send non-idle tick to " + v.GetType());
                    v.VerbTick();
                }
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


            foreach (Command_Target comm in GetPawnAbilityVerbs().ToList())
            {
                yield return comm;
            }

        }

        public override void PostExposeData()
        {
            //Scribe_Collections.Look<PawnAbility>(ref this.temporaryApparelPowers, "temporaryApparelPowers", LookMode.Deep, new object[]
            //    {
            //        this,
            //    });
            //Scribe_Collections.Look<PawnAbility>(ref this.temporaryWeaponPowers, "temporaryWeaponPowers", LookMode.Deep, new object[]
            //    {
            //        this,
            //    });
            Scribe_Collections.Look<PawnAbility>(ref this.Powers, "Powers", LookMode.Deep, new object[]
                {
                    this,
                });
            Scribe_Values.Look<bool>(ref this.IsInitialized, "IsInitialized", false);
        }

        #region virtual

        public virtual void PostInitialize() { }

        public virtual void Initialize()
        {
            //            Log.Warning(" CompAbilityUser.Initialize ");
            this.IsInitialized = true;
            //this.abilityPowerManager = new AbilityPowerManager(this);
            PostInitialize();
        }

        public virtual List<HediffDef> IgnoredHediffs()
        {
            List<HediffDef> result = new List<HediffDef>();
            return result;
        }


        public virtual bool CanCastPowerCheck(Verb_UseAbility verbAbility, out string reason)
        {
            reason = "";
            return true;
        }



        public virtual void PostAbilityAttempt(Pawn caster, AbilityDef ability)
        {
            return;
        }


        public virtual string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef) => "";

        #endregion virtual

        public void UpdateAbilities()
        {
            if (this.IsInitialized)
            {

                this.AbilityVerbs.Clear();
                List<PawnAbility> abList = new List<PawnAbility>();

                abList.AddRange(this.Powers);
                abList.AddRange(this.temporaryWeaponPowers);
                abList.AddRange(this.temporaryApparelPowers);

                this.AllPowers.Clear();

                this.AllPowers = abList;

                //                Log.Message("UpdateAbilities : with "+this.allPowers.Count+" powers");

                for (int i = 0; i < this.AllPowers.Count; i++)
                {
                    Verb_UseAbility newVerb = (Verb_UseAbility)Activator.CreateInstance(abList[i].powerdef.MainVerb.verbClass);
                    if (!this.AbilityVerbs.Any(item => item.verbProps == newVerb.verbProps))
                    {
                        ////Log.Message("UpdateAbilities: Added to AbilityVerbs");
                        newVerb.caster = this.AbilityUser;
                        newVerb.ability = abList[i];
                        newVerb.verbProps = abList[i].powerdef.MainVerb;
                        this.AbilityVerbs.Add(newVerb);
                    }
                }

                /*
                this.pawnAbilities.Clear();

                foreach (PawnAbility pow in abList)
                {
                Verb_UseAbility newVerb = (Verb_UseAbility)Activator.CreateInstance(pow.powerdef.MainVerb.verbClass);
                if (!AbilityVerbs.Any(item => item.verbProps == newVerb.verbProps))
                {
                ////Log.Message("UpdateAbilities: Added to pawnAbilities");
                newVerb.caster = this.abilityUser;
                newVerb.ability = pow;
                newVerb.verbProps = pow.powerdef.MainVerb;
                pawnAbilities.Add(pow, newVerb);
            }
        }
        //       //Log.Message(this.PawnAbilitys.Count.ToString());
        */
            }
        }

        public virtual bool CanOverpowerTarget(Pawn user, Thing target, AbilityDef ability) => true;

        public virtual IEnumerable<Command_PawnAbility> GetPawnAbilityVerbs()
        {
            if (this.AbilityVerbs != null && this.AbilityVerbs.Count > 0)
            {
                List<Verb_UseAbility> temp = new List<Verb_UseAbility>(this.AbilityVerbs);
                if (this.AllPowers != null && this.AllPowers.Count > 0)
                {
                    for (int i = 0; i < this.AllPowers.Count; i++)
                    {
                        int j = i;
                        Verb_UseAbility newVerb = temp[j];
                        VerbProperties_Ability newVerbProps = newVerb.UseAbilityProps;
                        newVerb.caster = this.AbilityUser;
                        newVerb.verbProps = temp[j].verbProps;

                        Command_PawnAbility command_CastPower = new Command_PawnAbility(this, this.AllPowers[i])
                        {
                            verb = newVerb,
                            defaultLabel = this.AllPowers[j].powerdef.LabelCap
                        };


                        //GetDesc
                        StringBuilder s = new StringBuilder();
                        s.AppendLine(this.AllPowers[j].powerdef.GetDescription());
                        s.AppendLine(PostAbilityVerbCompDesc(newVerb.UseAbilityProps));
                        command_CastPower.defaultDesc = s.ToString();
                        s = null;


                        command_CastPower.targetingParams = this.AllPowers[j].powerdef.MainVerb.targetParams;
                        command_CastPower.icon = this.AllPowers[j].powerdef.uiIcon;
                        command_CastPower.action = delegate(Thing target)
                        {
                            Action attackAction = CompAbilityUser.TryCastAbility(this.AbilityUser, target, this,
                                newVerb,
                                this.AllPowers[j].powerdef as AbilityDef);
                            if (attackAction != null)
                            {
                                if (CanOverpowerTarget(this.AbilityUser, target,
                                    this.AllPowers[j].powerdef as AbilityDef)) attackAction();
                            }
                        };
                        if (newVerb.caster.Faction != Faction.OfPlayer)
                        {
                            command_CastPower.Disable("CannotOrderNonControlled".Translate());
                        }
                        string reason = "";
                        if (newVerb.CasterIsPawn)
                        {
                            if (newVerb.CasterPawn.story.WorkTagIsDisabled(WorkTags.Violent) &&
                                this.AllPowers[j].powerdef.MainVerb.isViolent)
                            {
                                command_CastPower.Disable("IsIncapableOfViolence".Translate(new object[]
                                {
                                    newVerb.CasterPawn.NameStringShort
                                }));
                            }
                            else if (!newVerb.CasterPawn.drafter.Drafted)
                            {
                                command_CastPower.Disable("IsNotDrafted".Translate(new object[]
                                {
                                    newVerb.CasterPawn.NameStringShort
                                }));
                            }
                            else if (!newVerb.ability.CanFire)
                            {
                                command_CastPower.Disable("AU_PawnAbilityRecharging".Translate(new object[]
                                {
                                    newVerb.CasterPawn.NameStringShort
                                }));
                            }
                            //This is a hook for modders.
                            else if (!CanCastPowerCheck(newVerb, out reason))
                            {
                                command_CastPower.Disable(reason.Translate(new object[]
                                {
                                    newVerb.CasterPawn.NameStringShort
                                }));
                            }
                        }
                        yield return command_CastPower;
                    }
                }
                temp = null;
            }
            yield break;
        }



        public static Job AbilityJob(AbilityTargetCategory cat, LocalTargetInfo target)
        {
            switch (cat)
            {
                case AbilityTargetCategory.TargetSelf:
                    {
                        return new Job(AbilityDefOf.CastAbilitySelf, target);
                    }
                case AbilityTargetCategory.TargetAoE:
                    {
                        return new Job(AbilityDefOf.CastAbilitySelf, target);
                    }
                case AbilityTargetCategory.TargetThing:
                    {
                        return new Job(AbilityDefOf.CastAbilityVerb, target);
                    }
                default:
                    {
                        return new Job(AbilityDefOf.CastAbilityVerb, target);
                    }
            }
        }


        public static Action TryCastAbility(Pawn pawn, LocalTargetInfo target, CompAbilityUser compAbilityUser, Verb_UseAbility verb, AbilityDef psydef)
        {

            Action act = new Action(delegate
            {
                compAbilityUser.CurTarget = null;
                compAbilityUser.CurTarget = target;
                compAbilityUser.curVerb = verb;
                compAbilityUser.curPower = psydef;
                compAbilityUser.curRotation = Rot4.South;
                if (target != null && target.Thing != null)
                {
                    compAbilityUser.curRotation = target.Thing.Rotation;
                }

                Job job;
                if (target != null) job = CompAbilityUser.AbilityJob(verb.UseAbilityProps.AbilityTargetCategory, target);
                else job = CompAbilityUser.AbilityJob(verb.UseAbilityProps.AbilityTargetCategory, pawn);
                job.playerForced = true;
                job.verbToUse = verb;
                if (target != null)
                {
                    if (target.Thing is Pawn pawn2)
                    {
                        job.killIncappedTarget = pawn2.Downed;
                    }
                }

                pawn.jobs.TryTakeOrderedJob(job);
            });
            return act;
        }


        // override this in your children. this is used to determine if this pawn
        // should be instantiated with this type of CompAbilityUser. By default,
        // returns true.
        public virtual bool TryTransformPawn() => false;


    }

    // Exists for items to add powers to as it will always be on every Pawn
    // and initiated.
    public class GenericCompAbilityUser : CompAbilityUser
    {
        public override bool TryTransformPawn() => true;
    }

}