using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;
namespace CompSlotLoadable
{
    public class CompSlottedBonus : ThingComp
    {
        public CompProperties_SlottedBonus Props => (CompProperties_SlottedBonus)this.props;
    }
}
