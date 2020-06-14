using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace JecsTools
{
    public class ThinkNode_ConditionalShouldBeDestructive : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn.MapHeld.IsPlayerHome &&
                (pawn.Faction == Faction.OfPlayerSilentFail ||
                pawn.HostFaction == Faction.OfPlayerSilentFail))
                return false;
            return true;
        }
        
    }
}
