using System;
using RimWorld;
using Verse;
using System.Text;

namespace JecsTools
{
    public class HediffComp_ExtraMeleeDamages : HediffComp
    {
        public HediffCompProperties_ExtraMeleeDamages Props
        {
            get
            {
                return (HediffCompProperties_ExtraMeleeDamages)this.props;
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                StringBuilder s = new StringBuilder();
                string b = base.CompTipStringExtra;
                if (b != "")
                   s.Append(b);
                if ((Props?.extraDamages?.Count ?? 0) > 0)
                {
                    s.AppendLine("JT_HI_ExtraDamages".Translate());
                    for (int i = 0; i < Props.extraDamages.Count; i++)
                    {
                        s.AppendLine("  +" + Props.extraDamages[i].amount + " " + Props.extraDamages[i].def.LabelCap);
                    }
                }
                return s.ToString().TrimEndNewlines();
            }
        }
    }
}
