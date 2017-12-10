using RimWorld;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_Knockback : HediffCompProperties
    {
        public float knockbackChance = 0.2f;
        public float stunChance = -1f;
        public int stunTicks = 60;
        public IntRange knockDistance = new IntRange(2, 3);
        public bool explosiveKnockback = false;
        public float explosionSize = 2f;
        public DamageDef explosionDmg = DamageDefOf.Stun;
        public SoundDef knockbackSound = SoundDefOf.Pawn_Melee_Punch_HitPawn;

        public HediffCompProperties_Knockback()
        {
            this.compClass = typeof(HediffComp_Knockback);
        }
    }
}
