using Verse;
using Verse.AI;

namespace CompVehicle
{
    public class ThinkNode_ConditionalCanManipulate : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.GetComp<CompVehicle>() is CompVehicle compVehicle &&
                compVehicle.manipulationStatus == ManipulationState.able) return true;
            return false;
        }
    }
}