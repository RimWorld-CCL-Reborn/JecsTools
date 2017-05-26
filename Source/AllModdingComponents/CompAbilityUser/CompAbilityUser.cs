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

        //public AbilityPowerManager abilityPowerManager;
        public LocalTargetInfo CurTarget;
        public AbilityDef curPower;
        public Verb_UseAbility curVerb;
        public Rot4 curRotation;
        //public bool IsActive;
        public bool ShotFired = true;
        public bool IsInitialized = false;
        public float TicksToCastPercentage = 1;
        public int ticksToImpact = 500;
        public int TicksToCastMax = 100;
        public int TicksToCast = -1;

        public List<PawnAbility> Powers = new List<PawnAbility>();
        protected List<PawnAbility> temporaryWeaponPowers = new List<PawnAbility>();
        protected List<PawnAbility> temporaryApparelPowers = new List<PawnAbility>();
        public List<PawnAbility> allPowers = new List<PawnAbility>();
        public List<Verb_UseAbility> AbilityVerbs = new List<Verb_UseAbility>();
//        public Dictionary<PawnAbility, Verb_UseAbility> pawnAbilities = new Dictionary<PawnAbility, Verb_UseAbility>();

            /*
            if (!this.Powers.Any(x => x.powerdef.defName == abilityDef.defName))
            {
                this.Powers.Add(new PawnAbility(this.abilityUser, abilityDef));
            }

            this.UpdateAbilities();
            */
        public void AddPawnAbility(AbilityDef abilityDef) { this.addAbilityInternal(abilityDef,ref this.Powers); }
        public void AddWeaponAbility(AbilityDef abilityDef) { this.addAbilityInternal(abilityDef,ref this.temporaryWeaponPowers); }
        public void AddApparelAbility(AbilityDef abilityDef) { this.addAbilityInternal(abilityDef,ref this.temporaryApparelPowers); }
        private void addAbilityInternal(AbilityDef abilityDef, ref List<PawnAbility> thelist) {
            if (!thelist.Any(x => x.powerdef.defName == abilityDef.defName))
            {
                thelist.Add(new PawnAbility(this.abilityUser, abilityDef));
            }
            this.UpdateAbilities();
        }

        public void RemovePawnAbility(AbilityDef abilityDef) { this.removeAbilityInternal(abilityDef, ref this.Powers); }
        public void RemoveWeaponAbility(AbilityDef abilityDef) { this.removeAbilityInternal(abilityDef, ref this.temporaryWeaponPowers); }
        public void RemoveApparelAbility(AbilityDef abilityDef) { this.removeAbilityInternal(abilityDef, ref this.temporaryApparelPowers); }
        private void removeAbilityInternal(AbilityDef abilityDef, ref List<PawnAbility> thelist)
        {
            PawnAbility abilityToRemove = thelist.FirstOrDefault(x => x.powerdef.defName == abilityDef.defName);
            if (abilityToRemove != null)
            {
                thelist.Remove(abilityToRemove);
            }

            this.UpdateAbilities();
        }

        public Pawn abilityUserSave = null;
        public Pawn abilityUser
        {
            get
            {
                if (abilityUserSave == null)
                {
                    abilityUserSave = this.parent as Pawn;
                }
                return abilityUserSave;
            }
        }
        public CompProperties_AbilityUser Props
        {
            get
            {
                return (CompProperties_AbilityUser)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void CompTick()
        {
            base.CompTick();
//            Log.Message("CompAbiltyUser Tick");
            if ( !IsInitialized && TryTransformPawn() ) {
//                Log.Warning(" YES: a CompAbilityUser is being Initialized");
                Initialize();
            }
            if ( IsInitialized ) {
//                Log.Message("CompAbiltyUser CompTick 2");
                this.TicksToCast--;
                if (this.TicksToCast < -1)
                {
                    //this.IsActive = true;
                    this.ShotFired = true;
                    this.TicksToCast = -1;
                }
//                if (Powers != null && Powers.Count > 0) { foreach (PawnAbility power in Powers) { power.PawnAbilityTick(); } }
//                Log.Message("   there are "+this.allPowers.Count+" powers");
                if (this.allPowers != null && this.allPowers.Count > 0) {
                    foreach (PawnAbility power in this.allPowers) {
                        power.PawnAbilityTick(); }
                }
                this.TicksToCastPercentage = (1 - (this.TicksToCast / this.TicksToCastMax));
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
            //base.PostExposeData();
            Scribe_Collections.Look<PawnAbility>(ref this.allPowers, "allPowers", LookMode.Deep, new object[]
                {
                    this,
                });
            Scribe_Collections.Look<PawnAbility>(ref this.temporaryApparelPowers, "temporaryApparelPowers", LookMode.Deep, new object[]
                {
                    this,
                });
            Scribe_Collections.Look<PawnAbility>(ref this.temporaryWeaponPowers, "temporaryWeaponPowers", LookMode.Deep, new object[]
                {
                    this,
                });
            Scribe_Collections.Look<PawnAbility>(ref this.Powers, "Powers", LookMode.Deep, new object[]
                {
                    this,
                });

            Scribe_Values.Look<int>(ref this.TicksToCast, "TicksToCast", 0, false);
            Scribe_Values.Look<int>(ref this.TicksToCastMax, "TicksToCastMax", 1, false);
            Scribe_Values.Look<float>(ref this.TicksToCastPercentage, "TicksToCastPercentage", 1, false);
            //Scribe_Values.Look<bool>(ref this.IsActive, "IsActive", false, false);
            Scribe_Values.Look<bool>(ref this.ShotFired, "ShotFired", true, false);
            Scribe_Values.Look<bool>(ref this.IsInitialized, "IsInitialized", false);
            //Log.Message("PostExposeData Called: AbilityUser");
        }

        #region virtual

        public virtual void PostInitialize() { }

        public virtual void Initialize()
        {
//            Log.Warning(" CompAbilityUser.Initialize ");
            IsInitialized = true;
            //this.abilityPowerManager = new AbilityPowerManager(this);
            PostInitialize();
        }

        public virtual List<HediffDef> ignoredHediffs()
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


        public virtual string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef)
        {
            return "";
        }

        #endregion virtual

        public void UpdateAbilities()
        {
            if ( IsInitialized ) {

                AbilityVerbs.Clear();
                List<PawnAbility> abList = new List<PawnAbility>();

                abList.AddRange(this.Powers);
                abList.AddRange(this.temporaryWeaponPowers);
                abList.AddRange(this.temporaryApparelPowers);

                this.allPowers.Clear();

                this.allPowers = abList;

//                Log.Message("UpdateAbilities : with "+this.allPowers.Count+" powers");

                for (int i = 0; i < allPowers.Count; i++)
                {
                    Verb_UseAbility newVerb = (Verb_UseAbility)Activator.CreateInstance(abList[i].powerdef.MainVerb.verbClass);
                    if (!AbilityVerbs.Any(item => item.verbProps == newVerb.verbProps))
                    {
                        ////Log.Message("UpdateAbilities: Added to AbilityVerbs");
                        newVerb.caster = this.abilityUser;
                        newVerb.ability = abList[i];
                        newVerb.verbProps = abList[i].powerdef.MainVerb;
                        AbilityVerbs.Add(newVerb);
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

        public virtual bool CanOverpowerTarget(Pawn user, Thing target, AbilityDef ability)
        {
            return true;
        }

        public IEnumerable<Command_PawnAbility> GetPawnAbilityVerbs()
        {
            //Log.ErrorOnce("GetPawnAbilityVerbs Called", 912912);
            List<Verb_UseAbility> temp = new List<Verb_UseAbility>();
            temp.AddRange(this.AbilityVerbs);
            for (int i = 0; i < allPowers.Count; i++)
            {
                int j = i;
                Verb_UseAbility newVerb = temp[j];
                VerbProperties_Ability newVerbProps = newVerb.useAbilityProps;
                newVerb.caster = this.abilityUser;
                newVerb.verbProps = temp[j].verbProps;

                Command_PawnAbility command_CastPower = new Command_PawnAbility(this, allPowers[i]);
                command_CastPower.verb = newVerb;
                command_CastPower.defaultLabel = allPowers[j].powerdef.LabelCap;


                //GetDesc
                StringBuilder s = new StringBuilder();
                s.AppendLine(allPowers[j].powerdef.GetDescription());
                s.AppendLine(PostAbilityVerbCompDesc(newVerb.useAbilityProps));
                command_CastPower.defaultDesc = s.ToString();
                s = null;


                command_CastPower.targetingParams = allPowers[j].powerdef.MainVerb.targetParams;
                //command_CastPower.targetingParams = TargetingParameters.ForAttackAny();

                //if (newVerb.useAbilityProps.AbilityTargetCategory == AbilityTargetCategory.TargetSelf)
                //{
                //    command_CastPower.targetingParams = TargetingParameters.ForSelf(this.abilityUser);
                //}
                //else
                //{
                //    command_CastPower.targetingParams = TargetingParameters.
                //}
                command_CastPower.icon = allPowers[j].powerdef.uiIcon;
                string str;
                //if (FloatMenuUtility.GetAttackAction(this.abilityUser, LocalTargetInfo.Invalid, out str) == null)
                //{
                //    command_CastPower.Disable(str.CapitalizeFirst() + ".");
                //}
                command_CastPower.action = delegate (Thing target)
                {
                    Action attackAction = CompAbilityUser.TryCastAbility(abilityUser, target, this, newVerb, allPowers[j].powerdef as AbilityDef);
                    if (attackAction != null)
                    {
                        if (CanOverpowerTarget(abilityUser, target, allPowers[j].powerdef as AbilityDef)) attackAction();
                    }
                };
                if (newVerb.caster.Faction != Faction.OfPlayer)
                {
                    command_CastPower.Disable("CannotOrderNonControlled".Translate());
                }
                string reason = "";
                if (newVerb.CasterIsPawn)
                {
                    if (newVerb.CasterPawn.story.WorkTagIsDisabled(WorkTags.Violent) && allPowers[j].powerdef.MainVerb.isViolent)
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
            temp = null;
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
                if (target != null) job = CompAbilityUser.AbilityJob(verb.useAbilityProps.AbilityTargetCategory, target);
                else job = CompAbilityUser.AbilityJob(verb.useAbilityProps.AbilityTargetCategory, pawn);
                job.playerForced = true;
                job.verbToUse = verb;
                if (target != null)
                {
                    Pawn pawn2 = target.Thing as Pawn;
                    if (pawn2 != null)
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
        public virtual bool TryTransformPawn() { return false; }



    }

    // Exists for items to add powers to as it will always be on every Pawn
    // and initiated.
    public class GenericCompAbilityUser : CompAbilityUser {
        public override bool TryTransformPawn() { return true; }

    }

}
