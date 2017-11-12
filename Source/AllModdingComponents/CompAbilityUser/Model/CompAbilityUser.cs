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

        private AbilityData abilityData = null;
        public virtual AbilityData AbilityData
        {
            get
            {
                if (abilityData == null)
                {
                    abilityData = new AbilityData(this);
                }
                return abilityData;
            }
        }

        public bool IsInitialized = false;

        //public List<Verb_UseAbility> AbilityVerbs = new List<Verb_UseAbility>();

        public void AddPawnAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, this.AbilityData.Powers, activenow, savedTicks);
        public void AddWeaponAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, this.AbilityData.TemporaryWeaponPowers, activenow, savedTicks);
        public void AddApparelAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1) => this.AddAbilityInternal(abilityDef, this.AbilityData.TemporaryApparelPowers, activenow, savedTicks);

        private void AddAbilityInternal(AbilityDef abilityDef, List<PawnAbility> thelist, bool activenow, float savedTicks)
        {
            PawnAbility pa = (PawnAbility)Activator.CreateInstance(abilityDef.abilityClass);
            //Log.Message(abilityDef.abilityClass.ToString());
            pa.Pawn = this.AbilityUser;
            pa.Def = abilityDef;
            thelist.Add(pa);
            this.UpdateAbilities();
        }

        public void RemovePawnAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, this.AbilityData.Powers);
        public void RemoveWeaponAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, this.AbilityData.TemporaryWeaponPowers);
        public void RemoveApparelAbility(AbilityDef abilityDef) => this.RemoveAbilityInternal(abilityDef, this.AbilityData.TemporaryApparelPowers);

        private void RemoveAbilityInternal(AbilityDef abilityDef, List<PawnAbility> thelist)
        {
            PawnAbility abilityToRemove = thelist.FirstOrDefault(x => x.Def == abilityDef);
            if (abilityToRemove != null)
            {
                thelist.Remove(abilityToRemove);
            }
            abilityToRemove = this.AbilityData.Powers.FirstOrDefault(x => x.Def == abilityDef);
            if (abilityToRemove != null)
            {
                this.AbilityData.Powers.Remove(abilityToRemove);
            }
            this.UpdateAbilities();
        }

        public Pawn abilityUserSave = null;
        public Pawn Pawn => AbilityUser;
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
                if (this.AbilityData?.AllPowers != null && this.AbilityData?.AllPowers.Count > 0)
                {
                    foreach (PawnAbility power in this.AbilityData.AllPowers)
                    {
                        power.Tick();
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            for (int i = 0; i < this.AbilityData?.AllPowers.Count; i++)
            {
                yield return this.AbilityData?.AllPowers[i].GetGizmo();
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref this.IsInitialized, "IsInitialized", false);
            Scribe_Deep.Look<AbilityData>(ref this.abilityData, "abilityData", new object[]
            {
                this
            });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                List<PawnAbility> tempAbilities = new List<PawnAbility>(this.AbilityData.Powers);
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

        public virtual float GrappleModifier => 0f;


        #endregion virtual

        public void UpdateAbilities()
        {
            if (this.IsInitialized)
            {
                //this.AbilityVerbs.Clear();
                List<PawnAbility> abList = new List<PawnAbility>();
                if (!this.AbilityData.Powers.NullOrEmpty()) abList.AddRange(this.AbilityData.Powers);
                if (!this.AbilityData.TemporaryWeaponPowers.NullOrEmpty()) abList.AddRange(this.AbilityData.TemporaryWeaponPowers);
                if (!this.AbilityData.TemporaryApparelPowers.NullOrEmpty()) abList.AddRange(this.AbilityData.TemporaryApparelPowers);

                this.AbilityData.AllPowers = abList;
                
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