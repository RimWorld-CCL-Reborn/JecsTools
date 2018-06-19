using UnityEngine;
using Verse;

namespace JecsTools
{
    public class Hediff_InjuryCustomLabel : Hediff_Injury
    {
        private static readonly Color OldInjuryColor = new Color(0.72f, 0.72f, 0.72f);
        public override Color LabelColor => this.IsPermanent() ? OldInjuryColor : def.defaultLabelColor;
    }
}