using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalCombatCapable : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.story != null && pawn.story.WorkTagIsDisabled(WorkTags.Violent))
            {
                return false;
            }
            return true;
        }
    }
}
