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
                    if (Powers != null)
                        allPowers.AddRange(Powers);
                    if (TemporaryApparelPowers != null)
                        allPowers.AddRange(TemporaryApparelPowers);
                    if (TemporaryWeaponPowers != null)
                        allPowers.AddRange(TemporaryWeaponPowers);
                    //Log.Message($"AbilityData.AllPowers({this}) refresh => {allPowers.Count} powers");
                }
                return allPowers;
            }
            set => allPowers = value;
        }

        public int Count
        {
            get
            {
                if (allPowers != null)
                    return allPowers.Count;
                var count = 0;
                if (Powers != null)
                    count += Powers.Count;
                if (TemporaryApparelPowers != null)
                    count += TemporaryApparelPowers.Count;
                if (TemporaryWeaponPowers != null)
                    count += TemporaryWeaponPowers.Count;
                return count;
            }
        }

        public void ExposeData()
        {
            var typeString = GetType().ToString();
            Scribe_References.Look(ref pawn, "abilityDataPawn" + typeString);
            Scribe_Values.Look(ref abilityClass, "abilityDataClass" + typeString, null);
            Scribe_Collections.Look(ref powers, "abilityDataPowers" + typeString, LookMode.Deep, this);
        }

        public override string ToString()
        {
            return $"(AbilityClass={AbilityClass.Name}, Pawn={Pawn}, {AllPowersToString()})";
        }

        public string AllPowersToString()
        {
            if (Count == 0)
                return "(no powers)";
            return string.Format("Powers={{{0}}}, TemporaryApparelPowers={{{1}}}, TemporaryWeaponPowers={{{2}}}",
                Powers.ToStringSafeEnumerable(), TemporaryApparelPowers.ToStringSafeEnumerable(),
                TemporaryWeaponPowers.ToStringSafeEnumerable());
        }
    }
}
