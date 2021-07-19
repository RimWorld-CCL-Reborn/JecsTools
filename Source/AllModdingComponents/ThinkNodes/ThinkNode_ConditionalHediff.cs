using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalHediff : ThinkNode_Conditional
    {
        public string hediffDef;

        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.Drafted)
                return false;
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def.defName.EqualsIgnoreCase(hediffDef))
                    return true;
            }
            return false;
        }
    }
}
