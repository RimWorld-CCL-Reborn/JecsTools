using System;
using System.Collections.Generic;
using Verse;

namespace AbilityUser
{
    public class PassiveEffectProperties
    {
        public bool awakeOnly = false;
        public bool combatOnly = false;
        public List<HediffDef> hediffs;
        private PassiveEffectWorker passiveEffectWorkerInt;
        public TickerType tickerType = TickerType.Rare;
        public Type worker;

        public PassiveEffectWorker Worker
        {
            get
            {
                if (passiveEffectWorkerInt == null)
                {
                    passiveEffectWorkerInt = (PassiveEffectWorker) Activator.CreateInstance(worker);
                    passiveEffectWorkerInt.Props = this;
                }
                return passiveEffectWorkerInt;
            }
        }
    }
}