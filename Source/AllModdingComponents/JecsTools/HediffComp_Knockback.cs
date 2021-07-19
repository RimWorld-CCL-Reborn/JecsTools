using Verse;

namespace JecsTools
{
    public class HediffComp_Knockback : HediffComp
    {
        public HediffCompProperties_Knockback Props => (HediffCompProperties_Knockback)props;

        public override string CompTipStringExtra =>
            "JT_HI_Knockback".Translate(Props.knockbackChance.ToStringPercent()) +
                (Props.explosiveKnockback ? " (" + "JT_HI_KnockbackExplosive".Translate() + ")" : TaggedString.Empty);
    }
}
