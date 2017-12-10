using System;
using RimWorld;
using Verse;
using System.Text;

namespace JecsTools
{
    public class HediffComp_Knockback : HediffComp
    {
        public HediffCompProperties_Knockback Props
        {
            get
            {
                return (HediffCompProperties_Knockback)this.props;
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                StringBuilder s = new StringBuilder();
                s.Append(base.CompTipStringExtra);
                s.AppendLine("JT_HI_Knockback".Translate(Props.knockbackChance.ToStringPercent()) + ((Props.explosiveKnockback) ? " ("+"JT_HI_KnockbackExplosive".Translate()+")" : ""));
                return s.ToString();
            }
        }
    }
}
