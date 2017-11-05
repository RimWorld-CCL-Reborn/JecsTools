using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace AbilityUser
{
    public class AbilityData : IExposable
    {
        private Pawn pawn = null;
        private Type abilityClass = null;
        private List<PawnAbility> powers = new List<PawnAbility>();
        private List<PawnAbility> temporaryWeaponPowers = new List<PawnAbility>();
        private List<PawnAbility> temporaryApparelPowers = new List<PawnAbility>();
        private List<PawnAbility> allPowers;

        public Pawn Pawn => pawn;
        public Type AbilityClass => abilityClass;
        public List<PawnAbility> Powers { get => powers; set => powers = value; }
        public List<PawnAbility> TemporaryWeaponPowers { get => temporaryWeaponPowers; set => temporaryWeaponPowers = value; }
        public List<PawnAbility> TemporaryApparelPowers { get => temporaryApparelPowers; set => temporaryApparelPowers = value; }

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

        public AbilityData()
        {

        }

        public AbilityData(CompAbilityUser abilityUser)
        {
            this.abilityClass = abilityUser.GetType();
            this.pawn = abilityUser.Pawn;
        }
        
        public void ExposeData()
        {
            Scribe_References.Look<Pawn>(ref this.pawn, "pawn");
            Scribe_Values.Look<Type>(ref this.abilityClass, "abilityClass", null);
            Scribe_Collections.Look<PawnAbility>(ref this.powers, "Powers", LookMode.Deep, new object[]
            {
                            this,
            });

        }
    }
}
