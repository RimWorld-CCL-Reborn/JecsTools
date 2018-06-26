using System.Text;
using Verse;

namespace JecsTools
{
    public class HediffCompDamageOverTime : HediffComp
    {
        private int ticksUntilDamage = -1;
        public HediffCompProperties_DamageOverTime Props => props as HediffCompProperties_DamageOverTime;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (ticksUntilDamage < 0)
            {
                ticksUntilDamage = Props.cycleInTicks;
                MakeDamage();
            }
            ticksUntilDamage--;
        }

        public DamageInfo GetDamageInfo()
        {
            return new DamageInfo(Props.cycleDamage, Props.cycleDamageAmt, Props.armorPenetration, -1, parent.pawn, parent.Part, null,
                DamageInfo.SourceCategory.ThingOrUnknown);
        }

        public virtual void MakeDamage()
        {
            Pawn.TakeDamage(GetDamageInfo());
        }

        public override string CompDebugString()
        {
            var s = new StringBuilder();
            s.AppendLine(base.CompDebugString());
            s.AppendLine(ticksUntilDamage.ToString());
            return s.ToString().TrimEndNewlines();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksUntilDamage, "ticksUntilDamage", -1);
        }
    }
}