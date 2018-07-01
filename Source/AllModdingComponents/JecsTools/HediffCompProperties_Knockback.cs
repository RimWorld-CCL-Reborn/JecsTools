using RimWorld;
using Verse;

namespace JecsTools
{
    public class HediffCompProperties_Knockback : HediffCompProperties
    {
        public DamageDef explosionDmg = DefDatabase<DamageDef>.GetNamedSilentFail("Stun");
        public float explosionSize = 2f;
        public bool explosiveKnockback = false;
        public float knockbackChance = 0.2f;
        public SoundDef knockbackSound = DefDatabase<SoundDef>.GetNamedSilentFail("Pawn_Melee_Punch_HitPawn");
        public IntRange knockDistance = new IntRange(2, 3);
        public float stunChance = -1f;
        public int stunTicks = 60;

        public HediffCompProperties_Knockback()
        {
            compClass = typeof(HediffComp_Knockback);
        }
    }
}