// Uncomment following for testing GeneratePawns Harmony patch for CompAbilityUser.CombatPoints-based rebalancing
//#define COMBAT_POINTS_TEST

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
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
        private AbilityData abilityData;

        [Obsolete("Use AbilityUser property instead")]
        public Pawn abilityUserSave;

        [Obsolete("Use Initialized property instead")]
        public bool IsInitialized;

        public virtual AbilityData AbilityData => abilityData ??= new AbilityData(this);

#pragma warning disable CS0618 // Type or member is obsolete
        public Pawn Pawn => abilityUserSave ??= (Pawn)parent;
#pragma warning restore CS0618 // Type or member is obsolete

        [Obsolete("Use Pawn property instead")]
        public Pawn AbilityUser => Pawn;

        public bool Initialized
        {
#pragma warning disable CS0618 // Type or member is obsolete
            get => IsInitialized;
            protected set => IsInitialized = value;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public CompProperties_AbilityUser Props => (CompProperties_AbilityUser)props;

        public void AddPawnAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1)
        {
            AddAbilityInternal(abilityDef, AbilityData.Powers, savedTicks);
        }

        public void AddWeaponAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1)
        {
            AddAbilityInternal(abilityDef, AbilityData.TemporaryWeaponPowers, savedTicks);
        }

        public void AddApparelAbility(AbilityDef abilityDef, bool activenow = true, float savedTicks = -1)
        {
            AddAbilityInternal(abilityDef, AbilityData.TemporaryApparelPowers, savedTicks);
        }

        private void AddAbilityInternal(AbilityDef abilityDef, List<PawnAbility> abilities, float savedTicks)
        {
            abilities.Add(CreateAbility(abilityDef, savedTicks));
            UpdateAbilities();
        }

        private PawnAbility CreateAbility(AbilityDef abilityDef, float savedTicks)
        {
            // For backwards compatibility, must still use the parameterless constructor.
            var pa = (PawnAbility)Activator.CreateInstance(abilityDef.abilityClass);
            pa.Initialize(this, abilityDef, Mathf.RoundToInt(savedTicks));
            return pa;
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

        private void RemoveAbilityInternal(AbilityDef abilityDef, List<PawnAbility> abilities)
        {
            var abilityToRemoveIndex = abilities.FindIndex(x => x.Def == abilityDef);
            if (abilityToRemoveIndex != -1)
                abilities.RemoveAt(abilityToRemoveIndex);
            // TODO: Is always removing from AbilityData.Powers really necessary?
            var powers = AbilityData.Powers;
            if (abilities != powers)
            {
                abilityToRemoveIndex = powers.FindIndex(x => x.Def == abilityDef);
                if (abilityToRemoveIndex != -1)
                    powers.RemoveAt(abilityToRemoveIndex);
            }
            UpdateAbilities();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!Initialized && TryTransformPawn())
                Initialize();
            if (Initialized)
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
            var typeString = GetType().ToString();
#pragma warning disable CS0618 // Type or member is obsolete
            Scribe_Values.Look(ref IsInitialized, "abilityUserIsInitialized" + typeString);
#pragma warning restore CS0618 // Type or member is obsolete
            Scribe_Deep.Look(ref abilityData, nameof(abilityData) + typeString, this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var dirty = false;
                var powers = AbilityData.Powers;
                for (var i = 0; i < powers.Count; i++)
                {
                    var pa = powers[i];
                    if (pa.Def.abilityClass != pa.GetType())
                    {
                        powers[i] = CreateAbility(pa.Def, pa.CooldownTicksLeft);
                        dirty = true;
                    }
                }
                if (dirty)
                    UpdateAbilities();
            }
        }

        public void UpdateAbilities()
        {
            if (Initialized)
            {
                // This forces get access of AbilityData.AllPowers to refresh its list.
                AbilityData.AllPowers = null;
            }
        }


        // Override this in your implementation. This is used to determine if this pawn
        // should be initialized with this type of CompAbilityUser. By default, returns false.
        public virtual bool TryTransformPawn()
        {
            return false;
        }

#if COMBAT_POINTS_TEST
        private float? cachedCombatPoints;
#endif

        // Allows inherited classes to determine "true" combat points for characters that spawn with these components.
        // Note: This is called before the parent pawn is spawned and thus Initialize (and PostInitialize) isn't called
        // yet (unless already explicitly called in e.g. a PostPostMake override).
        public virtual float CombatPoints()
        {
#if COMBAT_POINTS_TEST
            if (cachedCombatPoints == null)
            {
                if (!Initialized)
                    Initialize();
                cachedCombatPoints = (Pawn.trader != null ? 100 : 0) + AbilityData.AllPowers.Count * 25;
                //Log.Message($"CompAbilityUser.CombatPoints({this}) => {cachedCombatPoints}");
            }
            return cachedCombatPoints.GetValueOrDefault();
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

        // Note: To avoid duplicate initialization and working Initialized property value,
        // subclasses should override PostInitialize instead of this method.
        public virtual void Initialize()
        {
            //Log.Message($"CompAbilityUser.Initialize({this})");
#pragma warning disable CS0618 // Type or member is obsolete
            IsInitialized = true;
#pragma warning restore CS0618 // Type or member is obsolete
            PostInitialize();
        }

        [ThreadStatic]
        private static List<HediffDef> defaultIgnoredHediffs;

        // Compatibility note: This should've returned IList<HediffDef> (and empty array by default),
        // but such a change would break binary compatibility.
        public virtual List<HediffDef> IgnoredHediffs()
        {
            if (defaultIgnoredHediffs == null)
                defaultIgnoredHediffs = new List<HediffDef>(0); // ThreadStatic field always needs to be lazy-init
            else
                defaultIgnoredHediffs.Clear(); // ensure default list is empty - should be cheap operation if already empty
            return defaultIgnoredHediffs;
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

        public override bool Equals(object obj)
        {
            return obj is CompAbilityUser other &&
                GetType() == other.GetType() &&
                parent.thingIDNumber == other.parent.thingIDNumber;
        }

        public override int GetHashCode()
        {
            // Stable hash code based off type and parent.
            return Gen.HashCombineInt(Gen.HashCombineInt(-66, GenText.StableStringHash(GetType().Name)), parent.thingIDNumber);
        }

        public override string ToString()
        {
            return $"{GetType().Name}(Pawn={Pawn}, AbilityData={abilityData?.AllPowersToString() ?? "null"})";
        }
    }

    // Exists for items to add powers to as it will always be on every Pawn and initialized.
    public class GenericCompAbilityUser : CompAbilityUser
    {
        public override bool TryTransformPawn()
        {
            return true;
        }
    }
}
