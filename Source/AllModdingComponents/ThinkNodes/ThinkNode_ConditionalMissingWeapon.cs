using RimWorld;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalMissingHuntingWeapon : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            return !WorkGiver_HunterHunt.HasHuntingWeapon(pawn);
        }
    }
}