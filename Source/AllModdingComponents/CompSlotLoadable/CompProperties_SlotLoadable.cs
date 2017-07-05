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
    public class CompProperties_SlotLoadable : CompProperties
    {
        public bool gizmosOnEquip = true;

        public List<SlotLoadableDef> slots = new List<SlotLoadableDef>();

        public CompProperties_SlotLoadable() => this.compClass = typeof(CompSlotLoadable);
    }
}
