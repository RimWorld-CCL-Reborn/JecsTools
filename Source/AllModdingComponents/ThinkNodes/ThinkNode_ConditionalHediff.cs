using System.Linq;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalHediff : ThinkNode_Conditional
    {
        public string hediffDef;

        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.Drafted) return false;
            foreach (var unused in pawn.health.hediffSet.hediffs.Where(x => x.def.defName.EqualsIgnoreCase(hediffDef)))
            {
                return true;
            }

            return false;
        }
    }
}