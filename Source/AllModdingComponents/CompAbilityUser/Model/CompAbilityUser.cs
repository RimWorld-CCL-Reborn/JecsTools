using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

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

        //public LocalTargetInfo CurTarget;
        //public AbilityDef curPower;
        //public Verb_UseAbility curVerb;
        //public Rot4 curRotation;
        public bool IsInitialized = false;

        protected List<PawnAbility> Powers = new List<PawnAbility>();
        protected List<PawnAbility> temporaryWeaponPowers = new List<PawnAbility>();
        protected List<PawnAbility> temporaryApparelPowers = new List<PawnAbility>();
        protected List<PawnAbility> allPowers;
        public List<PawnAbility> AllPowers
        {
            get
            {
                if (this.allPowers == null)
                {
                    this.allPowers = new List<PawnAbility>();
                    if (!this.Powers.NullOrEmpty())
                        this.allPowers.AddRange(this.Powers);
                    if (!this.temporaryApparelPowers.NullOrEmpty())
                        this.allPowers.AddRange(this.temporaryApparelPowers);
                    if (!this.temporaryWeaponPowers.NullOrEmpty())
                        this.allPowers.AddRange(this.temporaryWeaponPowers);
                }
                return this.allPowers;
            }
            set => this.allPowers = value;
        }
        public List<Verb_UseAbility> AbilityVerbs = new List<Verb_UseAbility>();

        public void AddPawnAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, ref this.Powers, activenow, savedTicks); public void AddWeaponAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, ref this.temporaryWeaponPowers, activenow, savedTicks); public void AddApparelAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, ref this.temporaryApparelPowers, activenow, savedTicks); private void AddAbilityInternal(AbilityDef abilityDef, ref List<PawnAbility> thelist, bool activenow, float savedTicks)
        {
            PawnAbility pa = (PawnAbility)Activator.CreateInstance(abilityDef.abilityClass);
            Log.Message(abilityDef.abilityClass.ToString());
            pa.Pawn = this.AbilityUser;
            pa.Def = abilityDef;
            thelist.Add(pa);
            this.UpdateAbilities();
        }

        public void RemovePawnAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, ref this.Powers); public void RemoveWeaponAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, ref this.temporaryWeaponPowers); public void RemoveApparelAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, ref this.temporaryApparelPowers); private void RemoveAbilityInternal(AbilityDef abilityDef, ref List<PawnAbility> thelist)
        {
            PawnAbility abilityToRemove = thelist.FirstOrDefault(x => x.Def == abilityDef);
            if (abilityToRemove != null)
            {
                thelist.Remove(abilityToRemove);
            }
            abilityToRemove = this.Powers.FirstOrDefault(x => x.Def == abilityDef);
            if (abilityToRemove != null)
            {
                this.Powers.Remove(abilityToRemove);
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
                        power.Tick();
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            for (int i = 0; i < this.AllPowers.Count; i++)
            {
                yield return this.AllPowers[i].GetGizmo();
            }
        }

        public override void PostExposeData()
        {
            Scribe_Collections.Look<PawnAbility>(ref this.Powers, "Powers", LookMode.Deep, new object[]
                {
                    this,
                });
            Scribe_Values.Look<bool>(ref this.IsInitialized, "IsInitialized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                List<PawnAbility> tempAbilities = new List<PawnAbility>(this.Powers);
                if (!tempAbilities.NullOrEmpty())
                {
                    foreach (PawnAbility pa in tempAbilities)
                    {
                        if (pa.Def.abilityClass != pa.GetType())
                        {
                            RemovePawnAbility(pa.Def);
                            AddPawnAbility(pa.Def);
                        }
                    }
                }
            }
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

        public virtual string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef) => "";


        public virtual string PostAbilityVerbDesc() => "";


        #endregion virtual

        public void UpdateAbilities()
        {
            if (this.IsInitialized)
            {
                //this.AbilityVerbs.Clear();

                List<PawnAbility> abList = new List<PawnAbility>();
                if (!this.Powers.NullOrEmpty()) abList.AddRange(this.Powers);
                if (!this.temporaryWeaponPowers.NullOrEmpty()) abList.AddRange(this.temporaryWeaponPowers);
                if (!this.temporaryApparelPowers.NullOrEmpty()) abList.AddRange(this.temporaryApparelPowers);

                this.AllPowers = abList;
                
            }
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