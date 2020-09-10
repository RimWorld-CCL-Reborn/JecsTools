using System.Collections.Generic;
using RimWorld;
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

        // Ideally, Def field defaults should be done in a ResolveReferences override,
        // when DefDatabase and DefOfs are populated, but that doesn't exist for HediffCompProperties.
        // ConfigErrors suffices, but it's awkward and happens later than ResolveAllReferences loading stage.
        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            explosionDmg ??= DamageDefOf.Stun;
            knockbackSound ??= SoundDefOf.Pawn_Melee_Punch_HitPawn;
            return base.ConfigErrors(parentDef);
        }
    }
}
