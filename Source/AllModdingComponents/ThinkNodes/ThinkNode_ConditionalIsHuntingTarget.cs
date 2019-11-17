using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalIsHuntingTarget : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            Pawn meleeThreat = pawn.mindState.meleeThreat;
            if (meleeThreat == null)
            {
                return false;
            }
            if (this.IsHunting(pawn, meleeThreat))
            {
                return true;
            }
            return false;
        }

        private bool IsHunting(Pawn pawn, Pawn prey)
        {
            if (pawn.CurJob == null)
            {
                return false;
            }
            JobDriver_Hunt jobDriver_Hunt = pawn.jobs.curDriver as JobDriver_Hunt;
            if (jobDriver_Hunt != null)
            {
                return jobDriver_Hunt.Victim == prey;
            }
            JobDriver_PredatorHunt jobDriver_PredatorHunt = pawn.jobs.curDriver as JobDriver_PredatorHunt;
            return jobDriver_PredatorHunt != null && jobDriver_PredatorHunt.Prey == prey;
        }
    }
}
