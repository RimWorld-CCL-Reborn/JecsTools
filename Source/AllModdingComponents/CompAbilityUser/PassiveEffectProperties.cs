using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace AbilityUser
{
    public class PassiveEffectProperties
    {
        public TickerType tickerType = TickerType.Rare;
        public Type worker;
        public List<HediffDef> hediffs;
        public bool combatOnly = false;
        public bool awakeOnly = false;
        private PassiveEffectWorker passiveEffectWorkerInt = null;
        public PassiveEffectWorker Worker
        {
            get
            {
                if (this.passiveEffectWorkerInt == null)
                {
                    this.passiveEffectWorkerInt = (PassiveEffectWorker)Activator.CreateInstance(this.worker);
                    this.passiveEffectWorkerInt.Props = this;
                }
                return this.passiveEffectWorkerInt;
            }
        }
    }
}
