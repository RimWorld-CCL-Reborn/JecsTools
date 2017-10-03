using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace JecsTools
{
    public class HediffCompDamageOverTime : HediffComp
    {
        public HediffCompProperties_DamageOverTime Props => this.props as HediffCompProperties_DamageOverTime;

        private int ticksUntilDamage = -1;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (ticksUntilDamage < 0)
            {
                ticksUntilDamage = Props.cycleInTicks;
                this.Pawn.TakeDamage(new DamageInfo(Props.cycleDamage, Props.cycleDamageAmt, -1, this.parent.pawn, this.parent.Part, null, DamageInfo.SourceCategory.ThingOrUnknown));
            }
            ticksUntilDamage--;
        }

        public override string CompDebugString()
        {
            StringBuilder s = new StringBuilder();
            s.AppendLine(base.CompDebugString());
            s.AppendLine(ticksUntilDamage.ToString());
            return s.ToString().TrimEndNewlines();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look<int>(ref this.ticksUntilDamage, "ticksUntilDamage", -1);
        }
    }
}
