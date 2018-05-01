using System.Linq;
using System.Text;
using Verse;

namespace JecsTools
{
    public class HediffWithComps_Expanded : HediffWithComps
    {
        private HediffExpandedDef Def => def as HediffExpandedDef;
        
        public override string TipStringExtra
        {
            get
            {
                if (Def == null) return base.TipStringExtra;
                StringBuilder s = new StringBuilder();
                if (Def.showDescription)
                {
                    s.AppendLine(def.description);
                }
                if (!string.IsNullOrEmpty(Def.preListText)) s.AppendLine(Def.preListText.Translate());
                s.AppendLine(base.TipStringExtra);
                if (!string.IsNullOrEmpty(Def.postListText)) s.AppendLine(Def.postListText.Translate());
                return s.ToString();
            }
        }
    }
}