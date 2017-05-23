using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CompSlotLoadable
{
    public class CompSlottedBonus : ThingComp
    {
        public CompProperties_SlottedBonus Props
        {
            get
            {
                return (CompProperties_SlottedBonus)this.props;
            }
        }
    }
}
