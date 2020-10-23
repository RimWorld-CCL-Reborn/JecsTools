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
            Scribe_Values.Look(ref damage, nameof(damage), -1);
            Scribe_Defs.Look(ref damageDef, nameof(damageDef));
            Scribe_Values.Look(ref chance, nameof(chance), -1f);
        }
    }
}
