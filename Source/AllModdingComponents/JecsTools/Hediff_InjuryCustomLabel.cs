using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace JecsTools
{
    public class Hediff_InjuryCustomLabel : Hediff_Injury
    {
        private static readonly Color OldInjuryColor = new Color(0.72f, 0.72f, 0.72f);
        public override Color LabelColor => (this.IsOld()) ? OldInjuryColor : this.def.defaultLabelColor;
    }
}
