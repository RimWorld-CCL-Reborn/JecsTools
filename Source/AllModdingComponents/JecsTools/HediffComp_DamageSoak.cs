using System;
using RimWorld;
using Verse;
using System.Text;

namespace JecsTools
{
    public class HediffComp_DamageSoak : HediffComp
    {

        public HediffCompProperties_DamageSoak Props
        {
            get
            {
                return (HediffCompProperties_DamageSoak)this.props;
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
                s.AppendLine("JT_HI_DamageSoaked".Translate(Props.damageToSoak));
                return s.ToString().TrimEndNewlines();
            }
        }
    }
}
