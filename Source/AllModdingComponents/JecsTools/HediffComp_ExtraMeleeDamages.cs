using System.Text;
using Verse;

namespace JecsTools
{
    public class HediffComp_ExtraMeleeDamages : HediffComp
    {
        public HediffCompProperties_ExtraMeleeDamages Props => (HediffCompProperties_ExtraMeleeDamages) props;

        public override string CompTipStringExtra
        {
            get
            {
                var s = new StringBuilder();
                var b = base.CompTipStringExtra;
                if (b != "")
                    s.Append(b);
                if ((Props?.ExtraDamages?.Count ?? 0) > 0)
                {
                    s.AppendLine("JT_HI_ExtraDamages".Translate());
                    for (var i = 0; i < Props.ExtraDamages.Count; i++)
                        s.AppendLine("  +" + Props.ExtraDamages[i].amount + " " + Props.ExtraDamages[i].def.LabelCap);
                }
                return s.ToString().TrimEndNewlines();
            }
        }
    }
}