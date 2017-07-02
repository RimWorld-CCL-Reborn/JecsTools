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
namespace CompOversizedWeapon
{
    internal class CompOversizedWeapon : ThingComp
    {
        public CompProperties_OversizedWeapon Props => (CompProperties_OversizedWeapon)this.props;
    }
}
