using RimWorld;
using Verse;
using Verse.AI;

namespace ThinkNodes
{
    public class ThinkNode_ConditionalHunter : ThinkNode_Conditional
    {
        public bool allowBrawlers = false;
        
        protected override bool Satisfied(Pawn pawn)
        {
            return AmHunter(pawn) && (allowBrawlers || !pawn.story.traits.HasTrait(TraitDefOf.Brawler));
        }

        public static bool AmHunter(Pawn pawn)
        {
            return pawn.workSettings.WorkIsActive(WorkTypeDefOf.Hunting);
        }
    }
}