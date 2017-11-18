using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AbilityUser
{
    public class PassiveEffectWorker
    {
        private PassiveEffectProperties props;
        public PassiveEffectProperties Props { get => props; set => props = value; }

        public virtual void DoEffect(CompAbilityUser abilityUser)
        {
            if (Props?.hediffs is List<HediffDef> hList && !hList.NullOrEmpty())
            {
                foreach (HediffDef h in hList)
                {
                    HealthUtility.AdjustSeverity(abilityUser.AbilityUser, h, 1f);
                }
            }
        }

        public virtual bool TryDoEffect(CompAbilityUser abilityUser)
        {
            DoEffect(abilityUser);
            return true;
        }

        public virtual bool CanDoEffect(CompAbilityUser abilityUser)
        {
            if (abilityUser == null)
            {
                return false;
            }
            Pawn pawn = abilityUser.AbilityUser;
            if (pawn == null)
            {
                return false;
            }
            if (pawn.jobs == null)
            {
                return false;
            }
            if (Props.awakeOnly && pawn?.CurJob?.def == JobDefOf.LayDown || pawn.Downed)
            {
                return false;
            }
            if (pawn.mindState == null)
            {
                return false;
            }
            if (Props.combatOnly && Props.combatOnly && !pawn.mindState.anyCloseHostilesRecently)
            {
                return false;
            }
            return true;
        }

        public virtual void Tick(CompAbilityUser abilityUser)
        {
            int rate = -1;
            switch (Props.tickerType)
            {
                case TickerType.Rare:
                    rate = 250;
                    break;
                case TickerType.Normal:
                    rate = 60;
                    break;
                case TickerType.Long:
                    rate = 2000;
                    break;
            }
            if (rate != -1)
            {
                if (Find.TickManager.TicksGame % rate == 0 && CanDoEffect(abilityUser))
                    TryDoEffect(abilityUser);
            }
        }
    }
}