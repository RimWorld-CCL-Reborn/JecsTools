using System.Text;
using Verse;

namespace JecsTools
{
    public class HediffComp_ExtraMeleeDamages : HediffComp
    {
        public HediffCompProperties_ExtraMeleeDamages Props => (HediffCompProperties_ExtraMeleeDamages)props;

        public override string CompTipStringExtra
        {
            get
            {
                var s = new StringBuilder();
                var b = base.CompTipStringExtra;
                if (b != "")
                    s.Append(b);
                var extraDamages = Props?.ExtraDamages;
                if (!extraDamages.NullOrEmpty())
                {
                    s.AppendLine("JT_HI_ExtraDamages".Translate());
                    for (var i = 0; i < extraDamages.Count; i++)
                        s.AppendLine("  +" + extraDamages[i].amount + " " + extraDamages[i].def.LabelCap);
                }
                return s.ToString().TrimEndNewlines();
            }
        }
    }
}
