using System.Text;
using Verse;

namespace JecsTools
{
    public class HediffComp_Knockback : HediffComp
    {
        public HediffCompProperties_Knockback Props => (HediffCompProperties_Knockback) props;

        public override string CompTipStringExtra
        {
            get
            {
                var s = new StringBuilder();
                s.Append(base.CompTipStringExtra);
                //Changed by Tad.
                //s.AppendLine("JT_HI_Knockback".Translate(Props.knockbackChance.ToStringPercent()) + (Props.explosiveKnockback ? " (" + "JT_HI_KnockbackExplosive".Translate() + ")" : ""));
                s.AppendLine("JT_HI_Knockback".Translate(Props.knockbackChance.ToStringPercent()) +" "+ Props.explosiveKnockback + " " + " (JT_HI_KnockbackExplosive".Translate() + ")");
                return s.ToString();
            }
        }
    }
}