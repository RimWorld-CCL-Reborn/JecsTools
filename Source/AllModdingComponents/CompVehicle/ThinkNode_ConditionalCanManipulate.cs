﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CompVehicle
{
    public class ThinkNode_ConditionalCanManipulate : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.GetComp<CompVehicle>() is CompVehicle compVehicle && compVehicle.manipulationStatus == ManipulationState.able) return true;
            return false;
        }
    }
}
