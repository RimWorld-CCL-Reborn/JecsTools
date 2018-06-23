using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

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

        private AbilityData abilityData;

        public Pawn abilityUserSave;

        public bool IsInitialized;

        public virtual AbilityData AbilityData
        {
            get
            {
                if (abilityData == null)
                    abilityData = new AbilityData(this);
                return abilityData;
            }
        }

        public Pawn Pawn => AbilityUser;

        public Pawn AbilityUser
        {
            get
            {
                if (abilityUserSave == null)
                    abilityUserSave = parent as Pawn;
                return abilityUserSave;
            }
        }

        public CompProperties_AbilityUser Props => (CompProperties_AbilityUser) props;

        //public List<Verb_UseAbility> AbilityVerbs = new List<Verb_UseAbility>();

        public void AddPawnAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1)
        {
            AddAbilityInternal(abilityDef, AbilityData.Powers, activenow, savedTicks);
        }

        public void AddWeaponAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1)
        {
            AddAbilityInternal(abilityDef, AbilityData.TemporaryWeaponPowers, activenow, savedTicks);
        }

        public void AddApparelAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1)
        {
            AddAbilityInternal(abilityDef, AbilityData.TemporaryApparelPowers, activenow, savedTicks);
        }

        private void AddAbilityInternal(AbilityDef abilityDef, List<PawnAbility> thelist, bool activenow,
            float savedTicks)
        {
            var pa = (PawnAbility) Activator.CreateInstance(abilityDef.abilityClass);
            pa.Pawn = AbilityUser;
            pa.Def = abilityDef;
            thelist.Add(pa);
            UpdateAbilities();
        }

        public void RemovePawnAbility(AbilityDef abilityDef)
        {
            RemoveAbilityInternal(abilityDef, AbilityData.Powers);
        }

        public void RemoveWeaponAbility(AbilityDef abilityDef)
        {
            RemoveAbilityInternal(abilityDef, AbilityData.TemporaryWeaponPowers);
        }

        public void RemoveApparelAbility(AbilityDef abilityDef)
        {
            RemoveAbilityInternal(abilityDef, AbilityData.TemporaryApparelPowers);
        }

        private void RemoveAbilityInternal(AbilityDef abilityDef, List<PawnAbility> thelist)
        {
            var abilityToRemove = thelist.FirstOrDefault(x => x.Def == abilityDef);
            if (abilityToRemove != null)
                thelist.Remove(abilityToRemove);
            abilityToRemove = AbilityData.Powers.FirstOrDefault(x => x.Def == abilityDef);
            if (abilityToRemove != null)
                AbilityData.Powers.Remove(abilityToRemove);
            UpdateAbilities();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!IsInitialized && TryTransformPawn())
                Initialize();
            if (IsInitialized)
                if (AbilityData?.AllPowers != null && AbilityData?.AllPowers.Count > 0)
                    foreach (var power in AbilityData.AllPowers)
                        power.Tick();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            for (var i = 0; i < AbilityData?.AllPowers.Count; i++)
            {
                var ability = AbilityData?.AllPowers[i];
                if (ability.ShouldShowGizmo())
                    yield return ability.GetGizmo();
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref IsInitialized, "abilityUserIsInitialized" + this.GetType().ToString(), false);
            Scribe_Deep.Look(ref abilityData, "abilityData" + this.GetType().ToString(), this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var tempAbilities = new List<PawnAbility>(AbilityData.Powers);
                if (!tempAbilities.NullOrEmpty())
                    foreach (var pa in tempAbilities)
                        if (pa.Def.abilityClass != pa.GetType())
                        {
                            RemovePawnAbility(pa.Def);
                            AddPawnAbility(pa.Def);
                        }
            }
        }

        public void UpdateAbilities()
        {
            if (IsInitialized)
            {
                //this.AbilityVerbs.Clear();
                var abList = new List<PawnAbility>();
                if (!AbilityData.Powers.NullOrEmpty()) abList.AddRange(AbilityData.Powers);
                if (!AbilityData.TemporaryWeaponPowers.NullOrEmpty())
                    abList.AddRange(AbilityData.TemporaryWeaponPowers);
                if (!AbilityData.TemporaryApparelPowers.NullOrEmpty())
                    abList.AddRange(AbilityData.TemporaryApparelPowers);

                AbilityData.AllPowers = abList;
            }
        }


        // override this in your children. this is used to determine if this pawn
        // should be instantiated with this type of CompAbilityUser. By default,
        // returns true.
        public virtual bool TryTransformPawn()
        {
            return false;
        }
        
        // Allows inherited classes to determine "true" combat points for characters that spawn with these components
        public virtual float CombatPoints()
        {
            return 0;
        }

        //In some cases, a special ability user might spawn as a single character raid and cause havoc.
        //To avoid this, a special check occurs to disable the ability user, should this situation occur.
        public virtual void DisableAbilityUser()
        {
            
        }

        #region virtual

        public virtual void PostInitialize()
        {
        }

        public virtual void Initialize()
        {
            //            Log.Warning(" CompAbilityUser.Initialize ");
            IsInitialized = true;
            //this.abilityPowerManager = new AbilityPowerManager(this);
            PostInitialize();
        }

        public virtual List<HediffDef> IgnoredHediffs()
        {
            var result = new List<HediffDef>();
            return result;
        }


        public virtual bool CanCastPowerCheck(Verb_UseAbility verbAbility, out string reason)
        {
            reason = "";
            return true;
        }

        public virtual string PostAbilityVerbCompDesc(VerbProperties_Ability verbDef)
        {
            return "";
        }


        public virtual string PostAbilityVerbDesc()
        {
            return "";
        }

        public virtual float GrappleModifier => 0f;

        #endregion virtual
    }

    // Exists for items to add powers to as it will always be on every Pawn
    // and initiated.
    public class GenericCompAbilityUser : CompAbilityUser
    {
        public override bool TryTransformPawn()
        {
            return true;
        }
    }
}