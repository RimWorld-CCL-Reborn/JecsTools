using System.Text;
using Verse;

namespace JecsTools
{
    public class HediffComp_DamageSoak : HediffComp
    {
        public HediffCompProperties_DamageSoak Props => (HediffCompProperties_DamageSoak) props;

        public override string CompTipStringExtra
        {
            get
            {
                var s = new StringBuilder();

                var b = base.CompTipStringExtra;
                if (b != "")
                    s.Append(b);

                if (Props.settings.NullOrEmpty())
                {
                    s.AppendLine("JT_HI_DamageSoaked".Translate((Props.damageType != null) ? Props.damageToSoak.ToString() + " (" +Props.damageType.LabelCap + ") " : Props.damageToSoak.ToString() + " (" +"AllDays".Translate() + ")"));                    
                }
                else
                {
                    foreach (var setting in Props.settings)
                    {
                        s.AppendLine("JT_HI_DamageSoaked".Translate((setting.damageType != null) ? setting.damageToSoak.ToString() + " (" +setting.damageType.LabelCap + ") " : setting.damageToSoak.ToString() + " (" +"AllDays".Translate() + ")"));                    
                    }
                }
                return s.ToString().TrimEndNewlines();
            }
        }
    }
}