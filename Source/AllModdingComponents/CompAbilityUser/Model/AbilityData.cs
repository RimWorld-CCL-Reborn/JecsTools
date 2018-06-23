using System;
using System.Collections.Generic;
using Verse;

namespace AbilityUser
{
    public class AbilityData : IExposable
    {
        private Type abilityClass;
        private List<PawnAbility> allPowers;
        private Pawn pawn;
        private List<PawnAbility> powers = new List<PawnAbility>();

        public AbilityData()
        {
        }

        public AbilityData(CompAbilityUser abilityUser)
        {
            abilityClass = abilityUser.GetType();
            pawn = abilityUser.Pawn;
        }

        public Pawn Pawn => pawn;
        public Type AbilityClass => abilityClass;

        public List<PawnAbility> Powers
        {
            get => powers;
            set => powers = value;
        }

        public List<PawnAbility> TemporaryWeaponPowers { get; set; } = new List<PawnAbility>();
        public List<PawnAbility> TemporaryApparelPowers { get; set; } = new List<PawnAbility>();

        public List<PawnAbility> AllPowers
        {
            get
            {
                if (allPowers == null)
                {
                    allPowers = new List<PawnAbility>();
                    if (!Powers.NullOrEmpty())
                        allPowers.AddRange(Powers);
                    if (!TemporaryApparelPowers.NullOrEmpty())
                        allPowers.AddRange(TemporaryApparelPowers);
                    if (!TemporaryWeaponPowers.NullOrEmpty())
                        allPowers.AddRange(TemporaryWeaponPowers);
                }
                return allPowers;
            }
            set => allPowers = value;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "abilityDataPawn" + this.GetType().ToString());
            Scribe_Values.Look(ref abilityClass, "abilityDataClass" + this.GetType().ToString(), null);
            Scribe_Collections.Look(ref powers, "abilityDataPowers" + this.GetType().ToString(), LookMode.Deep, this);
        }
    }
}