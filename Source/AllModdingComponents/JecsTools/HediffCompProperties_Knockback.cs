using System.Collections.Generic;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_Knockback : HediffCompProperties
    {
        public DamageDef explosionDmg;
        public float explosionSize = 2f;
        public bool explosiveKnockback = false;
        public float knockbackChance = 0.2f;
        public SoundDef knockbackSound;
        public IntRange knockDistance = new IntRange(2, 3);
        public float stunChance = -1f;
        public int stunTicks = 60;

        public HediffCompProperties_Knockback()
        {
            compClass = typeof(HediffComp_Knockback);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (var error in base.ConfigErrors(parentDef))
                yield return error;
            if (explosionDmg == null)
                yield return nameof(explosionDmg) + " is null";
            // Note: knockbackSound can be null - if it is, the explosion sound defaults to explosionDmg.soundExplosion.
        }
    }
}
