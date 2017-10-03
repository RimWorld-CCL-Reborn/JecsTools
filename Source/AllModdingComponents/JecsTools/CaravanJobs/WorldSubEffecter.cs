using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace JecsTools
{
        public class WorldSubEffecter
        {
            public SubEffecterDef def;

            public WorldSubEffecter(SubEffecterDef subDef)
            {
                this.def = subDef;
            }

            public virtual void SubEffectTick(GlobalTargetInfo A, GlobalTargetInfo B)
            {
            }

            public virtual void SubTrigger(GlobalTargetInfo A, GlobalTargetInfo B)
            {
            }

            public virtual void SubCleanup()
            {
            }
        }

}
