﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CompVehicle
{
    public class ThinkNode_ConditionalPawnInside : ThinkNode_Conditional
    {
        //If anyone is inside the cockpit, allow this action to take place.
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.GetComp<CompVehicle>() is CompVehicle compVehicle && ((compVehicle?.AllOccupants?.Count ?? 0) > 0 || compVehicle.manipulationStatus == ManipulationState.able )) return true;
            return false;
        }
    }
}
