//#define COMBAT_POINTS_TEST // for testing the GeneratePawns patch that rebalance based off CompAbilityUser.CombatPoints

using System;
using System.Collections.Generic;
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

        public virtual AbilityData AbilityData => abilityData ??= new AbilityData(this);

        public Pawn Pawn => AbilityUser;

        public Pawn AbilityUser => abilityUserSave ??= parent as Pawn;

        public CompProperties_AbilityUser Props => (CompProperties_AbilityUser)props;

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
            var pa = (PawnAbility)Activator.CreateInstance(abilityDef.abilityClass);
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
            var abilityToRemoveIndex = thelist.FindIndex(x => x.Def == abilityDef);
            if (abilityToRemoveIndex >= 0)
                thelist.RemoveAt(abilityToRemoveIndex);
            abilityToRemoveIndex = AbilityData.Powers.FindIndex(x => x.Def == abilityDef);
            if (abilityToRemoveIndex >= 0)
                AbilityData.Powers.RemoveAt(abilityToRemoveIndex);
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
            {
                var allPowers = AbilityData.AllPowers;
                foreach (var power in allPowers)
                    power.Tick();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var allPowers = AbilityData.AllPowers;
            for (var i = 0; i < allPowers.Count; i++)
            {
                var ability = allPowers[i];
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
                foreach (var pa in AbilityData.Powers.ToArray()) // using copy for enumeration due to potential mutation
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
                var abilityData = AbilityData;
                if (abilityData.Powers != null)
                    abList.AddRange(abilityData.Powers);
                if (abilityData.TemporaryWeaponPowers != null)
                    abList.AddRange(abilityData.TemporaryWeaponPowers);
                if (abilityData.TemporaryApparelPowers != null)
                    abList.AddRange(abilityData.TemporaryApparelPowers);

                abilityData.AllPowers = abList;
            }
        }


        // override this in your children. this is used to determine if this pawn
        // should be instantiated with this type of CompAbilityUser. By default,
        // returns true.
        public virtual bool TryTransformPawn()
        {
            return false;
        }

#if COMBAT_POINTS_TEST
        private float? cachedCombatPoints;
#endif

        // Allows inherited classes to determine "true" combat points for characters that spawn with these components
        public virtual float CombatPoints()
        {
#if COMBAT_POINTS_TEST
            if (cachedCombatPoints == null)
            {
                PostInitialize();
                cachedCombatPoints = (Pawn.trader != null ? 100 : 0) + AbilityData.AllPowers.Count * 25;
            }
            return cachedCombatPoints.Value;
#else
            return 0;
#endif
        }

        //In some cases, a special ability user might spawn as a single character raid and cause havoc.
        //To avoid this, a special check occurs to disable the ability user, should this situation occur.
        public virtual void DisableAbilityUser()
        {
#if COMBAT_POINTS_TEST
            cachedCombatPoints = 0;
#endif
        }

        #region virtual

        public virtual void PostInitialize()
        {
        }

        public virtual void Initialize()
        {
            //Log.Message("CompAbilityUser.Initialize");
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
