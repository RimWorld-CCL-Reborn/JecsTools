using Verse;

namespace AbilityUser
{
    public class ExtraDamage : IExposable
    {
        public float chance;
        public int damage;
        public DamageDef damageDef;

        public void ExposeData()
        {
            Scribe_Values.Look(ref damage, "damage", -1);
            Scribe_Defs.Look(ref damageDef, "damageDef");
            Scribe_Values.Look(ref chance, "chance", -1f);
        }
    }
}