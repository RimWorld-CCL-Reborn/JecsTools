using System;
using RimWorld;
using Verse;
namespace AbilityUser
{
    public class ExtraDamage : IExposable
    {
        public int damage;
        public DamageDef damageDef;
        public float chance;

        public ExtraDamage() { }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref this.damage, "damage", -1);
            Scribe_Defs.Look<DamageDef>(ref this.damageDef, "damageDef");
            Scribe_Values.Look<float>(ref this.chance, "chance", -1f);
        }
    }
}
