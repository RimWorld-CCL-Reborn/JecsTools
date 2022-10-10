using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AbilityUser
{
    public class PassiveEffectWorker
    {
        public PassiveEffectProperties Props { get; set; }

        public virtual void DoEffect(CompAbilityUser abilityUser)
        {
            if (Props?.hediffs is List<HediffDef> hList)
                foreach (var h in hList)
                    HealthUtility.AdjustSeverity(abilityUser.Pawn, h, 1f);
        }

        public virtual bool TryDoEffect(CompAbilityUser abilityUser)
        {
            DoEffect(abilityUser);
            return true;
        }

        public virtual bool CanDoEffect(CompAbilityUser abilityUser)
        {
            if (abilityUser == null)
                return false;
            var pawn = abilityUser.Pawn;
            if (pawn == null)
                return false;
            if (pawn.jobs == null)
                return false;
            if (Props.awakeOnly && pawn.CurJob?.def == JobDefOf.LayDown || pawn.Downed)
                return false;
            if (pawn.mindState == null)
                return false;
            if (Props.combatOnly && Props.combatOnly && !pawn.mindState.anyCloseHostilesRecently)
                return false;
            return true;
        }

        public virtual void Tick(CompAbilityUser abilityUser)
        {
            var rate = Props.tickerType switch
            {
                TickerType.Normal => GenTicks.TicksPerRealSecond, // TODO: shouldn't this be 1 instead?
                TickerType.Rare => GenTicks.TickRareInterval,
                TickerType.Long => GenTicks.TickLongInterval,
                _ => -1,
            };
            if (rate != -1)
                if (Find.TickManager.TicksGame % rate == 0 && CanDoEffect(abilityUser))
                    TryDoEffect(abilityUser);
        }
    }
}
